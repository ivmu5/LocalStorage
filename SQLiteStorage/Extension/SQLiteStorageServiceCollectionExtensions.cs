using LocalStorage;
using SQLite;

namespace SQLiteStorage;

/// <summary>
/// DI registration для SQLite storage.
/// </summary>
public static class SQLiteStorageServiceCollectionExtensions
{
    public static IServiceCollection AddSQLiteStorage(
        this IServiceCollection services,
        SQLiteStorageOptions options)
    {
        // =====================================
        // SQLite connection
        // =====================================
        var connection =
            new SQLiteAsyncConnection(options.DatabasePath);

        services.AddSingleton(connection);

        // =====================================
        // Core services
        // =====================================
        services.AddSingleton<ILocalStorage, SQLiteLocalStorage>();
        services.AddSingleton(typeof(IInstanceRepository<>), typeof(InstanceStore<>));

        // =====================================
        // Ensure tables exist
        // =====================================
        var entityTypes =
            options.EntityTypes ?? new List<Type>();

        Task.Run(async () =>
        {
            var tasks = entityTypes
                .Select(t => connection.CreateTableAsync(t));

            await Task.WhenAll(tasks);
        }).GetAwaiter().GetResult();

        // =====================================
        // Register repositories
        // =====================================
        foreach (var type in entityTypes)
        {
            var baseType = type.BaseType;

            if (baseType == null ||
                !baseType.IsGenericType)
            {
                continue;
            }

            var openGeneric = baseType
                .GetGenericTypeDefinition();

            if (!options.SortTypes.TryGetValue(
                    openGeneric,
                    out var repoOpenType))
            {
                continue;
            }

            var repoType =
                repoOpenType.MakeGenericType(type);

            var repo =
                Activator.CreateInstance(
                    repoType,
                    connection)!;

            // Register in DI
            services.AddSingleton(
                repoType,
                repo);

            // Register in repository registry
            typeof(RepositoryRegistry)
                .GetMethod(nameof(RepositoryRegistry.Register))!
                .MakeGenericMethod(type)
                .Invoke(null, new object[] { repo });
        }

        return services;
    }
}