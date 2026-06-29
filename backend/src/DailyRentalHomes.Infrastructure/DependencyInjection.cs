using DailyRentalHomes.Application.Abstractions.Messaging;
using DailyRentalHomes.Application.Abstractions.Persistence;
using DailyRentalHomes.Infrastructure.Messaging;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DailyRentalHomes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IMessageSender, DevelopmentMessageSender>();

        return services;
    }
}
