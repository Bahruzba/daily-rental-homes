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

`GET /api/rental-homes` returns only published, non-deleted homes and supports simple MVP search filters:

- `city`
- `district`
- `guests`
- `minPrice`
- `maxPrice`
- `startDate`
- `endDate`
- `q`

City and district are normalized exact matches. `guests` returns homes with `guest_count >= guests`. Price filters apply to `daily_price`. `q` performs a simple contains search over title, city, district, and description. `startDate` and `endDate` must be provided together in valid date format; the range is inclusive and returns only homes without overlapping manual availability blocks or blocking bookings. Cancelled and rejected bookings do not block public date availability; pending bookings still block, matching current booking creation behavior.

Invalid guest count, price ranges, one-sided date ranges, bad date format, or `startDate > endDate` return `400 Bad Request`.

### Bookings

- GET /api/bookings
- GET /api/bookings/{id}
- POST /api/bookings
- POST /api/bookings/{id}/status

`POST /api/bookings` accepts `rentalHomeId`, `name`, `phone`, `guests`, `dates[]`, and optional `note`. The backend loads the rental home's daily price, resolves the Pending status by its stable code, sorts the dates, and calculates the total amount. Duplicate dates in one request, dates blocked by non-cancelled bookings for the same home, and manual broker availability blocks return a validation error. Manual date ranges are inclusive.

### Broker dashboard

- GET /api/broker/summary
- GET /api/broker/rental-homes
- GET /api/broker/bookings
- GET /api/broker/bookings/{id}
- PATCH /api/broker/bookings/{id}/status
- PATCH /api/broker/bookings/{id}/accept
- PATCH /api/broker/bookings/{id}/reject
- PATCH /api/broker/bookings/{id}/cancel

Broker endpoints require a Broker or Admin JWT. Broker users only receive homes and bookings linked through `rental_homes.broker_user_id`; another broker's booking returns 404. Soft-deleted homes/bookings are not manageable through broker status actions.

Booking status lifecycle MVP:

- New customer bookings start as `pending`.
- Pending bookings can be accepted with `/accept`, rejected with `/reject`, or cancelled with `/cancel`.
- Accepting moves the booking to `confirmed`.
- Rejected and cancelled bookings cannot be accepted again.
- Confirmed and waiting-deposit bookings can be cancelled.
- Each broker status action writes `booking_status_history` and queues a `booking_status_changed` outbox record.
- Pending, waiting-deposit, paid, and confirmed bookings block availability. Rejected and cancelled bookings do not block future booking dates.

The generic broker status endpoint remains for backward compatibility and permits cancellation only. Deposit request/approval flow remains separate: accepting a booking does not automatically create a deposit, and requesting a deposit still moves an eligible booking to `waiting_deposit`. The legacy ID-based `POST /api/bookings/{id}/status` endpoint is Admin-only.

Broker booking detail (`GET /api/broker/bookings/{id}`) includes a nullable `cancellationRequest` summary when the booking has an active pending customer cancellation request. The summary contains `id`, `statusCode`, optional `reason`, and `createdAt`. This is read-only contract data for the broker UI; this PR does not add approve/reject cancellation-request workflow and does not automatically cancel bookings.

### Broker rental home management

Broker/Admin JWT endpoints:

- POST /api/broker/rental-homes
- GET /api/broker/rental-homes/{id}
- PUT /api/broker/rental-homes/{id}
- PATCH /api/broker/rental-homes/{id}/publish
- PATCH /api/broker/rental-homes/{id}/unpublish
- DELETE /api/broker/rental-homes/{id}
- POST /api/broker/rental-homes/{id}/media (`multipart/form-data`, field: `file`)
- DELETE /api/broker/rental-homes/{id}/media/{mediaId}
- PATCH /api/broker/rental-homes/{id}/media/{mediaId}/main
- GET /api/broker/rental-homes/{id}/availability-blocks
- POST /api/broker/rental-homes/{id}/availability-blocks
- DELETE /api/broker/rental-homes/{id}/availability-blocks/{blockId}

Broker users can only manage homes where `rental_homes.broker_user_id` matches their JWT user ID. Another broker receives 404 to avoid leaking ownership. Customer and unauthenticated users cannot access these endpoints.

