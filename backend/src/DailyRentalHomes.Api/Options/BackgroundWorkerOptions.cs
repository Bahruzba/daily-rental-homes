namespace DailyRentalHomes.Api.Options;

public sealed class BackgroundWorkerOptions
{
    public const string SectionName = "BackgroundWorkers";

    public DistributedLockingOptions DistributedLocking { get; set; } = new();
}

public sealed class DistributedLockingOptions
{
    public bool Enabled { get; set; } = true;
    public int LeaseSeconds { get; set; } = 120;
}
