using LocalStorage;
using System.Text.Json;

namespace PreferencesLocalStorage;

public class PreferencesLocalStorage : ILocalStorage
{
    public Task SetAsync<T>(string key, T value)
    {
        Preferences.Set(key, JsonSerializer.Serialize(value));
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    { 
        if (!Preferences.ContainsKey(key))
            return Task.FromResult(default(T));

        var json = Preferences.Get(key, "");
        return Task.FromResult(JsonSerializer.Deserialize<T>(json));
    }

    public Task<T> GetAsync<T>(string key, T defaultValue)
    {
        if (!Preferences.ContainsKey(key))
            return Task.FromResult(defaultValue);

        var json = Preferences.Get(key, "");
        var value = JsonSerializer.Deserialize<T>(json);

        return Task.FromResult(value ?? defaultValue);
    }

    public Task<bool> ContainsAsync(string key)
    {
        return Task.FromResult(Preferences.ContainsKey(key));
    }

    public Task RemoveAsync(string key)
    {
        Preferences.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Preferences.Clear();
        return Task.CompletedTask;
    }
}
