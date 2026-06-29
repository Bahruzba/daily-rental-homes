# Project Plan

## Goal

Build a local-market friendly daily rental homes platform where brokers/admins can manage homes and customers can view links, request bookings, and continue the booking flow.

## MVP Scope

1. OTP based authentication for admin, broker, and customer.
2. Broker/admin can create and manage rental homes.
3. Rental homes can have media files, contacts, amenities, address, price, and availability information.
4. Customer can open a shared home link and create a booking request.
5. Booking stores selected dates as a list.
6. Deposit/beh information is tracked separately from the booking.
7. Booking statuses are stored with history.
8. Notifications can be added later for deposit deadline reminders.

## Backend Phases

### Phase 1: Foundation

- .NET 8 Web API
- Clean Architecture folders
- MSSQL configuration
- EF Core DbContext
- Domain entities

### Phase 2: Auth

- Request OTP
- Verify OTP
- JWT token generation
- Role-based authorization

### Phase 3: Listings

- Home CRUD
- Media files
- Contacts
- Amenities
- Search/filter endpoints

### Phase 4: Booking

- Booking request
- Date list validation
- Deposit deadline
- Status history

### Phase 5: Admin/Broker Panel API

- Broker dashboard
- Booking requests
- Home management
- Reports
