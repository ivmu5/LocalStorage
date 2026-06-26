using System.Collections;

namespace SQLiteStorage;

/// <summary>
/// Extensions для каскадного сохранения графа сущностей.
/// 
/// Поддерживает:
/// - FK связи (1:1 / N:1)
/// - коллекции (1:N)
/// - защиту от циклических ссылок
/// </summary>
public static class CascadeExtensions
{
    /// <summary>
    /// Каскадное сохранение сущности и всех связанных объектов.
    /// </summary>
    public static async Task SaveCascadeAsync<T>(
        this T entity,
        HashSet<object>? visited = null)
        where T : BaseEntity, new()
    {
        visited ??= new HashSet<object>();

        // Защита от циклических ссылок:
        // User -> Orders -> User
        if (visited.Contains(entity))
            return;

        visited.Add(entity);

        var type = entity.GetType();

        // Получаем все связи сущности
        var relations = RelationshipMetadataCache.Get(type);

        foreach (var rel in relations)
        {
            // =====================================
            // FK (1:1 / Many:1)
            // =====================================
            if (!rel.IsCollection)
            {
                // Получаем navigation object
                // Пример:
                // user.Profile
                var navigation =
                    rel.NavigationGetter?.Invoke(entity);

                if (navigation == null)
                    continue;

                // Сначала сохраняем связанную сущность
                await SaveCascadeAsync(
                    (dynamic)navigation,
                    visited);

                // Получаем ID связанной сущности
                var id =
                    ((ILocalEntity)navigation).LocalId;

                // Записываем FK:
                // user.ProfileId = profile.LocalId
                rel.ForeignKeySetterOnParent?.Invoke(
                    entity,
                    id);
            }

            // =====================================
            // Collections (1:N)
            // =====================================
            else
            {
                // Получаем коллекцию:
                // user.Orders
                var collection =
                    rel.NavigationGetter?.Invoke(entity)
                    as IEnumerable;

                if (collection == null)
                    continue;

                foreach (var item in collection)
                {
                    // Устанавливаем FK:
                    // order.UserId = user.LocalId
                    rel.ForeignKeySetterOnChild?.Invoke(
                        item!,
                        ((ILocalEntity)entity).LocalId);

                    // Каскадно сохраняем дочерний объект
                    await SaveCascadeAsync(
                        (dynamic)item!,
                        visited);
                }
            }
        }

        // =====================================
        // Сохраняем текущую сущность
        // =====================================
        var repo = RepositoryRegistry.Get(type);

        await repo.SaveAsync(entity);
    }

    /// <summary>
    /// Каскадное сохранение списка сущностей.
    /// 
    /// Использует общий visited cache,
    /// чтобы избежать повторного сохранения.
    /// </summary>
    public static async Task SaveCascadeAsync<T>(
        this IEnumerable<T> entities)
        where T : BaseEntity, new()
    {
        var visited = new HashSet<object>();

        foreach (var entity in entities)
        {
            if (entity == null)
                continue;

            await SaveCascadeAsync(entity, visited);
        }
    }
}