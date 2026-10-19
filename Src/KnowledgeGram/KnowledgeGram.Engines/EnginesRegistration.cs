using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeGram.Engines;

public static class EnginesRegistration
{
    public static IServiceCollection AddEngines(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PolicyOptions>(configuration.GetSection(PolicyOptions.SectionName));

        services.AddSingleton<IPolicyEngine, PolicyEngine>();
        services.AddSingleton<IFeedEngine, FeedEngine>();
        services.AddSingleton<IMediaEngine, MediaEngine>();
        return services;
    }
}
