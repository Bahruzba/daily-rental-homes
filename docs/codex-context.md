# Codex Context - Daily Rental Homes

This file is the handoff context for Codex. The previous product, API, database and design decisions were discussed in ChatGPT, but Codex cannot see that chat. Use this document and the other files in `/docs` as the source of truth.

## Repository

- Repository: `Bahruzba/daily-rental-homes`
- Default branch: `main`
- Backend path: `backend/`
- Future clients path: `clients/`

## Main Goal

Build an MVP for a daily rental homes platform.

The first priority is a working backend MVP. Do not start frontend until backend build, EF model and initial migration are clean.

## Architecture

- Backend: .NET 10 Web API
- Database: Microsoft SQL Server
- ORM: EF Core Code First
- Architecture: API-first, clean architecture style
- Projects:
  - `DailyRentalHomes.Api`
  - `DailyRentalHomes.Application`
  - `DailyRentalHomes.Domain`
  - `DailyRentalHomes.Infrastructure`
- Future clients:
  - `clients/web-app`
  - `clients/mobile-app`
- Website and mobile app must consume the same API.
- API must return JSON only. Do not put UI logic in the backend.

## Build Rules for Codex

When working in Codex:

1. Read this file first.
2. Read the files in `/docs` and `backend/README.md`.
3. Run backend restore/build before and after meaningful code changes.
4. Fix compile errors yourself.
5. Do not wait for confirmation for backend MVP work.
6. Keep business decisions from this file unless the user explicitly changes them.
7. Prefer small safe commits/changes.
8. Do not redesign the product from scratch.

Useful commands:

```bash
cd backend
dotnet restore DailyRentalHomes.slnx
dotnet build DailyRentalHomes.slnx --configuration Release --no-restore
```

EF commands:

```bash
cd backend
dotnet ef migrations add InitialCreate --project src/DailyRentalHomes.Infrastructure --startup-project src/DailyRentalHomes.Api
dotnet ef database update --project src/DailyRentalHomes.Infrastructure --startup-project src/DailyRentalHomes.Api
```

## Business Roles

The system has three roles:

- Admin
- Broker / Makler
- Customer / Musteri

Customers may not use the system as a full listing marketplace at first, but the platform must support customer continuation from a shared listing or booking link.

## Authentication

- Admin logs in with OTP.
- Broker logs in with OTP.
- Customer logs in with OTP.
- No passwords for admin or broker.
- OTP is the MVP login method.
- Current backend has a temporary access token builder. Replace it with signed JWT when possible.
- Add role claims for Admin, Broker and Customer.

## Messaging

- Main notification channel: WhatsApp.
- SMS is fallback/future secondary channel.
- Keep a provider abstraction so real WhatsApp/SMS providers can be added later.
- Outgoing messages must be logged.
- OTP, deposit reminders and booking notifications should use the common messaging flow.

## Core Product Flow

1. Broker/admin creates a rental home.
2. Broker adds home images, contacts, amenities and payment card/deposit details.
3. Broker shares home or booking link with customer.
4. Customer enters booking dates and contact details.
5. Booking is created with a status.
6. If deposit is required, customer receives payment card/PAN info and deadline.
7. Customer can upload deposit receipt image.
8. Broker can mark deposit paid, returned, not returned, expired or cancelled.
9. Customer and broker receive reminders before deposit deadline.
10. Broker can extend deposit deadline.

## UX Direction

Design inspiration: `https://gunlukevler.az/`

Expected UX direction:

- Homepage listing grid.
- Search/filter area similar to that site.
- Listing cards with image, price, location, room/guest info.
- Detail page with gallery, contacts and booking action.
- Broker/admin panel later for managing homes, bookings and deposits.

## Database Naming Rules

- Use snake_case table and column names in the database.
- Do not prefix every table with `rental_unit`.
- Do not use `unit` everywhere.
- Table names should be clear and direct, for example:
  - `rental_homes`
  - `media_files`
  - `related_contacts`
  - `bookings`
  - `booking_dates`
  - `booking_deposits`
  - `booking_statuses`
  - `booking_status_history`
  - `payment_cards`
  - `outbound_messages`
