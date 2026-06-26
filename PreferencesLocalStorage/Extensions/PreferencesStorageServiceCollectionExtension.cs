using LocalStorage;

namespace PreferencesLocalStorage;

public static class PreferencesStorageServiceCollectionExtension
{
    public static IServiceCollection AddPreferencesStorage(this IServiceCollection services)
    {
        services.AddSingleton<ILocalStorage, PreferencesLocalStorage>();
        return services;
    }
}
