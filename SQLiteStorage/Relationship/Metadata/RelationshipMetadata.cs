using SQLiteStorage;

internal class RelationshipMetadata
{
    /// <summary>
    /// Тип связанной сущности.
    /// </summary>
    public required Type RelatedType { get; init; }

    /// <summary>
    /// Атрибут relationship.
    /// </summary>
    public required IRelationship Attribute { get; init; }

    /// <summary>
    /// Является ли связь коллекцией.
    /// </summary>
    public bool IsCollection { get; init; }

    /// <summary>
    /// Указывает, может ли связь быть пустой. 
    /// Если true, то при загрузке будет разрешено отсутствие значения. 
    /// Если false, то поле обязательно, и при отсутствии значения будет вызываться стандартный конструктор построения сущности и ее сохранение.
    /// </summary>
    public bool IsNullable { get; init; }

    // =============================
    // Navigation
    // =============================

    /// <summary>
    /// Getter navigation property.
    /// 
    /// user => user.Profile
    /// user => user.Orders
    /// </summary>
    public required Func<object, object?> NavigationGetter { get; init; }

    /// <summary>
    /// Setter navigation property.
    /// 
    /// (user, profile) => user.Profile = profile
    /// </summary>
    public required Action<object, object?> NavigationSetter { get; init; }

    // =============================
    // FK на родителе
    // =============================

    /// <summary>
    /// Getter FK свойства.
    /// 
    /// user => user.ProfileId
    /// </summary>
    public Func<object, object?>? ForeignKeyGetterOnParent { get; init; }

    /// <summary>
    /// Setter FK свойства.
    /// 
    /// (user, id) => user.ProfileId = id
    /// </summary>
    public Action<object, object?>? ForeignKeySetterOnParent { get; init; }

    // =============================
    // FK на дочернем объекте
    // =============================

    /// <summary>
    /// Setter FK дочернего объекта.
    /// 
    /// (order, userId) => order.UserId = userId
    /// </summary>
    public Action<object, object?>? ForeignKeySetterOnChild { get; init; }

    /// <summary>
    /// Getter FK дочернего объекта.
    /// 
    /// order => order.UserId
    /// </summary>
    public Func<object, object?>? ForeignKeyGetterOnChild { get; init; }
}