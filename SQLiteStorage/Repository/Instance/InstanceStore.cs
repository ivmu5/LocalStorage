using SQLite;

namespace SQLiteStorage;

/// <summary>
/// Репозиторий для сущностей с единственным активным экземпляром (Singleton Entity).
///
/// Используется для конфигурационных и глобальных объектов приложения,
/// например: UISettings, AppSettings.
/// </summary>
public class InstanceStore<T> : IInstanceRepository<T>
    where T : BaseEntity<T>, IInstanceEntity<T>, new()
{
    private T? _instance;

    /// <summary>
    /// Событие, возникающее при изменении текущего instance.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<T>>? InstanceChanged;

    /// <summary>
    /// Вызывает событие InstanceChanged с указанным значением.
    /// </summary>
    /// <param name="oldValue">Старый instance сущности.</param>
    /// <param name="newValue">Новый instance сущности.</param>
    private void InvokeInstanceChanged(T? oldValue, T? newValue)
    {
        InstanceChanged?.Invoke(
            this,
            new ValueChangedEventArgs<T>(oldValue, newValue));
    }

    /// <summary>
    /// Инициализирует instance из базы данных.
    ///
    /// Если instance не найден — создаётся дефолтный и сохраняется.
    /// Также загружаются связанные сущности.
    /// </summary>
    public async Task InitAsync()
    {
        if (_instance != null)
            return;

        var instance =
            await GetQuery()
                .Where(t => t.IsInstance)
                .FirstOrDefaultAsync();

        if (instance == null)
        {
            instance = DefaultValue();
            await instance.SaveAsync();
        }
        await instance.LoadRelationshipsAsync();

        _instance = instance;

        InvokeInstanceChanged(null, instance);
    }

    /// <summary>
    /// Возвращает текущий instance синхронно.
    ///
    /// Требует предварительного вызова InitAsync().
    /// </summary>
    public T Get()
        => _instance
        ?? throw new InvalidOperationException(
            $"Instance '{typeof(T).Name}' not initialized");

    /// <summary>
    /// Асинхронно возвращает instance, гарантируя инициализацию.
    /// </summary>
    public async Task<T> GetAsync()
    {
        await InitAsync();
        return _instance!;
    }

    /// <summary>
    /// Заменяет текущий instance, сохраняет изменения и подгружает связанные сущности.
    /// Если новый instance уже является текущим — сохраняет его. Иначе — помечает старый как неактивный, сохраняет новый и вызывает событие InstanceChanged.
    /// </summary>
    /// <param name="instance">Новый instance.</param>
    public async Task SetAsync(T instance)
    {
        if (_instance?.LocalId == instance.LocalId)
        {
            await instance.SaveAsync();
            return;
        }

        var oldValue = _instance;

        await GetRepository()
            .UpdateColumnValueAsync(
                c => c.IsInstance,
                false);

        instance.IsInstance = true;

        await instance.SaveAsync();
        await instance.LoadRelationshipsAsync();

        _instance = instance;
        
        InvokeInstanceChanged(oldValue, instance);
    }

    /// <summary>
    /// Сохраняет текущий instance в базу данных.
    /// </summary>
    public async Task SaveAsync()
    {
        await InitAsync();
        await GetRepository()
            .UpdateColumnValueAsync(
                c => c.IsInstance,
                false);
        _instance!.IsInstance = true;
        await _instance!.SaveAsync();
    }

    /// <summary>
    /// Сбрасывает текущий instance и создаёт новый дефолтный.
    ///
    /// Все предыдущие instance помечаются как неактивные.
    /// </summary>
    public async Task<T> ResetAsync()
    {
        var oldValue = _instance;

        await GetRepository()
            .UpdateColumnValueAsync(
                c => c.IsInstance,
                false);

        oldValue?.IsInstance = false;

        var instance = DefaultValue();

        await instance.SaveAsync();
        await instance.LoadRelationshipsAsync();

        _instance = instance;

        InvokeInstanceChanged(oldValue, instance);

        return instance;
    }

    /// <summary>
    /// Получает repository для текущего типа сущности.
    /// </summary>
    private IRepository<T> GetRepository()
        => RepositoryRegistry.Get<T, IRepository<T>>();

    /// <summary>
    /// Возвращает IQueryable запрос к таблице сущности.
    /// </summary>
    private AsyncTableQuery<T> GetQuery()
        => GetRepository().AsyncQuery();

    /// <summary>
    /// Создаёт дефолтный instance сущности.
    ///
    /// Используется при отсутствии данных в базе.
    /// </summary>
    private T DefaultValue()
    {
        return new T
        {
            IsInstance = true
        };
    }
}