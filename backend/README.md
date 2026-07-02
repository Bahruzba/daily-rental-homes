# Backend

## Requirements

- .NET 10 SDK
- Microsoft SQL Server

## Projects

- DailyRentalHomes.Api
- DailyRentalHomes.Application
- DailyRentalHomes.Domain
- DailyRentalHomes.Infrastructure

## Build

```bash
dotnet restore DailyRentalHomes.slnx
dotnet build DailyRentalHomes.slnx
```

## Run locally

The API must use the Development environment so that the local JWT key is loaded from `appsettings.Development.json`.

Windows PowerShell:

```powershell
cd src/DailyRentalHomes.Api
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --urls http://127.0.0.1:5099
```

Bash:

```bash
cd src/DailyRentalHomes.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5099
```

The local API is available at `http://127.0.0.1:5099`. The included launch profile also selects Development and port `5099` for a plain `dotnet run` from the API project directory.

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

Development rejimində `/api/auth/send` telefon nömrəsi üçün 5 dəqiqəlik OTP yaradır və lokal yoxlama üçün cavabda `devPin` qaytarır. Bu sahə production cavabına daxil edilmir. `/api/auth/confirm` uğurlu olduqda `accessToken`, `expiresAt` və `user` məlumatlarını (`id`, `fullName`, `phone`, `role`) qaytarır. Mövcud Admin/Broker/Customer istifadəçinin rolu qorunur; yeni telefon nömrəsi Customer kimi yaradılır. Real SMS/WhatsApp provayderi bu MVP-yə daxil deyil.

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

`POST /api/bookings` accepts `rentalHomeId`, `name`, `phone`, `guests`, `dates[]`, and optional `note`. The backend loads the rental home's daily price, resolves the Pending status by its stable code, sorts the dates, and calculates the total amount. Duplicate dates in one request and dates blocked by non-cancelled bookings for the same home return a validation error.

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
