# Production readiness checklist

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
