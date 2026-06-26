using SQLite;
using System.Linq.Expressions;
using System.Reflection;

namespace SQLiteStorage;

/// <summary>
/// Базовый локальный repository для SQLite.
///
/// Отвечает за:
/// - CRUD операции
/// - загрузку relationships
/// - выполнение SQL запросов
/// - работу с AsyncTableQuery
/// </summary>
public class LocalBaseRepository<T> : IRepository<T>
    where T : BaseEntity<T>, new()
{
    /// <summary>
    /// SQLite connection.
    /// </summary>
    protected readonly SQLiteAsyncConnection _db;

    /// <summary>
    /// Создание repository.
    /// </summary>
    public LocalBaseRepository(SQLiteAsyncConnection db)
    {
        _db = db;
    }

    // =====================================================
    // Table metadata
    // =====================================================

    /// <summary>
    /// Получить имя таблицы сущности.
    /// </summary>
    public async Task<string> GetTableNameAsync()
        => (await GetTableMappingAsync()).TableName;

    /// <summary>
    /// Получить SQLite mapping таблицы.
    /// </summary>
    public async Task<TableMapping> GetTableMappingAsync()
        => await _db.GetMappingAsync(typeof(T));

    // =====================================================
    // Save / Delete
    // =====================================================

    /// <summary>
    /// Сохранение сущности.
    /// 
    /// Если сущность новая → Insert.
    /// Если уже существует → Update.
    /// </summary>
    public virtual async Task<int> SaveAsync(T entity)
    {
        int updated = await _db.UpdateAsync(entity);

        if (updated > 0)
        {
            entity.Version++;
            return updated;
        }

        var inserted = await _db.InsertAsync(entity);

        if (inserted > 0)
            entity.Version++;

        return inserted;
    }

    /// <summary>
    /// Удаление сущности.
    /// </summary>
    public virtual async Task<int> DeleteAsync(T entity)
        => await _db.DeleteAsync(entity);

    // =====================================================
    // Column updates
    // =====================================================

    /// <summary>
    /// Массовое обновление значения колонки.
    ///
    /// Пример:
    /// UpdateColumnValueAsync(
    ///     x => x.IsEnabled,
    ///     true)
    /// </summary>
    public async Task<int> UpdateColumnValueAsync(
        Expression<Func<T, object>> selector,
        object value)
    {
        var mapping =
            await GetTableMappingAsync();

        // Получаем PropertyInfo
        // из expression:
        // x => x.Property
        var property =
            GetProperty(selector);

        // Находим SQLite column mapping
        var column = mapping.Columns
            .FirstOrDefault(x =>
                x.PropertyName == property.Name);

        if (column == null)
        {
            throw new InvalidOperationException(
                $"Column {property.Name} " +
                $"not found in table {mapping.TableName}");
        }

        // Приведение значения
        // к SQLite-compatible формату
        var normalized =
            value.NormalizeValueToSQLite(
                column.ColumnType);

        // UPDATE table
        // SET column = value
        // WHERE column != value
        string query =
            $"UPDATE {mapping.TableName} " +
            $"SET {column.Name} = ? " +
            $"WHERE {column.Name} != ?";

        return await ExecuteAsync(
            query,
            normalized,
            normalized);
    }

    /// <summary>
    /// Получить PropertyInfo из expression.
    ///
    /// Примеры:
    /// x => x.Name
    /// x => x.Version
    /// </summary>
    private static PropertyInfo GetProperty(
        Expression<Func<T, object>> selector)
    {
        return selector.Body switch
        {
            // x => x.Property
            MemberExpression m =>
                (PropertyInfo)m.Member,

            // boxing value types:
            // x => (object)x.Version
            UnaryExpression
            {
                Operand: MemberExpression m
            } =>
                (PropertyInfo)m.Member,

            _ => throw new InvalidOperationException(
                "Invalid property expression")
        };
    }

    // =====================================================
    // Queries
    // =====================================================

    /// <summary>
    /// Получить сущность по ID.
    ///
    /// Автоматически загружает relationships.
    /// </summary>
    public virtual async Task<T> GetAsync(Guid id)
    {
        var entity =
            await _db.FindAsync<T>(id);

        if (entity == null)
            return null!;

        // Загружаем:
        // FK + collections
        await entity.LoadRelationshipsAsync();

        return entity;
    }

    /// <summary>
    /// Получить все сущности.
    ///
    /// Автоматически загружает relationships.
    /// </summary>
    public virtual async Task<List<T>> GetAllAsync()
    {
        var list =
            await AsyncQuery().ToListAsync();

        // Batch loading relationships
        await list.LoadRelationshipsAsync();

        return list;
    }

    /// <summary>
    /// Получить несколько сущностей по ID.
    ///
    /// Использует SQL IN query.
    /// </summary>
    public virtual async Task<List<T>> GetManyAsync(
        List<Guid> ids)
    {
        var list = await AsyncQuery()
            .Where(x => ids.Contains(x.LocalId))
            .ToListAsync();

        // Batch loading relationships
        await list.LoadRelationshipsAsync();

        return list;
    }

    /// <summary>
    /// Получить AsyncTableQuery.
    ///
    /// Используется для LINQ-like запросов.
    /// </summary>
    public virtual AsyncTableQuery<T> AsyncQuery()
        => _db.Table<T>();

    // =====================================================
    // Raw SQL
    // =====================================================

    /// <summary>
    /// Выполнить raw SQL query.
    /// </summary>
    public async Task<int> ExecuteAsync(
        string sqlQuery,
        params object[] args)
    {
        return await _db.ExecuteAsync(
            sqlQuery,
            args);
    }
}