# Production readiness checklist

This checklist documents the current MVP deployment expectations. It is not a claim that the system is production-complete; it lists the concrete configuration and operational steps required before a production-like run.

## Required backend environment variables

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS`, for example `http://+:8080` in a container
- `ConnectionStrings__DefaultConnection`
- `Token__Issuer`
- `Token__Audience`
- `Token__Key`
- `Token__Minutes`

`Token__Key` must be a secure secret of at least 32 bytes. Do not use the development key from `appsettings.Development.json`.

## Optional backend environment variables

- `Notifications__WorkerEnabled`
- `Notifications__PollSeconds`
- `Notifications__BatchSize`
- `DepositReminderOptions__ReminderBeforeHours`
- `DepositReminderOptions__ProcessingIntervalMinutes`
- `NotificationDelivery__Provider`

The default notification provider is `Fake`, which is safe for local development and tests but does not deliver real messages.

## Notification retry/backoff configuration

The retry/backoff settings are inherited from `feature/notification-retry-backoff`:

- `NotificationDelivery__Retry__MaxAttempts`
- `NotificationDelivery__Retry__InitialDelayMinutes`
- `NotificationDelivery__Retry__MaxDelayMinutes`

Defaults:

- `MaxAttempts = 5`
- `InitialDelayMinutes = 2`
- `MaxDelayMinutes = 60`

Invalid retry configuration fails options validation at startup.

## Meta WhatsApp configuration

When `NotificationDelivery__Provider=MetaWhatsApp`, configure:

- `NotificationDelivery__MetaWhatsApp__PhoneNumberId`
- `NotificationDelivery__MetaWhatsApp__AccessToken`
- `NotificationDelivery__MetaWhatsApp__ApiVersion`, for example `v22.0`
- `NotificationDelivery__MetaWhatsApp__WebhookVerifyToken`
- `NotificationDelivery__MetaWhatsApp__AppSecret`
- `NotificationDelivery__MetaWhatsApp__DefaultLanguageCode`
- `NotificationDelivery__MetaWhatsApp__Templates__deposit_deadline_reminder`
- `NotificationDelivery__MetaWhatsApp__Templates__deposit_deadline_extended`

Do not commit real Meta credentials. Approved WhatsApp templates must already exist in Meta WhatsApp Manager.

Webhook routes:

- `GET /api/webhooks/meta-whatsapp`
- `POST /api/webhooks/meta-whatsapp`

The public webhook URL must point to the deployed API host. POST requests require `X-Hub-Signature-256` validation using `MetaWhatsApp:AppSecret`.

## Database migration step

Apply EF Core migrations before serving production traffic:

```bash
cd backend
dotnet ef database update --project src/DailyRentalHomes.Infrastructure --startup-project src/DailyRentalHomes.Api
```

The migration chain is not squashed. A fresh database must include all migrations through the latest branch migration before the API is used.

## Backend startup

Local production-like run:

```bash
cd backend/src/DailyRentalHomes.Api
ASPNETCORE_ENVIRONMENT=Production \
ASPNETCORE_URLS=http://127.0.0.1:5099 \
ConnectionStrings__DefaultConnection="YOUR_SQLSERVER_CONNECTION_STRING" \
Token__Issuer=DailyRentalHomes \
Token__Audience=DailyRentalHomesClients \
Token__Key=CHANGE_ME_TO_A_SECURE_32_BYTE_MINIMUM_SECRET \
dotnet run
```

Development mode still uses `appsettings.Development.json` for the local JWT key and should not be used as the production configuration.

## Health/readiness endpoints

- `GET /health` checks process/liveness health.
- `GET /health/ready` checks database readiness.
- `GET /api/health` is a legacy lightweight `ok` endpoint.

Use `/health/ready` for deployment readiness gates that need database connectivity.

## Frontend build and API configuration

Frontend production build:

```bash
cd clients/web-app
npm install
npm run build
```

Live API mode is configured with:

- `VITE_USE_LIVE_API=true`
- `VITE_API_BASE_URL`

If `VITE_API_BASE_URL` is empty, the frontend uses relative `/api` calls. In that setup, the deployed frontend host or reverse proxy must route `/api` to the backend API.

## Docker usage

Backend image:

```bash
docker build -f backend/Dockerfile -t daily-rental-homes-api .
```

Development compose:

```bash
docker compose up --build
```

The compose file is for local/development SQL Server usage. Replace placeholder passwords and JWT secrets through environment-specific overrides before any shared or production-like deployment.

## Security-sensitive defaults

- Swagger is enabled only in Development.
- Development CORS is enabled only in Development for `localhost:5173` and `127.0.0.1:5173`.
- Production JWT key must be supplied outside source control.
- Meta access token, verify token, and app secret must be supplied outside source control.
- The default `Fake` notification provider does not send real messages.

## Known remaining limitations

- No distributed locking for notification workers across multiple API instances.
- No external queue such as RabbitMQ, Kafka, or Hangfire.
- Local upload storage is still an MVP implementation; production needs private object storage and authorization-aware downloads.
- No payment gateway or refund automation.
- No inbound WhatsApp chat handling.
- No Kubernetes/Terraform/CI deployment automation in this repository.