Create/update accepts title, description, city, district, address, daily price, room count, guest count, and publication state. New homes default to draft/unpublished unless `isPublished` is explicitly sent. Public rental-home endpoints still return existing homes; published media can be used by the frontend as the main/card image.

Home images are stored in the existing `media_files` table with `file_type = HomeImage`. The first image for a home is assigned `sort_order = 0` and treated as the main image. Setting another image as main moves it to `sort_order = 0`. Upload accepts JPG, PNG, and WebP images up to 5 MB and stores development files under `src/DailyRentalHomes.Api/wwwroot/uploads/rental-homes/{homeId}`. Public URLs are returned as `/uploads/rental-homes/...`; local filesystem paths are not exposed.

Media type usage:

- `HomeImage` — rental home gallery/card image
- `CardImage` — reserved for future payment/card images
- `DepositReceipt` — customer deposit receipt upload
- `Other` — fallback/manual records

Availability blocks are stored in `rental_home_availability_blocks` with inclusive `start_date` and `end_date`. Broker notes are visible only through broker endpoints. Public rental-home detail returns unavailable ranges from manual broker blocks and active/non-cancelled bookings without exposing broker notes.

MVP limits: no private object storage, image resizing/compression, malware scan, magic-byte validation, full admin CRUD, owner onboarding, recurring availability rules, or advanced pricing yet.

### Booking deposit flow

Broker endpoints (Broker or Admin JWT, ownership-scoped for Broker):

- POST /api/broker/bookings/{bookingId}/deposit/request
- POST /api/broker/bookings/{bookingId}/deposit/approve
- POST /api/broker/bookings/{bookingId}/deposit/reject

Customer endpoints (Customer JWT, matched by booking customer/user or verified phone):

- GET /api/account/bookings
- GET /api/account/bookings/{id}
- POST /api/account/bookings/{id}/deposit/receipt (`multipart/form-data`, field: `file`)
- POST /api/account/bookings/{id}/cancellation-requests

Requesting a deposit creates one deposit per booking, stores only a masked card value, and moves a Pending booking to `waiting_deposit`. Customer account booking list/detail responses include booking status, selected dates, total amount, rental home city/district/main image, and deposit instructions when available. Customer-visible deposit data includes amount, deadline, status, card holder, masked PAN, bank name, broker instruction note, uploaded receipt, review note, and `allowReupload`; broker-only private availability notes are not exposed.

Receipt upload accepts JPG, PNG, or WebP images up to 5 MB and changes the deposit to `receipt_uploaded`. Customers can upload only for bookings matched to their account/user or verified phone. Upload is allowed only when the deposit is waiting for a receipt, or when it was rejected with `allowReupload = true`. Approval requires a receipt, sets the deposit to `approved`, and moves the booking to `confirmed`. Rejection keeps the booking in `waiting_deposit` and may allow a replacement upload. Legacy generic deposit/media write endpoints are Admin-only so Broker and Customer users cannot bypass this flow.

Customer cancellation requests are stored in `booking_cancellation_requests`. `POST /api/account/bookings/{id}/cancellation-requests` accepts optional JSON `{ "reason": "..." }`; reason is limited to 1000 characters. Customers can request cancellation only for their own active bookings in `pending`, `waiting_deposit`, `confirmed`, or `paid`. `completed`, `rejected`, and `cancelled` bookings return `400 Bad Request`. If the booking is not owned by the customer, the API returns the existing not-found behavior. A duplicate active `pending` cancellation request returns `400 Bad Request` with a readable message.

Submitting a cancellation request does not change the booking status, does not release availability, does not refund, and does not change deposit state. Account booking detail responses include `cancelRequestSent = true` when an active pending request exists. A new request queues a `booking_cancellation_requested` notification outbox record for the broker contact using the existing outbox pattern; there is still no automatic delivery worker/provider in this MVP.

Customer-visible status meanings:

