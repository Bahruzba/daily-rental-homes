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

## Configuration

Important environment variables:

- `ASPNETCORE_ENVIRONMENT` — use `Development` locally and `Production` for container/production runs.
- `ASPNETCORE_URLS` — listening URL, for example `http://+:8080` in Docker.
- `ConnectionStrings__DefaultConnection` — SQL Server connection string used by EF Core.
- `Token__Issuer`
- `Token__Audience`
- `Token__Key` — must be at least 32 bytes and must be replaced with a secure secret outside local development.
- `Token__Minutes`
- `Notifications__WorkerEnabled` — defaults to `false`.
- `Notifications__PollSeconds` — defaults to `30`.
- `Notifications__BatchSize` — defaults to `20`.
- `NotificationDelivery__Provider` — selects the outbox delivery provider. Supported values are `Fake` and `MetaWhatsApp`; the default is `Fake`.
- `NotificationDelivery__MetaWhatsApp__PhoneNumberId` — Meta WhatsApp Cloud API phone number id, required only when `Provider=MetaWhatsApp`.
- `NotificationDelivery__MetaWhatsApp__AccessToken` — Meta Graph API access token, required only when `Provider=MetaWhatsApp`; never commit real tokens.
- `NotificationDelivery__MetaWhatsApp__ApiVersion` — Meta Graph API version such as `v22.0`, required only when `Provider=MetaWhatsApp`.
- `NotificationDelivery__MetaWhatsApp__WebhookVerifyToken` — Meta webhook verification token used by `GET /api/webhooks/meta-whatsapp`; never commit real tokens.
- `NotificationDelivery__MetaWhatsApp__AppSecret` — Meta App Secret used to validate `X-Hub-Signature-256` on webhook POST requests; required when `Provider=MetaWhatsApp`; never commit real secrets.
- `FileStorage__Provider` — uploaded-file storage provider. Supported values are `Local` and `S3`; default is `Local`.
- `FileStorage__Local__RootPath` — local storage root. Relative values are resolved under the API web root; default is `uploads`.
- `FileStorage__Local__PrivateRootPath` — local private storage root. Relative values are resolved under the API content root; default is `private-uploads`.
- `FileStorage__Local__PublicBasePath` — public URL base for local files; default is `/uploads`.
- `FileStorage__S3__BucketName` — S3 bucket name, required only when `FileStorage__Provider=S3`.
- `FileStorage__S3__Region` — AWS region such as `eu-central-1`; optional when `ServiceUrl` is used by an S3-compatible provider.
- `FileStorage__S3__ServiceUrl` — optional S3-compatible endpoint such as MinIO or another object-storage service.
- `FileStorage__S3__AccessKey` / `FileStorage__S3__SecretKey` — optional explicit credentials; provide both together or use the normal AWS credential chain. Never commit real secrets.
- `FileStorage__S3__PublicBaseUrl` — optional public/CDN base URL used to build public rental-home media URLs.
- `FileStorage__S3__ForcePathStyle` — set `true` for providers that require path-style bucket URLs.

This backend currently uses Entity Framework Core SQL Server provider. The development compose file therefore uses SQL Server. Switching to PostgreSQL would require a separate provider/migration compatibility task.

## File storage

Uploaded rental-home media and deposit receipt files go through the shared `IFileStorage` abstraction. The default implementation is `LocalFileStorage`, configured by:

```json
"FileStorage": {
  "Provider": "Local",
  "Local": {
    "RootPath": "uploads",
    "PrivateRootPath": "private-uploads",
    "PublicBasePath": "/uploads"
  }
}
```

For the default local configuration, public rental-home media is saved below `src/DailyRentalHomes.Api/wwwroot/uploads` in development and exposed with `/uploads/...` URLs, preserving existing public media behavior. Private deposit receipts are saved below `src/DailyRentalHomes.Api/private-uploads` and are not served by static-file middleware. Storage keys are normalized relative paths; traversal-style keys such as `../file` or absolute paths are rejected so upload code cannot escape the configured root. Delete operations are idempotent for already-missing local files.

S3-compatible object storage can be selected with:

```json
"FileStorage": {
  "Provider": "S3",
  "S3": {
    "BucketName": "daily-rental-homes",
    "Region": "eu-central-1",
    "ServiceUrl": "",
    "AccessKey": "",
    "SecretKey": "",
    "PublicBaseUrl": "https://cdn.example.com",
    "ForcePathStyle": false
  }
}
```

