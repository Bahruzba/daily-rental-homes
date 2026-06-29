# MVP API Flow

## Authentication

1. Client sends phone to `POST /api/auth/send`.
2. API creates a temporary PIN record.
3. Client sends phone and PIN to `POST /api/auth/confirm`.
4. API creates user if not exists and returns a temporary MVP token.

Production note: temporary token must be replaced with signed JWT.

## Rental Home

1. Broker creates home with `POST /api/rental-homes`.
2. Broker adds media files with `POST /api/media-files`.
3. Broker adds contacts with `POST /api/contacts`.
4. Client lists homes with `GET /api/rental-homes`.

## Booking

1. Customer creates booking with `POST /api/bookings`.
2. Booking dates are stored in `booking_dates` as separate rows.
3. Initial status id is `1`.

## Deposit

1. Broker creates deposit request with `POST /api/deposits`.
2. Customer can later upload receipt as media file with type `DepositReceipt`.
3. Deposit status starts as `Waiting`.

## Dictionaries

- `GET /api/amenities`
- `GET /api/booking-statuses`
- `GET /api/payment-cards`
