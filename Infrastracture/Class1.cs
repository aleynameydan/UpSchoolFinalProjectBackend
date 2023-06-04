using Application.Common.Interfaces;
using Infrastracture.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastracture;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration, string wwwrootPath)
    {

        var connectionString = configuration.GetConnectionString("MariaDB")!;

        // DbContext
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Scoped Services

        // Singleton Services

        return services;
    }
}