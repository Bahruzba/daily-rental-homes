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

Install EF tool:

```bash
dotnet tool install --global dotnet-ef
```

Create migration:

```bash
dotnet ef migrations add InitialCreate --project src/DailyRentalHomes.Infrastructure --startup-project src/DailyRentalHomes.Api
```

Update database:

```bash
dotnet ef database update --project src/DailyRentalHomes.Infrastructure --startup-project src/DailyRentalHomes.Api
```

## MVP Endpoints

- GET /api/health
- POST /api/auth/send
- POST /api/auth/confirm
- GET /api/rental-homes
- POST /api/rental-homes
- GET /api/bookings
- POST /api/bookings
- GET /api/deposits
- POST /api/deposits
- GET /api/payment-cards
- POST /api/payment-cards
- GET /api/media-files
- POST /api/media-files
- GET /api/contacts
- POST /api/contacts
- GET /api/amenities
- POST /api/amenities
- GET /api/booking-statuses
