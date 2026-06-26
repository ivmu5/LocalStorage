namespace SQLiteStorage;

public interface IInstanceRepository<T> where T : class, new()
{
    Task InitAsync();
    T Get();
    Task<T> GetAsync();
    Task SaveAsync();
    Task SetAsync(T instance);
    Task<T> ResetAsync();
}