- C# entity names should stay PascalCase.
- API JSON should be camelCase.

## Media Files

All images and files are stored in `media_files`.

Use `file_type` to distinguish:

- HomeImage
- CardImage
- DepositReceipt
- Other

Examples:

- Home photos are `HomeImage`.
- Payment card image is `CardImage`.
- Customer uploaded deposit proof is `DepositReceipt`.

## Contacts

Use `related_contacts`.

Phone and WhatsApp should be kept in the same `value` column.

Use `contact_type` to distinguish:

- Phone
- WhatsApp
- Telegram
- Other

Use one notification flag:

- `notify_enabled`

Do not split notification flags into many columns.

## Bookings

Bookings should use a date list, not only `check_in` and `check_out`.

Use:

- `bookings`
- `booking_dates`

A booking can contain multiple dates.

`nights_count` can be computed from booking dates or added as a future snapshot if needed. Do not overcomplicate it now.

Booking source can be stored as a simple string for MVP, for example:

- `web`
- `admin`
- `broker_link`
- `mobile`

## Booking Status

Booking status is separate from the booking row.

Use:

- `booking_statuses`
- `booking_status_history`

Store status changes with note and changed by user when available.

Seed basic statuses:

- Pending
- WaitingDeposit
- Paid
- Confirmed
- Cancelled
- Completed

## Deposit / Beh

Deposit is separate because it has many fields and business rules.

Use:

- `booking_deposits`

Rules:

- Deposit amount is stored separately.
- Deposit status is separate from booking status.
- Deposit can have a deadline.
- Customer can upload receipt image.
- Broker can extend deadline.
- Broker decides whether to return deposit or not.
- Deposit status examples:
  - NotRequired
  - Waiting
  - Paid
  - Expired
  - Returned
  - NotReturned
  - Cancelled

When waiting for deposit, the system should be able to send payment card/PAN information to the customer.

For safety, store masked PAN for now unless a future secure storage decision is made.

## Existing Backend State

The backend already contains:

- Clean-ish project structure.
- Domain entities for users, homes, media, contacts, amenities, bookings, deposits, statuses, payment cards and outbound messages.
- EF Core DbContext.
- Several EF configuration files.
- Controllers for:
  - auth
  - rental homes
  - bookings
  - deposits
  - messages
  - payment cards
  - media files
  - contacts
  - amenities
  - booking statuses
  - deposit reminders
- API response wrapper.
- Basic validation helper.
- Development message sender.
- Build scripts.
- GitHub Actions workflow.

## Immediate Backend Tasks

Continue with these tasks in order:

1. Run restore/build.
2. Fix all compile errors.
3. Review EF relationships and add missing mappings.
4. Add global snake_case mapping for table and column names if not already fully handled.
5. Ensure soft delete fields are handled consistently.
6. Generate InitialCreate migration.
7. Fix migration errors.
8. Replace temporary Base64 token with signed JWT.
9. Add authentication and role-based authorization foundation.
10. Keep OTP login flow.
11. Keep WhatsApp as primary messaging channel.
12. Add tests only after backend build and migration are stable.
13. Do not start frontend until backend MVP is stable.

## Important Constraints

- Do not add passwords.
- Do not remove OTP login.
- Do not merge broker and admin roles.
- Do not use only check-in/check-out for booking dates.
- Do not scatter images across different tables.
- Do not store home photos outside `media_files`.
- Do not rename the whole domain without reason.
- Do not start a heavy marketplace/customer browsing feature before MVP backend is stable.

## Suggested First Codex Task

Use this exact task when starting Codex:

```text
Read docs/codex-context.md and the other docs files first.
Then work only on backend.
Run restore/build for backend/DailyRentalHomes.slnx.
Fix all compile errors.
Complete EF relationships and snake_case mapping.
Generate InitialCreate migration.
Do not start frontend.
Do not wait for confirmation.
Run build after each meaningful change.
```
