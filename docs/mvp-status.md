# MVP Status

## Ready in repository

- .NET 10 backend structure
- MSSQL EF Core Code First setup
- Clean Architecture folders
- Domain entities
- DbContext
- EF configurations
- Seed helper
- API-first structure
- Auth local test flow
- Rental homes API
- Bookings API
- Deposits API
- Media files API
- Contacts API
- Amenities API
- Payment cards API
- Booking statuses API

## MVP API endpoints

- POST /api/auth/send
- POST /api/auth/confirm
- GET /api/rental-homes
- GET /api/rental-homes/{id}
- POST /api/rental-homes
- GET /api/bookings
- GET /api/bookings/{id}
- POST /api/bookings
- POST /api/bookings/{id}/status
- GET /api/deposits
- GET /api/deposits/{id}
- POST /api/deposits
- GET /api/media-files
- POST /api/media-files
- GET /api/contacts
- POST /api/contacts
- GET /api/amenities
- POST /api/amenities
- GET /api/payment-cards
- POST /api/payment-cards
- GET /api/booking-statuses

## Still needed before real production

- Build locally and fix compiler errors if any.
- Create EF migration.
- Replace temporary token with signed JWT.
- Replace development PIN response with SMS provider.
- Add authorization policies.
- Add validation.
- Add frontend web app.