Equivalent environment-variable configuration:

```bash
FileStorage__Provider=S3
FileStorage__S3__BucketName=daily-rental-homes
FileStorage__S3__Region=eu-central-1
FileStorage__S3__PublicBaseUrl=https://cdn.example.com
FileStorage__S3__ForcePathStyle=false
```

For MinIO or other compatible providers, set `FileStorage__S3__ServiceUrl` and usually `FileStorage__S3__ForcePathStyle=true`. When explicit `AccessKey` and `SecretKey` are omitted, the AWS SDK uses its normal environment/profile/instance credential chain. The application does not create buckets or bucket policies; operators must provision the bucket, credentials, public-read/CDN behavior for public media, and any server-side encryption requirements separately.

Public rental-home media still stores stable URL values in `media_files.file_url`. With S3, public upload responses use `PublicBaseUrl` plus the normalized object key when configured, so a later CDN can be introduced without changing business logic. If `PublicBaseUrl` is omitted, the provider builds a direct S3-style URL from the configured bucket/region or service URL.

Private deposit receipts remain private with S3. `SavePrivateAsync` stores only the object key as the database URL value, and customer/broker/admin access continues through the authorized `GET /api/bookings/{bookingId}/deposit/receipt` endpoint, which streams the object through `IFileStorage.OpenReadAsync`. Do not make the receipt prefix public in the bucket. Legacy receipt records that still contain `/uploads/deposit-receipts/...` can be read through the controlled endpoint with the Local provider, while direct static-file requests to `/uploads/deposit-receipts/...` are blocked.

The default smoke/local path remains `FileStorage:Provider=Local`, so local smoke checks and CI do not require AWS credentials or network access. Automated tests mock the S3 client and do not call real AWS or S3-compatible services. For production verification, operators should run the API with S3 configuration in a staging environment and manually verify public media upload/delete plus authorized private receipt download.

## Docker

Build the backend image from the repository root:

```bash
docker build -f backend/Dockerfile -t daily-rental-homes-api .
```

Run the image manually:

```bash
docker run --rm -p 5099:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=DailyRentalHomes;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True" \
  -e Token__Issuer=DailyRentalHomes \
  -e Token__Audience=DailyRentalHomesClients \
  -e Token__Key=CHANGE_ME_TO_A_SECURE_32_BYTE_MINIMUM_SECRET \
  daily-rental-homes-api
```

Start the API with a local SQL Server container:

```bash
docker compose up --build
```

The compose file exposes:

- API: `http://127.0.0.1:5099`
- SQL Server: `127.0.0.1:1433`

SQL Server data is persisted in the `sqlserver-data` Docker volume. Apply EF migrations before using a fresh database:

```bash
cd backend
dotnet ef database update --project src/DailyRentalHomes.Infrastructure --startup-project src/DailyRentalHomes.Api
```

The compose API service runs with `ASPNETCORE_ENVIRONMENT=Production` so it does not execute the Development-only local seed before migrations exist. For local non-container development, continue to use the `dotnet run` Development command above.

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
- GET /health
- GET /health/ready

`/health` is a lightweight application liveness endpoint. `/health/ready` checks SQL Server connectivity and returns unhealthy if the configured database is unavailable. The older `/api/health` endpoint remains for backward compatibility and returns `ok`.

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
- GET /api/broker/calendar?from=YYYY-MM-DD&to=YYYY-MM-DD
- GET /api/broker/rental-homes
- GET /api/broker/bookings
- GET /api/broker/bookings/{id}
- PATCH /api/broker/bookings/{id}/status
- PATCH /api/broker/bookings/{id}/accept
- PATCH /api/broker/bookings/{id}/reject
- PATCH /api/broker/bookings/{id}/cancel

Broker endpoints require a Broker or Admin JWT. Broker users only receive homes and bookings linked through `rental_homes.broker_user_id`; another broker's booking returns 404. Soft-deleted homes/bookings are not manageable through broker status actions.

Broker summary (`GET /api/broker/summary`) includes compact dashboard counts for `totalProperties`, `publishedProperties`, `activeBookings`, `pendingDeposits`, and `pendingCancellationRequests`. These counts use the same broker ownership scope as the rest of the broker endpoints. `activeBookings` excludes rejected, cancelled, and completed bookings. `pendingCancellationRequests` counts only active pending customer cancellation requests.

