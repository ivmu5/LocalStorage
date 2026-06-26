using SQLite;

namespace SQLiteStorage;

public interface IRepository
{
    Task<string> GetTableNameAsync();
    Task<TableMapping> GetTableMappingAsync();

    Task<int> SaveAsync(object entity);
    Task<int> DeleteAsync(object entity);

    Task<List<object>> GetAllAsync();
    Task<object> GetAsync(Guid id);

    Task<int> ExecuteAsync(string sqlQuery, params object[] args);

    Task<List<object>> GetManyAsync(List<Guid> ids);
}