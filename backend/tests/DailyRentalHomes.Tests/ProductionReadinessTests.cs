using DailyRentalHomes.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DailyRentalHomes.Tests;

public sealed class ProductionReadinessTests
{
    [Fact]
    public void InfrastructureRequiresDefaultConnectionString()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddInfrastructure(configuration));

        Assert.Contains("DefaultConnection", exception.Message);
    }
}
