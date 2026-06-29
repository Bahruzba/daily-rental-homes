# Messaging

## Decision

WhatsApp is the main notification channel.

SMS is kept as a secondary fallback channel for the future.

## Current backend state

The backend has a common messaging abstraction:

```text
IMessageSender
```

The current implementation is development-only:

```text
DevelopmentMessageSender
```

It does not send real messages yet. It returns a fake provider message id for local testing.

## Outgoing message log

All outgoing messages are stored in:

```text
outbound_messages
```

The log stores:

- Channel
- Receiver
- Text
- Status
- Provider message id
- Booking reference
- Deposit reference

## API endpoints

```text
GET  /api/messages
POST /api/messages
```

## Auth flow

OTP is sent through the messaging abstraction with WhatsApp as the default channel.

In local development, the API still returns `devPin` so the flow can be tested without a real provider.

## Future production work

- Add real WhatsApp provider.
- Disable `devPin` response.
- Add retry logic.
- Add provider delivery status callback.
- Add rate limiting.
- Add templates for OTP and deposit reminders.
