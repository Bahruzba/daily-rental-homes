# API First Architecture

The project must be developed with the API as a separate product.

## Main Decision

Backend API, website frontend, and future mobile app must be independent clients.

```text
clients/
  web-app/        # future website
  mobile-app/     # future mobile app

backend/
  src/
    DailyRentalHomes.Api/
    DailyRentalHomes.Application/
    DailyRentalHomes.Domain/
    DailyRentalHomes.Infrastructure/
```

## Backend API

The backend exposes REST endpoints only. It must not depend on any website UI.

Responsibilities:

- Authentication
- Users and roles
- Rental homes
- Media files
- Contacts
- Bookings
- Deposits
- Status history
- Notifications

## Website

The website will be a separate frontend application in the future.

Recommended folder:

```text
clients/web-app
```

The website will consume the backend through HTTP API.

## Mobile App

A mobile app can be added later.

Recommended folder:

```text
clients/mobile-app
```

The mobile app will use the same backend API as the website.

## Rule

Do not put UI logic inside the API project. The API must return clean JSON responses that can be used by web, mobile, and admin clients.