Booking status lifecycle MVP:

- New customer bookings start as `pending`.
- Pending bookings can be accepted with `/accept`, rejected with `/reject`, or cancelled with `/cancel`.
- Accepting moves the booking to `confirmed`.
- Rejected and cancelled bookings cannot be accepted again.
- Confirmed and waiting-deposit bookings can be cancelled.
- Each broker status action writes `booking_status_history` and queues a `booking_status_changed` outbox record.
- Pending, waiting-deposit, paid, and confirmed bookings block availability. Rejected and cancelled bookings do not block future booking dates.

The generic broker status endpoint remains for backward compatibility and permits cancellation only. Deposit request/approval flow remains separate: accepting a booking does not automatically create a deposit, and requesting a deposit still moves an eligible booking to `waiting_deposit`. The legacy ID-based `POST /api/bookings/{id}/status` endpoint is Admin-only.

Broker booking list (`GET /api/broker/bookings`) includes `hasPendingCancellationRequest`, which is `true` only when that booking has an active `pending` customer cancellation request. Approved, rejected, or otherwise resolved requests return `false`, and another broker's bookings remain hidden by the existing scope rules.

Broker calendar (`GET /api/broker/calendar`) requires inclusive `from` and `to` query parameters. It returns broker-owned booking events and manual availability block events for the requested date range. Event rows include `bookingId`, `rentalHomeId`, `rentalHomeTitle`, `startDate`, `endDate`, `bookingStatus`, `customerName`, and `eventType` (`booking` or `manual-block`). Broker ownership scoping is the same as other broker endpoints; Admin can see all data through the existing Broker/Admin policy.

Broker booking detail (`GET /api/broker/bookings/{id}`) includes a nullable `cancellationRequest` summary when the booking has an active pending customer cancellation request. The summary contains `id`, `bookingId`, `statusCode`, optional `reason`, optional `decisionNote`, `createdAt`, and nullable `decidedAt`. Pending cancellation requests can be decided through:

- POST /api/broker/bookings/{bookingId}/cancellation-requests/{requestId}/approve
- POST /api/broker/bookings/{bookingId}/cancellation-requests/{requestId}/reject

Both endpoints accept optional JSON `{ "note": "..." }`; note is limited to 1000 characters. Broker users can decide only requests for bookings linked to their own rental homes; Admin can decide any booking in the Broker/Admin policy scope. Another broker receives 404.

Approving sets the request status to `approved`, stores decision metadata, moves the booking to `cancelled`, writes booking status history using the broker note when provided, and queues a `booking_cancellation_approved` customer notification. It does not delete booking dates and does not modify deposit/refund state. Because the booking status becomes `cancelled`, existing availability rules stop treating its dates as blocking. Rejecting sets the request status to `rejected`, stores decision metadata, keeps the booking status unchanged, and queues `booking_cancellation_rejected`; the broker note is included in the notification message when provided.

### Broker rental home management

Broker/Admin JWT endpoints:

- POST /api/broker/rental-homes
- GET /api/broker/rental-homes/{id}
- PUT /api/broker/rental-homes/{id}
- POST /api/broker/rental-homes/{id}/duplicate
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

`POST /api/broker/rental-homes/{id}/duplicate` creates a draft copy of an existing rental home. Broker users can duplicate only their own homes; Admin can duplicate any home. The duplicate gets a new ID, new timestamps, `isPublished = false`, copied basic property fields, copied amenities, and new media rows that reference the same existing media URLs. It does not copy bookings, availability blocks, expenses, reports, cancellation requests, or any booking-related state. Coordinates and house rules are not copied because those fields are not part of the current rental-home model yet.

Home images are stored in the existing `media_files` table with `file_type = HomeImage`. The first image for a home is assigned `sort_order = 0` and treated as the main image. Setting another image as main moves it to `sort_order = 0`. Upload accepts JPG, PNG, and WebP images up to 5 MB and saves files through `IFileStorage`. With the default Local provider, development files are stored under the configured local root, usually `src/DailyRentalHomes.Api/wwwroot/uploads/rental-homes/{homeId}`. Public URLs are returned as `/uploads/rental-homes/...`; local filesystem paths are not exposed.

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
- POST /api/broker/bookings/{bookingId}/deposit/extend-deadline
- POST /api/broker/bookings/{bookingId}/deposit/approve
- POST /api/broker/bookings/{bookingId}/deposit/reject

