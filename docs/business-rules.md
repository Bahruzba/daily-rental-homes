# Business Rules

## Roles

### Admin

- Logs in with OTP.
- Can manage brokers, homes, bookings, statuses, and system dictionaries.

### Broker

- Logs in with OTP.
- Can add and manage their own homes.
- Can share a home link with customers.
- Can manage booking requests for their homes.
- Can extend deposit deadline when needed.

### Customer

- Can open a shared home link.
- Can create a booking request.
- Can upload deposit receipt if deposit is requested.

## Media Files

All images are stored in one media table. The file type identifies the purpose:

- Home image
- Card image
- Deposit receipt image
- Other

## Contacts

Phone and WhatsApp contacts are stored in the same table. Contact type identifies the purpose.

Notification preference is stored in one column, not split into multiple notification columns.

## Bookings

Booking dates are stored as a list in a separate table. We do not rely only on check-in/check-out columns.

This allows flexible date blocking and avoids confusion later.

## Deposit / Beh

Deposit information is stored separately from the booking.

Deposit can have:

- Amount
- Deadline
- Status
- Payment card
- Receipt image
- Broker note

When the deadline is close, the customer and broker should be notified.

## Booking Status

Booking status is stored through separate status and history tables.

Every status change should keep:

- Old status
- New status
- Changed by user
- Change date
- Note
