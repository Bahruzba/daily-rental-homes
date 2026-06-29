# Auth Notes

## MVP state

The auth flow is usable for local testing:

1. POST /api/auth/send
2. POST /api/auth/confirm

The send endpoint returns a development PIN. This is only for local testing.

The confirm endpoint returns a temporary token. This must be replaced before production.

## Production tasks

- Use real SMS provider.
- Do not return PIN in API response.
- Use signed JWT.
- Add token expiration validation.
- Add role policies.
- Add rate limiting.
