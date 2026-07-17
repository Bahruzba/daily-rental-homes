# Production security checklist

This checklist captures the final MVP security posture. It is intentionally operational and should be reviewed before any real production deployment.

## Secrets and configuration

- Set `ASPNETCORE_ENVIRONMENT=Production` for production containers.
- Generate a unique high-entropy `Token__Key`; known local/development placeholders are rejected outside Development.
- Store `ConnectionStrings__DefaultConnection`, `Token__Key`, Meta WhatsApp credentials, and S3 credentials in a secret manager or protected deployment variables.
- Do not commit real JWT keys, SQL passwords, AWS keys, Meta access tokens, webhook verify tokens, or app secrets.
- Keep `NotificationDelivery__Provider=Fake` unless real Meta WhatsApp credentials and approved templates are configured.

## Authentication and authorization

- JWT validation checks issuer, audience, lifetime, signing key, and role claims.
- Admin/Broker/Customer endpoints use role policies; broker/customer data access must continue using existing ownership scopes.
- Keep branch protection checks enabled for `Backend`, `Frontend`, and `Migrations` before merging changes.

## Private files and uploads

- Rental-home media is intentionally public.
- Deposit receipts are private and must be downloaded through `GET /api/bookings/{bookingId}/deposit/receipt` with authorization.
- Do not make the `deposit-receipts/` prefix public in local static files or S3 buckets.
- Local and S3 storage keys must remain normalized relative keys; traversal and absolute paths are rejected.
- Uploads currently validate size and accepted image content types, but do not include malware scanning, image re-encoding, or antivirus infrastructure.

## S3 least privilege

- The application only needs object read/write/delete for the configured bucket/prefixes.
- Grant public read/CDN access only to intended public rental-home media prefixes.
- Keep private receipt prefixes non-public.
- Prefer server-side encryption and bucket access logging according to the hosting environment's policies.

## Meta WhatsApp

- Configure `NotificationDelivery__MetaWhatsApp__AppSecret`; unsigned or invalidly signed webhook POST requests must not update delivery state.
- Do not log access tokens, app secrets, webhook signatures, Authorization headers, or raw webhook payloads.
- Templates must be pre-approved in Meta; the application does not manage template lifecycle.

## CORS, middleware, and error exposure

- Development CORS is enabled only in Development for `localhost`/`127.0.0.1:5173`.
- Swagger is enabled only in Development.
- Production should run behind HTTPS with trusted reverse-proxy/header configuration supplied by the hosting platform.
- Avoid returning raw exception details, connection strings, private storage paths, JWTs, or provider credentials in API responses.

## Remaining known risks

- No malware scanning or content disarm/reconstruction for uploads.
- No production payment gateway/refund automation.
- No cloud infrastructure provisioning, bucket policy automation, or WAF configuration.
- No inbound WhatsApp chat handling.
- Full deployment smoke validation remains a separate pre-deployment operation.
