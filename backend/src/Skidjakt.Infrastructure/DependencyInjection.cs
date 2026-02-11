using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skidjakt.Core.Interfaces;
using Skidjakt.Infrastructure.Data;

namespace Skidjakt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=data/skidjakt.db";

        services.AddDbContext<SkidjaktDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IDealRepository, DealRepository>();

        return services;
    }
}
