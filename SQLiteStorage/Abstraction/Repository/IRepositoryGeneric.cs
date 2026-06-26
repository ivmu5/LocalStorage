using SQLite;
using System.Linq.Expressions;

namespace SQLiteStorage;

public interface IRepository<T> : IRepository
    where T : class, new()
{
    Task<int> SaveAsync(T entity);
    async Task<int> IRepository.SaveAsync(object entity)
    {
        return await SaveAsync((T)entity);
    }
    Task<int> DeleteAsync(T entity);
    async Task<int> IRepository.DeleteAsync(object entity)
    {
        return await DeleteAsync((T)entity);
    }

    new Task<List<T>> GetAllAsync();
    async Task<List<object>> IRepository.GetAllAsync()
    {
        var entities = await GetAllAsync();
        return entities.Cast<object>().ToList();
    }
    new Task<T> GetAsync(Guid id);
    async Task<object> IRepository.GetAsync(Guid id)
    {
        return await GetAsync(id);
    }

    AsyncTableQuery<T> AsyncQuery();

    new Task<int> ExecuteAsync(string sqlQuery, params object[] args);
    async Task<int> IRepository.ExecuteAsync(string sqlQuery, params object[] args)
    {
        return await ExecuteAsync(sqlQuery, args);
    }
    Task<int> UpdateColumnValueAsync(Expression<Func<T, object>> selector, object value);

    new Task<List<T>> GetManyAsync(List<Guid> ids);
    async Task<List<object>> IRepository.GetManyAsync(List<Guid> ids)
    {
        var entities = await GetManyAsync(ids);
        return entities.Cast<object>().ToList();
    }
}