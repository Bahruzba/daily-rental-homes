# Production readiness checklist

## Deployment order

1. Configure environment variables, including `ConnectionStrings__DefaultConnection`, `Token__Key`, file storage, notification provider, and worker settings.
2. Prepare the database server and database.
3. Generate/review the idempotent migration script with `backend/scripts/verify-migrations.ps1`.
4. Apply EF migrations to the intended database. For command-line EF usage, set `DAILY_RENTAL_HOMES_CONNECTION_STRING` to the same value as `ConnectionStrings__DefaultConnection`.
5. Configure Local file storage or a future object-storage provider. Local storage must be backed by shared persistent storage for multi-instance deployments.
6. Configure notification provider. The automated smoke path uses `NotificationDelivery__Provider=Fake`; real Meta WhatsApp credentials are separate production configuration.
7. Start the backend.
8. Check `/health` and `/health/ready`.
9. Build/start the frontend.
10. Verify one basic public API request, for example `GET /api/rental-homes`.

## Smoke validation

PowerShell scripts:

- `powershell -NoProfile -ExecutionPolicy Bypass -File backend/scripts/verify-migrations.ps1` generates an idempotent EF migration script under `backend/artifacts/` and does not mutate the database.
- `powershell -NoProfile -ExecutionPolicy Bypass -File backend/scripts/deployment-smoke.ps1` starts the API with production-like settings, Fake notification provider, distributed locking enabled, and Local storage configured, then checks `/health`, `/health/ready`, Local storage root creation, one public API request, and frontend `npm run build`.

`deployment-smoke.ps1` requires a reachable SQL Server database with the current schema. It intentionally does not apply migrations automatically to avoid accidental production database mutation.

## Background worker distributed locking

The backend uses a database-backed `distributed_locks` table to prevent duplicate recurring worker processing when multiple API instances are running.

Locked workers:

- `NotificationDeliveryWorker` with key `notification-delivery-worker`
- `DepositDeadlineReminderWorker` with key `deposit-deadline-reminder-worker`

Configuration:

```json
"BackgroundWorkers": {
  "DistributedLocking": {
    "Enabled": true,
    "LeaseSeconds": 120
  }
}
```

Deployment notes:

- Apply EF migrations before enabling multiple API instances.
- Keep `BackgroundWorkers__DistributedLocking__Enabled=true` in multi-instance deployments.
- Set `BackgroundWorkers__DistributedLocking__LeaseSeconds` longer than a normal worker processing pass.
- If future worker work becomes long-running, add lease renewal before increasing batch sizes significantly.
- Distributed locking does not replace existing idempotency checks; notification retry/backoff and deposit reminder duplicate protection must remain enabled.

Remaining limitations:

- This is not a general job framework.
- There is no Redis/RabbitMQ/Hangfire/Quartz integration.
- Manual Admin processing endpoints are not distributed-lock protected in this PR; they remain operational support tools and should be used carefully in production.
