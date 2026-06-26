using SQLite;

namespace SQLiteStorage;

/// <summary>
/// Таблица key-value storage.
/// </summary>
public class StorageItem
{
    [PrimaryKey]
    public string Key { get; set; } = default!;

    public string Value { get; set; } = default!;
}