namespace SQLiteStorage;

/// <summary>
/// Options для SQLite storage.
/// </summary>
public class SQLiteStorageOptions
{
    /// <summary>
    /// Маппинг:
    /// BaseEntity&lt;T&gt; -> Repository&lt;T&gt;
    /// </summary>
    public Dictionary<Type, Type> SortTypes { get; init; } = new()
    {
        { typeof(BaseEntity<>), typeof(LocalBaseRepository<>) }
    };

    /// <summary>
    /// DB filename
    /// </summary>
    public static string DefaultDatabaseFileName
        => "storage.db";

    /// <summary>
    /// Default path (MAUI)
    /// </summary>
    public static string DefaultDatabasePath
        => Path.Combine(
            FileSystem.AppDataDirectory,
            DefaultDatabaseFileName);

    /// <summary>
    /// Path to SQLite database
    /// </summary>
    public string DatabasePath { get; set; }
        = DefaultDatabasePath;

    /// <summary>
    /// Entities to register
    /// </summary>
    public List<Type> EntityTypes { get; init; }
        = new();

    /// <summary>
    /// Key for encryption database
    /// </summary>
    public string? EncryptionKey { get; set; }
}