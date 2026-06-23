using DataAccessLayer;
using DataAccessLayer.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServicesLayer;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddKnowledgeBusinessServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IKnowledgeRepository>(_ =>
            new SqlKnowledgeRepository(configuration.GetConnectionString("DefaultConnection") ?? string.Empty));
        services.AddSingleton<IKnowledgeService, KnowledgeService>();

        return services;
    }
}