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

Frontend login route is `/login`. Mock mode uses OTP `123456` and allows selecting Admin, Broker, or Customer. Live mode obtains the role from the backend and redirects to `/admin`, `/broker`, or `/account`. The MVP stores the JWT session in `localStorage`; this must be revisited before production use.

Broker users can manage their own rental homes from `/broker`: create/edit draft homes, publish/unpublish, upload JPG/PNG/WebP images up to 5 MB, set the main image, and delete images. In live mode this uses `/api/broker/rental-homes...` endpoints with the Broker JWT. Uploaded development images are served from `/uploads/rental-homes/...`; production can switch the backend storage provider from Local to S3-compatible object storage with `FileStorage__Provider=S3`.

Broker users can also block unavailable date ranges for their own homes. Public detail exposes unavailable ranges without broker notes, and booking creation rejects blocked or already-booked dates.

Booking lifecycle MVP: new customer bookings start as `pending`. A broker can accept (`confirmed`), reject (`rejected`), or cancel (`cancelled`) their own bookings from the broker booking detail screen. Pending and confirmed bookings block the same dates from being booked again; rejected and cancelled bookings do not block availability. Deposit requests remain a separate broker action and are not created automatically when a booking is accepted.

Customer account MVP: customers can open `/account` to see their own bookings, status, selected dates, total amount, rental home summary, and beh/deposit state. Booking detail shows the next required action, deposit instructions, uploaded receipt link, broker review note, and re-upload option when a rejected receipt allows it.

Public search MVP: the homepage can filter published rental homes by keyword, city, district, guest capacity, daily price range, and an available date range. Date availability excludes homes with overlapping manual broker blocks or blocking bookings; rejected and cancelled bookings do not block dates, while pending behavior follows the existing backend blocking rule.

## Development Status

The repository contains the backend API and a frontend MVP. The frontend uses mock data by default and can be switched to the live API for integration testing.

## CI quality gates

GitHub Actions runs `CI Quality Gates` on pull requests targeting `main` and on pushes to `main`. The workflow uses minimal `contents: read` permissions and cancels outdated pull-request runs when a newer commit is pushed to the same PR.

Jobs:

- `Backend` uses the .NET SDK from `backend/global.json`, restores `backend/DailyRentalHomes.slnx`, builds Release, and runs the backend test suite in Release.
- `Frontend` uses Node.js `22.12.0`, runs `npm ci`, and builds `clients/web-app`.
- `Migrations` restores backend tools and generates an idempotent EF Core migration SQL script to a temporary runner path, then verifies the script is non-empty and includes EF migration history markers.

CI intentionally uses the repository's test/default configuration and does not contact production services. It does not require Meta WhatsApp credentials, AWS/S3 credentials, production JWT secrets, production database credentials, Docker publishing, or deployment infrastructure. Recommended branch protection checks are `Backend`, `Frontend`, and `Migrations`.

## Production security

Before production deployment, review [docs/production-security-checklist.md](docs/production-security-checklist.md). It covers JWT/database/Meta/S3 secrets, private deposit receipts, upload limitations, CORS, least-privilege storage permissions, and remaining MVP risks.

Manual production deployment is documented in [docs/production-deployment.md](docs/production-deployment.md). The GitHub Actions workflow is manually triggered, packages backend/frontend/migration artifacts, and can deploy to a configured Linux VPS over SSH with Docker Compose.