Customer endpoints (Customer JWT, matched by booking customer/user or verified phone):

- GET /api/account/bookings
- GET /api/account/bookings/{id}
- POST /api/account/bookings/{id}/deposit/receipt (`multipart/form-data`, field: `file`)
- GET /api/bookings/{id}/deposit/receipt
- POST /api/account/bookings/{id}/cancellation-requests

Requesting a deposit creates one deposit per booking, stores only a masked card value, and moves a Pending booking to `waiting_deposit`. Customer account booking list/detail responses include booking status, selected dates, total amount, rental home city/district/main image, and deposit instructions when available. Customer-visible deposit data includes amount, deadline, status, card holder, masked PAN, bank name, broker instruction note, uploaded receipt, review note, and `allowReupload`; broker-only private availability notes are not exposed.

Receipt upload accepts JPG, PNG, or WebP images up to 5 MB and changes the deposit to `receipt_uploaded`. Customers can upload only for bookings matched to their account/user or verified phone. Upload is allowed only when the deposit is waiting for a receipt, or when it was rejected with `allowReupload = true`. Approval requires a receipt, sets the deposit to `approved`, and moves the booking to `confirmed`. Rejection keeps the booking in `waiting_deposit` and may allow a replacement upload. Legacy generic deposit/media write endpoints are Admin-only so Broker and Customer users cannot bypass this flow.

Deposit receipt files are private. New Local receipt uploads are stored outside `wwwroot`, and deposit DTOs return `/api/bookings/{bookingId}/deposit/receipt` as the receipt link. The download endpoint requires authentication and authorizes the related customer, owning broker, or Admin before streaming the file. It returns `no-store/private` cache headers and a safe download file name. Unrelated users receive not-found behavior; anonymous users are blocked by authentication. Public rental-home media under `/uploads/rental-homes/...` remains unchanged.

Broker/Admin users can extend a requested deposit deadline with `POST /api/broker/bookings/{bookingId}/deposit/extend-deadline`:

```json
{
  "deadlineAt": "2026-07-20T18:00:00Z",
  "reason": "Müştəri əlavə vaxt istədi"
}
```

The booking and deposit must exist in the broker ownership scope. The new deadline must be in the future and later than the current deadline. `reason` is optional and limited to 500 characters. Approved deposits cannot be extended, and bookings in `cancelled`, `completed`, or `rejected` status reject the request. Extending a deadline updates only deposit deadline metadata (`deadlineAt`, `deadlineExtendedAt`, `deadlineExtendedByUserId`, `deadlineExtensionReason`); it does not change booking status, deposit amount, deposit approval status, availability, refunds, or payment state.

Successful extensions queue one customer notification outbox record with type `deposit_deadline_extended`, title `Beh müddəti uzadıldı`, and a message containing the new deadline plus the reason when provided. This PR does not add automatic refund, automatic booking cancellation, reminder worker changes, real message sending, or frontend UI.

Admin users can manually run one deposit deadline reminder processing pass:

- POST /api/admin/deposit-deadline-reminders/process

The processor queues customer `deposit_deadline_reminder` outbox records for deposits whose `deadlineAt` is in the future and inside the configured reminder window. It skips approved deposits and bookings in `cancelled`, `completed`, or `rejected` status. The reminder window is configured with:

```json
"DepositReminderOptions": {
  "ReminderBeforeHours": 24,
  "ProcessingIntervalMinutes": 15
}
```

Duplicate protection reuses the existing `outbound_messages` table: a reminder is considered already queued when the same deposit already has a `deposit_deadline_reminder` message containing the current effective deadline. Running the manual processor repeatedly does not queue duplicates for the same deadline. If a broker later extends the deposit deadline, the new deadline is treated as a new reminder cycle and can queue a new reminder when it enters the configured window.

Reminder processing is notification-only. It does not change booking status, deposit status, deposit amount, deadline extension metadata, refunds, or availability. `DepositDeadlineReminderWorker` automatically runs the same processing service in the background every `ProcessingIntervalMinutes` minutes. Very small or invalid intervals are clamped to a safe minimum of one minute so configuration mistakes do not create a tight database loop. The Admin manual endpoint remains available as an operational fallback for local/support use. Reminders are queued through the existing notification outbox; this worker does not send WhatsApp/SMS/email directly.