- `pending` — broker confirmation is still pending.
- `confirmed` — broker accepted the booking.
- `rejected` / `cancelled` — booking is no longer active and no customer action is required.
- Deposit `requested` — customer should upload a receipt.
- Deposit `receipt_uploaded` — broker is reviewing the receipt.
- Deposit `approved` — deposit was accepted.
- Deposit `rejected` — customer may re-upload only when `allowReupload` is true.

Development receipts are stored under `src/DailyRentalHomes.Api/wwwroot/uploads/deposit-receipts` and served from `/uploads/deposit-receipts/...`. This local file storage is an MVP implementation; production requires private object storage, authorization-aware downloads, malware/content validation, retention rules, and encryption. There is no payment gateway or real SMS/WhatsApp provider. Full card PAN must never be stored; the API requires a masked value containing `*`.

### Broker booking expenses

Broker/Admin JWT endpoints:

- GET /api/broker/bookings/{bookingId}/expenses
- POST /api/broker/bookings/{bookingId}/expenses
- PUT /api/broker/bookings/{bookingId}/expenses/{expenseId}
- DELETE /api/broker/bookings/{bookingId}/expenses/{expenseId}

Broker users can manage expenses only for bookings linked to their own rental homes. Another broker receives 404, and soft-deleted bookings/homes are not manageable. Delete uses the existing soft-delete pattern.

Expense fields:

- `bookingId`
- `typeCode` such as `cleaning`, `owner_payout`, `utility`, `repair`, or `other`
- `title`
- `amount`
- optional `note`
- audit and soft-delete fields from the shared BaseEntity pattern

Validation requires booking, type, title, and amount greater than zero. The current scope is storage and broker CRUD only; report summary endpoints and frontend UI will come in later PRs.

### Broker report summary

Broker/Admin JWT endpoint:

- GET /api/broker/reports/summary?from=YYYY-MM-DD&to=YYYY-MM-DD

Broker users receive report totals only for bookings linked to their own rental homes. Admin users receive totals for all bookings. Customer and unauthenticated users cannot access the endpoint.

Date parameters are optional. If no date range is provided, the summary uses all available booking data in the caller's scope. If a range is provided, both `from` and `to` are required, the range is inclusive, and a booking is included when at least one non-deleted booking date falls inside the range. One-sided ranges or `from > to` return `400 Bad Request`.

Revenue totals exclude `rejected` and `cancelled` bookings. The current revenue statuses are:

- `pending`
- `waiting_deposit`
- `confirmed`
- `paid`
- `completed`

`totalBookingAmount` sums each included revenue booking's total amount once, not once per booking date. Expenses are grouped from non-deleted expenses belonging to the included bookings:

- `totalCleaningCost` uses `typeCode = cleaning`
- `totalOwnerPayout` uses `typeCode = owner_payout`
- `totalOtherExpenses` includes all other expense type codes

`estimatedProfit` is calculated as `totalBookingAmount - totalExpenses`. Soft-deleted bookings, rental homes, booking dates, and expenses are ignored. There is no frontend report dashboard UI yet.

### Deposits

- GET /api/deposits
- GET /api/deposits/{id}
- POST /api/deposits
- POST /api/deposits/{id}/status
- POST /api/deposits/{id}/reminder

### Messages

- GET /api/messages
- POST /api/messages

### Notification outbox

- GET /api/admin/notifications (Admin JWT)

The existing `outbound_messages` table is reused as the MVP notification outbox. Booking creation, customer cancellation request, deposit request, receipt upload, deposit approval/rejection, and broker booking-status changes create `pending` records; no real WhatsApp or SMS provider sends these notifications yet. Records use stable channel (`whatsapp`, `sms`, `in_app`), type, and status (`pending`, `sent`, `failed`, `cancelled`, `skipped`) codes. The Admin endpoint supports optional `status`, `type`, and `bookingId` filters.

When a deposit is requested, an immediate `deposit_requested` record is created. If the deadline is more than three hours away, `deposit_deadline_reminder` is scheduled two hours before it. If it is between 30 minutes and three hours away, the reminder is scheduled 30 minutes before it; closer deadlines skip the reminder.

Production still requires a real WhatsApp/SMS provider worker, retry/backoff and idempotency strategy, delivery receipts, rate limits, observability, retention, and protection of recipient/payload personal data.

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
