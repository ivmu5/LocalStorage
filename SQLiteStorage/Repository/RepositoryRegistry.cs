using System.Collections.Concurrent;

namespace SQLiteStorage;

/// <summary>
/// Глобальный registry repositories.
///
/// Хранит repository экземпляры
/// для каждого типа сущности.
///
/// Используется как lightweight DI container
/// внутри ORM.
///
/// Пример:
///
/// RepositoryRegistry.Register(new UserRepository());
///
/// var repo = RepositoryRegistry.Get&lt;User&gt;();
/// </summary>
public static class RepositoryRegistry
{
    /// <summary>
    /// Кеш repositories.
    ///
    /// Пример:
    /// typeof(User) -> UserRepository
    /// </summary>
    private static readonly ConcurrentDictionary<Type, IRepository>
        _repos = new();

    /// <summary>
    /// Зарегистрировать repository
    /// для сущности.
    ///
    /// Пример:
    ///
    /// Register&lt;User&gt;(new UserRepository())
    /// </summary>
    public static void Register<TEntity>(
        IRepository<TEntity> repo)
        where TEntity : BaseEntity<TEntity>, new()
    {
        var type = typeof(TEntity);
        if (_repos.ContainsKey(type))
            throw new InvalidOperationException(
                $"Repository for {type} already registered");

        if (!_repos.TryAdd(type, repo))
        {
            throw new InvalidOperationException(
                $"Repository for {type} already registered");
        }
    }

    /// <summary>
    /// Получить repository
    /// с конкретным типом repository.
    ///
    /// Пример:
    ///
    /// var repo =
    ///     Get&lt;User, UserRepository&lt;User&gt;();
    /// </summary>
    public static TRepo Get<TEntity, TRepo>()
        where TEntity : BaseEntity<TEntity>, new()
        where TRepo : IRepository<TEntity>
    {
        return (TRepo)Get<TEntity>();
    }

    /// <summary>
    /// Получить repository сущности.
    ///
    /// Пример:
    ///
    /// var repo = Get&lt;User&gt;();
    /// </summary>
    public static IRepository<TEntity> Get<TEntity>()
        where TEntity : BaseEntity<TEntity>, new()
    {
        return (IRepository<TEntity>)Get(typeof(TEntity));
    }

    /// <summary>
    /// Получить repository по runtime type.
    ///
    /// Используется в:
    /// - relationships
    /// - cascade save
    /// - reflection/runtime ORM logic
    /// </summary>
    public static IRepository Get(Type type)
    {
        if (!_repos.TryGetValue(type, out var repo))
            throw new InvalidOperationException(
                $"Repository for {type} not registered");

        return repo;
    }
}