using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeGram.ResourceAccess;

public static class ResourceAccessRegistration
{
    public static IServiceCollection AddResourceAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        services.AddSingleton<IPostAccess, SqlitePostAccess>();
        services.AddSingleton<IFileAccess, LocalFileAccess>();
        services.AddHostedService<SqliteDatabaseInitializer>();
        return services;
    }
}
