namespace SQLiteStorage;

/// <summary>
/// Extensions для загрузки relationships.
///
/// Поддерживает:
/// - FK relationships (1:1 / Many:1)
/// - Collection relationships (1:N)
/// </summary>
public static class SQLiteRelationshipExtensions
{
    /// <summary>
    /// Загрузка relationships для одной сущности.
    /// </summary>
    public static async Task LoadRelationshipsAsync<T>(this T entity)
        where T : BaseEntity<T>, new()
    {
        var type = typeof(T);

        // Получаем metadata relationships
        var relations =
            RelationshipMetadataCache.Get(type);

        foreach (var rel in relations)
        {
            // =====================================
            // FK (1:1 / Many:1)
            // =====================================
            if (!rel.IsCollection)
            {
                // Получаем FK:
                // user.ProfileId
                var fkValue =
                    rel.ForeignKeyGetterOnParent?.Invoke(entity);

                if (fkValue is not Guid id)
                    continue;

                // Получаем repository связанной сущности
                var repo = RepositoryRegistry.Get(rel.RelatedType);

                // Загружаем связанную сущность
                var value =
                    await repo.GetAsync(id);

                // Записываем navigation:
                // user.Profile = profile
                rel.NavigationSetter(entity, value);
            }

            // =====================================
            // Collections (1:N)
            // =====================================
            else
            {
                // Repository дочерней сущности
                var repo = RepositoryRegistry.Get(rel.RelatedType);

                // ID родителя:
                // user.LocalId
                var parentId =
                    ((ILocalEntity)entity).LocalId;

                // Загружаем все дочерние сущности
                // TODO:
                // заменить на SQL query по FK
                var all =
                    await repo.GetAllAsync();

                // Фильтруем:
                // order.UserId == user.LocalId
                var list = all
                    .Cast<object>()
                    .Where(x =>
                    {
                        var fk =
                            rel.ForeignKeyGetterOnChild?.Invoke(x);

                        return fk is Guid g
                               && g == parentId;
                    })
                    .ToList();

                // user.Orders = orders
                rel.NavigationSetter(entity, list);
            }
        }
    }

    /// <summary>
    /// Загрузка relationships для списка сущностей.
    ///
    /// Оптимизирована:
    /// - FK загружаются batch-запросом
    /// - коллекции группируются через lookup
    /// </summary>
    public static async Task LoadRelationshipsAsync<T>(
        this IEnumerable<T> entities)
        where T : BaseEntity<T>, new()
    {
        var entityList =
            entities.ToList();

        if (entityList.Count == 0)
            return;

        var type = typeof(T);

        // Получаем metadata relationships
        var relations =
            RelationshipMetadataCache.Get(type);

        foreach (var rel in relations)
        {
            // =====================================
            // FK (1:1 / Many:1)
            // =====================================
            if (!rel.IsCollection)
            {
                // Собираем все FK ids:
                // user.ProfileId
                var ids = entityList
                    .Select(x =>
                        rel.ForeignKeyGetterOnParent?.Invoke(x))
                    .OfType<Guid>()
                    .Distinct()
                    .ToList();

                if (ids.Count == 0)
                    continue;

                // Repository связанной сущности
                var repo = RepositoryRegistry.Get(rel.RelatedType);

                // Batch loading:
                // SELECT * WHERE Id IN (...)
                var related =
                    await repo.GetManyAsync(ids);

                // Dictionary:
                // ProfileId -> Profile
                var dict = related
                    .Cast<ILocalEntity>()
                    .ToDictionary(x => x.LocalId);

                foreach (var entity in entityList)
                {
                    var fkValue =
                        rel.ForeignKeyGetterOnParent?.Invoke(entity);

                    if (fkValue is not Guid id)
                        continue;

                    // user.Profile = loadedProfile
                    if (dict.TryGetValue(id, out var value))
                    {
                        rel.NavigationSetter(entity, value);
                    }
                }
            }

            // =====================================
            // Collections (1:N)
            // =====================================
            else
            {
                // Repository дочерней сущности
                var repo = RepositoryRegistry.Get(rel.RelatedType);

                // Загружаем все дочерние сущности
                // TODO:
                // заменить на query по parent ids
                var allChildren =
                    await repo.GetAllAsync();

                // Группировка:
                // UserId -> List<Order>
                var lookup = allChildren
                    .Cast<object>()
                    .GroupBy(x =>
                    {
                        return (Guid)
                            rel.ForeignKeyGetterOnChild!
                                .Invoke(x)!;
                    })
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList());

                foreach (var entity in entityList)
                {
                    // user.LocalId
                    var parentId =
                        ((ILocalEntity)entity).LocalId;

                    // user.Orders = orders
                    if (lookup.TryGetValue(parentId, out var list))
                    {
                        rel.NavigationSetter(entity, list);
                    }
                    else
                    {
                        // Создаём пустую коллекцию:
                        // new List<Order>()
                        var emptyCollection =
                            Activator.CreateInstance(
                                rel.NavigationGetter(entity)!
                                    .GetType());

                        rel.NavigationSetter(
                            entity,
                            emptyCollection);
                    }
                }
            }
        }
    }

    public static async Task EnsureRelationshipsAsync<T>(this T entity)
        where T : BaseEntity<T>, new()
    {
        var type = typeof(T);
        var relations = RelationshipMetadataCache.Get(type);

        foreach (var rel in relations)
        {
            // =========================
            // FK (1:1)
            // =========================
            if (!rel.IsCollection)
            {
                var fkValue = (Guid)rel.ForeignKeyGetterOnParent?.Invoke(entity);

                if (fkValue != Guid.Empty)
                    continue;

                if (!rel.IsNullable)
                {
                    // =========================
                    // CREATE MISSING ENTITY
                    // =========================
                    var newEntity = (dynamic)Activator.CreateInstance(rel.RelatedType)!;

                    var repo = RepositoryRegistry.Get(rel.RelatedType);

                    await repo.SaveAsync(newEntity);

                    rel.NavigationSetter(entity, newEntity);

                    rel.ForeignKeySetterOnParent?.Invoke(
                        entity,
                        newEntity.LocalId);
                }
            }

            // =========================
            // Collection (1:N)
            // =========================
            else
            {
                var collection = rel.NavigationGetter(entity);

                if (collection is not System.Collections.IEnumerable enumerable)
                {
                    var listType = typeof(List<>).MakeGenericType(rel.RelatedType);
                    var empty = Activator.CreateInstance(listType);

                    rel.NavigationSetter(entity, empty);
                }
            }
        }
    }
}