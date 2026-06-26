using SQLite;

namespace SQLiteStorage;

/// <summary>
/// Базовая сущность SQLite ORM.
///
/// Содержит:
/// - Primary Key
/// - дату создания
/// - версию записи
/// </summary>
public abstract class BaseEntity : ILocalEntity
{
    /// <summary>
    /// Локальный primary key сущности.
    ///
    /// Используется как:
    /// - SQLite PrimaryKey
    /// - ForeignKey
    /// - identity сущности
    /// </summary>
    [PrimaryKey]
    public Guid LocalId { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Дата создания сущности.
    ///
    /// Устанавливается автоматически
    /// при создании объекта.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    /// <summary>
    /// Версия сущности.
    ///
    /// Увеличивается при сохранении.
    ///
    /// Может использоваться для:
    /// - optimistic concurrency
    /// - sync
    /// - change tracking
    /// </summary>
    public int Version { get; set; } = 0;
}

/// <summary>
/// Generic базовая сущность.
///
/// Добавляет:
/// - доступ к repository
/// - ActiveRecord-like API
///
/// Пример:
///
/// await user.SaveAsync();
/// var users = await User.AllAsync();
/// </summary>
public abstract class BaseEntity<T> : BaseEntity
    where T : BaseEntity<T>, new()
{
    /// <summary>
    /// Получить repository сущности.
    ///
    /// Пример:
    /// User.Repository()
    /// </summary>
    public static IRepository<T> Repository()
        => RepositoryRegistry.Get<T, IRepository<T>>();

    // =====================================================
    // Instance methods
    // =====================================================

    /// <summary>
    /// Сохранить текущую сущность.
    ///
    /// Аналог:
    /// repository.SaveAsync(entity)
    /// </summary>
    public virtual Task SaveAsync()
        => Repository().SaveAsync((T)this);

    /// <summary>
    /// Удалить текущую сущность.
    ///
    /// Аналог:
    /// repository.DeleteAsync(entity)
    /// </summary>
    public virtual Task DeleteAsync()
        => Repository().DeleteAsync((T)this);

    // =====================================================
    // Static query methods
    // =====================================================

    /// <summary>
    /// Получить все сущности.
    ///
    /// Автоматически загружает relationships.
    /// </summary>
    public static Task<List<T>> AllAsync()
        => Repository().GetAllAsync();

    /// <summary>
    /// Получить сущность по ID.
    ///
    /// Автоматически загружает relationships.
    /// </summary>
    public static Task<T> GetAsync(Guid id)
        => Repository().GetAsync(id);
}