using FoodProducts.Application.Abstractions;
using FoodProducts.Infrastructure.Data;
using FoodProducts.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodProducts.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "FoodProductsDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        }

        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<IFoodProductRepository, FoodProductRepository>();
        return services;
    }
}