Expired deposit deadline state is derived at read time. A deposit is considered expired when it has a `deadlineAt` in the past, is not approved, and the related booking is not `cancelled`, `completed`, or `rejected`. This state is exposed as `isDeadlineExpired` in deposit responses used by Broker and Customer booking detail/list flows. Broker booking list supports `hasExpiredDepositDeadline=true` to filter owned bookings with currently expired deposit deadlines. Admin users can inspect current expired deposit deadlines with:

- GET /api/admin/deposit-deadlines/expired

This endpoint is read-only and returns booking, deposit, rental home, customer, deadline, deposit status, and booking status fields. Expired deadline tracking does not automatically cancel bookings, reject deposits, issue refunds, send notifications, or change reminder duplicate-protection behavior.

Customer cancellation requests are stored in `booking_cancellation_requests`. `POST /api/account/bookings/{id}/cancellation-requests` accepts optional JSON `{ "reason": "..." }`; reason is limited to 1000 characters. Customers can request cancellation only for their own active bookings in `pending`, `waiting_deposit`, `confirmed`, or `paid`. `completed`, `rejected`, and `cancelled` bookings return `400 Bad Request`. If the booking is not owned by the customer, the API returns the existing not-found behavior. A duplicate active `pending` cancellation request returns `400 Bad Request` with a readable message.

Submitting a cancellation request does not change the booking status, does not release availability, does not refund, and does not change deposit state. Account booking detail responses include `cancelRequestSent = true` when an active pending request exists. A new request queues a `booking_cancellation_requested` notification outbox record for the broker contact using the existing outbox pattern. Broker approval/rejection queues customer outbox records, but no real WhatsApp/SMS provider is used.

Customer-visible status meanings:

- `pending` — broker confirmation is still pending.
- `confirmed` — broker accepted the booking.
- `rejected` / `cancelled` — booking is no longer active and no customer action is required.
- Deposit `requested` — customer should upload a receipt.
- Deposit `receipt_uploaded` — broker is reviewing the receipt.
- Deposit `approved` — deposit was accepted.
- Deposit `rejected` — customer may re-upload only when `allowReupload` is true.

Development receipts are saved through `IFileStorage`; with the default Local provider, new receipt files are stored under `src/DailyRentalHomes.Api/private-uploads/deposit-receipts` and opened through the authorized API endpoint. Direct static access to `/uploads/deposit-receipts/...` is blocked for compatibility records. Production still requires malware/content validation, retention rules, and encryption. There is no payment gateway or real SMS/WhatsApp provider. Full card PAN must never be stored; the API requires a masked value containing `*`.

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

Notification delivery foundation uses `INotificationDeliveryProvider` and the development-safe `FakeNotificationDeliveryProvider`. The selected provider is configured with:

```json
"NotificationDelivery": {
  "Provider": "Fake"
}
```

Supported providers:

- `Fake` — default for local development and automated tests.
- `MetaWhatsApp` — sends due outbox records through Meta WhatsApp Cloud API.

The fake provider does not call external WhatsApp/SMS/email APIs, does not need vendor credentials, and is safe for local development and tests. Normal pending messages are marked as `sent`, `sent_at` is set, and `provider_message_id` is stored as `fake-{outboxId}`. If title, text, recipient name, or recipient phone contains `FAIL_FAKE_PROVIDER`, the message is marked as `failed` and `error_message` is populated.

Meta WhatsApp configuration example:

```json
"NotificationDelivery": {
  "Provider": "MetaWhatsApp",
  "MetaWhatsApp": {
    "PhoneNumberId": "YOUR_META_PHONE_NUMBER_ID",
    "AccessToken": "DO_NOT_COMMIT_REAL_TOKENS",
    "ApiVersion": "v22.0",
    "WebhookVerifyToken": "DO_NOT_COMMIT_REAL_TOKENS",
    "AppSecret": "DO_NOT_COMMIT_REAL_SECRETS"
  }
}
```

Environment variable override example:

```bash
NotificationDelivery__Provider=MetaWhatsApp
NotificationDelivery__MetaWhatsApp__PhoneNumberId=YOUR_META_PHONE_NUMBER_ID
NotificationDelivery__MetaWhatsApp__AccessToken=YOUR_SECRET_TOKEN
NotificationDelivery__MetaWhatsApp__ApiVersion=v22.0
NotificationDelivery__MetaWhatsApp__WebhookVerifyToken=YOUR_WEBHOOK_VERIFY_TOKEN
NotificationDelivery__MetaWhatsApp__AppSecret=YOUR_META_APP_SECRET
```

