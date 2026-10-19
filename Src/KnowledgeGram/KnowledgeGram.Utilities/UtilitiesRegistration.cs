using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeGram.Utilities;

public static class UtilitiesRegistration
{
    public static IServiceCollection AddUtilities(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUserSession, InMemoryUserSession>();   // scoped = one per browser connection
        return services;
    }
}
