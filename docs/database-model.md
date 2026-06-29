# Database Model Notes

Database: Microsoft SQL Server

## Main Tables

- users
- otp_codes
- rental_homes
- media_files
- related_contacts
- amenities
- rental_home_amenities
- bookings
- booking_dates
- booking_deposits
- booking_statuses
- booking_status_history
- payment_cards
- notifications

## Naming Style

- Table names: snake_case
- Column names: snake_case
- C# entities: PascalCase
- API JSON: camelCase

## Important Decisions

1. Do not prefix every table with rental_unit or unit.
2. Use media_files for all images and identify purpose with file_type.
3. Use related_contacts for phone and WhatsApp contacts with contact_type.
4. Use one notify_enabled column for contact notification.
5. Store booking dates in booking_dates as separate rows.
6. Store deposit/beh information in booking_deposits.
7. Store status dictionary in booking_statuses and changes in booking_status_history.
8. Admin and broker login will use OTP, not password.
