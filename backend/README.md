# Backend

## Requirements

- .NET 10 SDK
- Microsoft SQL Server

## Projects

- DailyRentalHomes.Api
- DailyRentalHomes.Application
- DailyRentalHomes.Domain
- DailyRentalHomes.Infrastructure

## Run

```bash
dotnet restore DailyRentalHomes.slnx
dotnet build DailyRentalHomes.slnx
dotnet run --project src/DailyRentalHomes.Api/DailyRentalHomes.Api.csproj
```

## Database

Connection string is in:

```text
src/DailyRentalHomes.Api/appsettings.json
```

Default database name:

```text
DailyRentalHomes
```

## EF Core Code First

Migration commands will be added after the first compile check.
