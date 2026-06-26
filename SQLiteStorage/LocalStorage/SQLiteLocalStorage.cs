using LocalStorage;
using SQLite;
using System.Text.Json;

namespace SQLiteStorage;

/// <summary>
/// Key-Value storage поверх SQLite.
/// 
/// Используется для:
/// - settings
/// - cache
/// - small persisted data
/// </summary>
public class SQLiteLocalStorage : ILocalStorage
{
    private readonly SQLiteAsyncConnection _database;

    public SQLiteLocalStorage(SQLiteAsyncConnection db)
    {
        _database = db;

        // ⚠️ sync-over-async здесь допустим только при startup
        _database.CreateTableAsync<StorageItem>()
            .GetAwaiter()
            .GetResult();
    }

    // =====================================
    // SET VALUE (insert or update)
    // =====================================
    public async Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);

        var item = new StorageItem
        {
            Key = key,
            Value = json
        };

        await _database.InsertOrReplaceAsync(item);
    }

    // =====================================
    // GET VALUE (nullable)
    // =====================================
    public async Task<T?> GetAsync<T>(string key)
    {
        var item = await _database.Table<StorageItem>()
            .FirstOrDefaultAsync(x => x.Key == key);

        if (item == null)
            return default;

        return JsonSerializer.Deserialize<T>(item.Value);
    }

    // =====================================
    // GET WITH DEFAULT VALUE
    // =====================================
    public async Task<T> GetAsync<T>(string key, T defaultValue)
    {
        var item = await _database.Table<StorageItem>()
            .FirstOrDefaultAsync(x => x.Key == key);

        if (item == null)
            return defaultValue;

        var value = JsonSerializer.Deserialize<T>(item.Value);

        return value ?? defaultValue;
    }

    // =====================================
    // CHECK EXISTENCE
    // =====================================
    public async Task<bool> ContainsAsync(string key)
    {
        var item = await _database.Table<StorageItem>()
            .FirstOrDefaultAsync(x => x.Key == key);

        return item != null;
    }

    // =====================================
    // REMOVE ITEM
    // =====================================
    public Task RemoveAsync(string key)
    {
        return _database.DeleteAsync<StorageItem>(key);
    }

    // =====================================
    // CLEAR STORAGE
    // =====================================
    public Task ClearAsync()
    {
        return _database.DeleteAllAsync<StorageItem>();
    }
}