When `MetaWhatsApp` is selected, missing phone number id, access token, or API version fails configuration validation clearly. The API posts plain text messages to Meta Graph API `/{apiVersion}/{phoneNumberId}/messages` with `messaging_product = whatsapp`, `recipient_type = individual`, and `type = text`. The provider combines the outbox title and text into the WhatsApp text body, normalizes common Azerbaijani phone formats to international digits, stores Meta's returned message id when available, and maps Meta HTTP/error responses into the existing failed outbox path. Access tokens and full Authorization headers are not logged.

The background worker is registered but disabled by default:

```json
"Notifications": {
  "WorkerEnabled": false,
  "PollSeconds": 30,
  "BatchSize": 20
}
```

When enabled, `NotificationDeliveryWorker` polls due `pending` messages where `scheduled_at` is empty or in the past and `next_attempt_at` is empty or due, processes a limited batch, and sends each eligible outbox row through `INotificationDeliveryProvider`. Eligibility and retry/failure decisions stay in the outbox processing layer; the provider only attempts delivery and returns success/failure details. Provider exceptions are caught, logged with the outbox message id, and converted into the retryable failed-attempt path so one provider failure does not crash the processing pass. For local/dev testing while the worker is disabled, Admin users can manually process pending messages:

- POST /api/admin/notifications/process-pending

Optional body:

```json
{
  "batchSize": 20
}
```

The response contains `processed`, `sent`, `failed`, and `retried`. Batch size must be between 1 and 100. Manual Admin processing and the background worker use the same provider-backed delivery path; there is no separate Admin-only delivery implementation.

Notification delivery retry configuration:

```json
"NotificationDelivery": {
  "Retry": {
    "MaxAttempts": 5,
    "InitialDelayMinutes": 2,
    "MaxDelayMinutes": 60
  }
}
```

Retry behavior:

- Retryable failures keep the outbox row in `pending`, increment `delivery_attempt_count`, store `last_attempt_at` and `error_message`, and schedule `next_attempt_at`.
- Backoff is exponential: first retry uses `InitialDelayMinutes`, later retries double the delay, and the delay is capped by `MaxDelayMinutes`.
- Messages are not automatically retried before `next_attempt_at`.
- When `MaxAttempts` is reached, the row is marked `failed` and automatic retries stop.
- Successful delivery marks the row `sent`, stores the provider message id, clears `error_message`, and clears `next_attempt_at`.
- Permanent failures are marked `failed` immediately and are not automatically retried.

Current failure classification is deliberately small:

- Retryable: provider exceptions, network/timeout failures, Meta HTTP `429`, and Meta HTTP `5xx`.
- Permanent: invalid/missing recipient phone number, missing/empty configured Meta template mapping, missing required template metadata, and other Meta `4xx` responses.

Admin users can manually retry a failed/exhausted notification through:

- POST /api/admin/notifications/{id}/retry

Manual retry resets the outbox row to a fresh pending attempt and immediately uses the same `INotificationDeliveryProvider` delivery path. It does not bypass provider authentication, phone normalization, Meta template request building, or provider message-id persistence. Messages are never deleted by delivery processing. This PR does not add distributed locks, external queues, Meta webhooks beyond the existing status webhook, inbound WhatsApp messages, template CRUD, SMS/email fallback, or a second retry framework.

Meta WhatsApp delivery status webhooks are supported for outbound delivery visibility:

- GET /api/webhooks/meta-whatsapp
- POST /api/webhooks/meta-whatsapp

The GET route implements Meta's webhook verification handshake with `hub.mode`, `hub.verify_token`, and `hub.challenge`. It returns the challenge only when mode is `subscribe`, the configured `WebhookVerifyToken` matches, and a challenge value is present. Invalid verification attempts return `403`; the configured verification token is never logged.

The POST route validates the `X-Hub-Signature-256` header before processing. The expected header format is `sha256=<hex digest>`. The digest is computed with HMAC SHA-256 over the exact raw HTTP request body bytes using the configured `AppSecret`, and signatures are compared using constant-time comparison. Missing, malformed, or invalid signatures return `401` and the webhook payload is not processed. The App Secret, expected signature, received signature, Authorization headers, and raw payload are not logged.

