# Daily Rental Homes

Daily Rental Homes is a short-term rental platform for listing, searching, and booking daily rental properties.

## Tech Stack

- Backend: .NET 8 Web API
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
database/
  mssql/
docs/
```

## Development Status

The project is in the initial backend setup stage.
