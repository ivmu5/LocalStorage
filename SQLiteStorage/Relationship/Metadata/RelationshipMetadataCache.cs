using SQLiteStorage;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Кеш метаданных relationships.
///
/// Reflection и Expression compilation
/// выполняются только один раз для типа.
/// </summary>
internal static class RelationshipMetadataCache
{
    /// <summary>
    /// Кеш:
    ///
    /// typeof(User)
    ///     -> List&lt;RelationshipMetadata&gt;
    /// </summary>
    private static readonly Dictionary<Type, List<RelationshipMetadata>>
        _cache = new();

    /// <summary>
    /// Получить metadata relationships для типа.
    ///
    /// Если metadata уже построены —
    /// возвращаются из кеша.
    /// </summary>
    public static List<RelationshipMetadata> Get(Type type)
    {
        if (_cache.TryGetValue(type, out var cached))
            return cached;

        var meta = Build(type);

        _cache[type] = meta;

        return meta;
    }

    /// <summary>
    /// Построение metadata relationships.
    ///
    /// Здесь:
    /// - ищутся relationship attributes
    /// - определяется тип relationship
    /// - создаются compiled delegates
    /// </summary>
    private static List<RelationshipMetadata> Build(Type type)
    {
        var result = new List<RelationshipMetadata>();

        foreach (var prop in type.GetProperties())
        {
            // Ищем relationship attribute
            var attr = prop.GetCustomAttributes()
                .OfType<IRelationship>()
                .FirstOrDefault();

            if (attr == null)
                continue;

            // Проверяем:
            // navigation это коллекция или нет
            var isCollection =
                typeof(System.Collections.IEnumerable)
                    .IsAssignableFrom(prop.PropertyType)
                && prop.PropertyType != typeof(string);

            // =====================================
            // FK (1:1 / Many:1)
            // =====================================
            if (!isCollection)
            {
                // User.ProfileId
                var fkProp =
                    type.GetProperty(attr.ForeignKeyName)!;

                result.Add(new RelationshipMetadata
                {
                    // Profile
                    RelatedType = prop.PropertyType,

                    // [ForeignKey]
                    Attribute = attr,

                    // Не коллекция
                    IsCollection = false,

                    IsNullable = attr.IsNullable,

                    // =============================
                    // Navigation
                    // =============================

                    // user => user.Profile
                    NavigationGetter =
                        BuildNavigationGetter(type, prop),

                    // (user, profile)
                    //     => user.Profile = profile
                    NavigationSetter =
                        BuildNavigationSetter(type, prop),

                    // =============================
                    // FK на родителе
                    // =============================

                    // user => user.ProfileId
                    ForeignKeyGetterOnParent =
                        BuildForeignKeyGetter(type, fkProp),

                    // (user, id)
                    //     => user.ProfileId = id
                    ForeignKeySetterOnParent =
                        BuildForeignKeySetter(type, fkProp)
                });
            }

            // =====================================
            // Collection (1:N)
            // =====================================
            else
            {
                // List<Order> -> Order
                var elementType =
                    prop.PropertyType.GetGenericArguments()[0];

                // Order.UserId
                var fkProp =
                    elementType.GetProperty(
                        attr.ForeignKeyName)!;

                result.Add(new RelationshipMetadata
                {
                    // Order
                    RelatedType = elementType,

                    // [OneToMany]
                    Attribute = attr,

                    // Коллекция
                    IsCollection = true,

                    IsNullable = true,

                    // =============================
                    // Navigation
                    // =============================

                    // user => user.Orders
                    NavigationGetter =
                        BuildNavigationGetter(type, prop),

                    // (user, orders)
                    //     => user.Orders = orders
                    NavigationSetter =
                        BuildNavigationSetter(type, prop),

                    // =============================
                    // FK на дочернем объекте
                    // =============================

                    // order => order.UserId
                    ForeignKeyGetterOnChild =
                        BuildForeignKeyGetter(
                            elementType,
                            fkProp),

                    // (order, userId)
                    //     => order.UserId = userId
                    ForeignKeySetterOnChild =
                        BuildForeignKeySetter(
                            elementType,
                            fkProp)
                });
            }
        }

        return result;
    }

    // =====================================================
    // Navigation
    // =====================================================

    /// <summary>
    /// Getter navigation property.
    ///
    /// user => user.Profile
    /// user => user.Orders
    /// </summary>
    private static Func<object, object?> BuildNavigationGetter(
        Type type,
        PropertyInfo prop)
    {
        return BuildGetter(type, prop);
    }

    /// <summary>
    /// Setter navigation property.
    ///
    /// (user, profile)
    ///     => user.Profile = profile
    /// </summary>
    private static Action<object, object?> BuildNavigationSetter(
        Type type,
        PropertyInfo prop)
    {
        return BuildSetter(type, prop);
    }

    // =====================================================
    // Foreign Keys
    // =====================================================

    /// <summary>
    /// Getter FK свойства.
    ///
    /// user => user.ProfileId
    /// order => order.UserId
    /// </summary>
    private static Func<object, object?> BuildForeignKeyGetter(
        Type type,
        PropertyInfo prop)
    {
        return BuildGetter(type, prop);
    }

    /// <summary>
    /// Setter FK свойства.
    ///
    /// (user, id)
    ///     => user.ProfileId = id
    ///
    /// (order, userId)
    ///     => order.UserId = userId
    /// </summary>
    private static Action<object, object?> BuildForeignKeySetter(
        Type type,
        PropertyInfo prop)
    {
        return BuildSetter(type, prop);
    }

    // =====================================================
    // Core builders
    // =====================================================

    /// <summary>
    /// Построение getter delegate.
    ///
    /// PropertyInfo превращается в:
    ///
    /// x => x.Property
    /// </summary>
    private static Func<object, object?> BuildGetter(
        Type type,
        PropertyInfo prop)
    {
        // object x
        var obj =
            Expression.Parameter(typeof(object), "x");

        // (User)x
        var castObj =
            Expression.Convert(obj, type);

        // ((User)x).Property
        var body =
            Expression.Property(castObj, prop);

        // object result
        var convert =
            Expression.Convert(body, typeof(object));

        // x => ((User)x).Property
        return Expression
            .Lambda<Func<object, object?>>(
                convert,
                obj)
            .Compile();
    }

    /// <summary>
    /// Построение setter delegate.
    ///
    /// PropertyInfo превращается в:
    ///
    /// (x, v) => x.Property = v
    /// </summary>
    private static Action<object, object?> BuildSetter(
        Type type,
        PropertyInfo prop)
    {
        // object x
        var obj =
            Expression.Parameter(typeof(object), "x");

        // object v
        var value =
            Expression.Parameter(typeof(object), "v");

        // (User)x
        var castObj =
            Expression.Convert(obj, type);

        // (PropertyType)v
        var castValue =
            Expression.Convert(
                value,
                prop.PropertyType);

        // ((User)x).Property = (PropertyType)v
        var body =
            Expression.Assign(
                Expression.Property(castObj, prop),
                castValue);

        // (x, v) => ((User)x).Property = value
        return Expression
            .Lambda<Action<object, object?>>(
                body,
                obj,
                value)
            .Compile();
    }
}