After signature validation succeeds, the POST route processes the WhatsApp `messages` webhook `statuses[]` payload and ignores unrelated inbound customer `messages[]` payloads in this PR. Status entries are correlated only by Meta provider message id (`statuses[].id`) against the existing `outbound_messages.provider_message_id`; unknown provider ids are ignored safely and do not create new outbox rows.

Supported Meta delivery statuses:

- `sent`
- `delivered`
- `read`
- `failed`

Webhook status state is stored on the existing outbox row using `provider_delivery_status`, `provider_status_updated_at`, `delivered_at`, and `read_at`. `sent` keeps/sets `sent_at`; `failed` sets the existing outbox status to `failed` and stores useful provider error code/title/message details in `error_message`. Status handling is idempotent and ordered: later lifecycle states are not regressed by duplicate or earlier events, so `read` is not overwritten by a later `sent` webhook. Multiple status entries in one payload are processed independently where practical; malformed/unrelated entries are skipped without corrupting valid entries.

Meta WhatsApp template delivery is supported for selected notification outbox type codes. The delivery pipeline still processes the same `outbound_messages` rows; the Meta provider only changes the outbound Graph API request format when a notification type has a configured template mapping. Unsupported notification types continue to use the existing plain-text WhatsApp request.

Template configuration example:

```json
"NotificationDelivery": {
  "Provider": "MetaWhatsApp",
  "MetaWhatsApp": {
    "PhoneNumberId": "YOUR_META_PHONE_NUMBER_ID",
    "AccessToken": "DO_NOT_COMMIT_REAL_TOKENS",
    "ApiVersion": "v22.0",
    "WebhookVerifyToken": "DO_NOT_COMMIT_REAL_TOKENS",
    "AppSecret": "DO_NOT_COMMIT_REAL_SECRETS",
    "DefaultLanguageCode": "az",
    "Templates": {
      "deposit_deadline_reminder": "deposit_deadline_reminder",
      "deposit_deadline_extended": "deposit_deadline_extended"
    }
  }
}
```

Environment variable override example:

```bash
NotificationDelivery__MetaWhatsApp__DefaultLanguageCode=az
NotificationDelivery__MetaWhatsApp__Templates__deposit_deadline_reminder=deposit_deadline_reminder
NotificationDelivery__MetaWhatsApp__Templates__deposit_deadline_extended=deposit_deadline_extended
```

Current template-backed notification types:

- `deposit_deadline_reminder`
- `deposit_deadline_extended`

Expected body parameter contracts:

- `deposit_deadline_reminder`: one text parameter, the current deposit deadline formatted for users.
- `deposit_deadline_extended`: one text parameter for the new deposit deadline, plus a second text parameter for the extension reason when a reason exists. Configure the approved Meta template to match the parameter count you expect to send.

Template names are configuration-driven and are not known by booking/deposit business logic. Deposit deadline notifications store reusable structured outbox metadata (`deadlineAt`, formatted `deadlineText`, and optional `deadlineExtensionReason`) so the provider does not parse human-readable Azerbaijani message text to reconstruct template variables.

Fallback and safety behavior:

- If a notification type has no template mapping, Meta delivery uses the existing plain-text request format.
- If a notification type has a template mapping but the configured template name is empty, provider delivery fails safely and the outbox row follows the existing failed-message path.
- If a mapped template notification is missing required structured parameters, provider delivery fails safely instead of silently sending malformed template requests or falling back to plain text.

Templates must already exist and be approved in the Meta WhatsApp account. This backend does not create, update, delete, or validate Meta templates, and does not add Admin template-management UI, inbound chat, automated replies, SMS/email fallback, or a new messaging system.

Production WhatsApp messaging may require approved templates depending on Meta account status, conversation window, and business messaging rules. Richer delivery receipts, rate limiting, and production retry/idempotency strategy remain future work.

Admin notification list responses also expose delivery/retry fields for UI/debugging: nullable `providerMessageId`, nullable `errorMessage`, nullable `sentAt`, `deliveryAttemptCount`, nullable `lastAttemptAt`, and nullable `nextAttemptAt`.

Production still requires template governance where needed, stronger idempotency strategy, richer delivery receipts, rate limits, observability, retention, and protection of recipient/payload personal data.

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
