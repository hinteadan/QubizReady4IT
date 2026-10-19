using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeGram.Managers;

public static class ManagersRegistration
{
    public static IServiceCollection AddManagers(this IServiceCollection services)
    {
        services.AddScoped<IContentManager, ContentManager>();   // scoped: it uses the per-connection user session
        return services;
    }
}
