namespace LocalStorage;

public interface ILocalStorage
{
    Task SetAsync<T>(string key, T value);
    Task<T?> GetAsync<T>(string key);
    Task<T> GetAsync<T>(string key, T defaultValue);
    Task<bool> ContainsAsync(string key);
    Task RemoveAsync(string key);
    Task ClearAsync();
}