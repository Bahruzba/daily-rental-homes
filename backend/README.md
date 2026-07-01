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
dotnet run --project src/DailyRentalHomes.Api/DailyRentalHomes.Api.csproj --urls http://127.0.0.1:5099
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

## Build Scripts

```bash
build.cmd
build.ps1
build.sh
```

## MVP Endpoints

### Health

- GET /api/health

### Auth

- POST /api/auth/send
- POST /api/auth/confirm

### Rental Homes

- GET /api/rental-homes
- GET /api/rental-homes/{id}
- POST /api/rental-homes
- PUT /api/rental-homes/{id}
- DELETE /api/rental-homes/{id}

### Bookings

- GET /api/bookings
- GET /api/bookings/{id}
- POST /api/bookings
- POST /api/bookings/{id}/status

### Deposits

- GET /api/deposits
- GET /api/deposits/{id}
- POST /api/deposits
- POST /api/deposits/{id}/status
- POST /api/deposits/{id}/reminder

### Messages

- GET /api/messages
- POST /api/messages

### Dictionaries and related data

- GET /api/payment-cards
- POST /api/payment-cards
- PUT /api/payment-cards/{id}
- DELETE /api/payment-cards/{id}
- GET /api/media-files
- POST /api/media-files
- DELETE /api/media-files/{id}
- GET /api/contacts
- POST /api/contacts
- DELETE /api/contacts/{id}
- GET /api/amenities
- POST /api/amenities
- PUT /api/amenities/{id}
- DELETE /api/amenities/{id}
- GET /api/booking-statuses
- GET /api/deposit-reminders/due
