using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Tests;

internal static class TestFileStorageFactory
{
    public static IFileStorage Create(IWebHostEnvironment environment) =>
        new LocalFileStorage(
            Options.Create(new FileStorageOptions
            {
                Provider = "Local",
                Local = new LocalFileStorageOptions
                {
                    RootPath = "uploads",
                    PublicBasePath = "/uploads"
                }
            }),
            environment);
}
