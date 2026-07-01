# Daily Rental Homes

Daily Rental Homes is a short-term rental platform for listing, searching, and booking daily rental properties.

## Tech Stack

- Backend: .NET 10 Web API
- Frontend: React + TypeScript + Vite
- Database: Microsoft SQL Server
- ORM: Entity Framework Core
- Auth: OTP + JWT
- Architecture: Clean Architecture

## Planned Modules

- Authentication with OTP
- Admin, broker, and customer roles
- Rental home listings
- Media files for homes, cards, and deposit receipts
- Related contacts with phone/WhatsApp types
- Booking date list instead of only check-in/check-out
- Booking deposits with deadline and receipt tracking
- Booking statuses and status history
- Notifications

## Repository Structure

```text
backend/
  src/
    DailyRentalHomes.Api/
    DailyRentalHomes.Application/
    DailyRentalHomes.Domain/
    DailyRentalHomes.Infrastructure/
clients/
  web-app/
database/
  mssql/
docs/
```

## Local Development

Start the backend API in Development mode. In Windows PowerShell:

```powershell
cd backend/src/DailyRentalHomes.Api
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --urls http://127.0.0.1:5099
```

In bash:

```bash
cd backend/src/DailyRentalHomes.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5099
```

The local API is available at `http://127.0.0.1:5099`. The Development environment loads the local JWT key from `appsettings.Development.json`.

Start the frontend in mock mode (the default):

```bash
cd clients/web-app
npm install
npm run dev
```

For live API mode, create `clients/web-app/.env.local`:

```env
VITE_USE_LIVE_API=true
VITE_API_BASE_URL=
```

An empty base URL uses the Vite `/api` proxy to `http://127.0.0.1:5099`.

## Development Status

The repository contains the backend API and a frontend MVP. The frontend uses mock data by default and can be switched to the live API for integration testing.
