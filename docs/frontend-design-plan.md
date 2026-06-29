# Frontend Design Plan â€” Daily Rental Homes

## Purpose and scope

This proposal defines the visual and interaction direction for the future web client. It is not a frontend implementation and does not select a JavaScript framework. The plan follows the API-first architecture and the current MVP flow: a broker shares a rental home, a customer reviews it, selects a list of dates, and submits a booking request.

The information hierarchy is inspired by the practical search, filter, listing, and favourites patterns visible on [gunlukevler.az](https://gunlukevler.az/), but the visual system, layout, components, and interaction details below are original and intentionally simpler.

## 1. Overall design direction

- **Simple and calm:** generous whitespace, limited visual noise, and one clear primary action per section.
- **Photo-first:** property imagery is the strongest element on listing and detail pages.
- **Local and trustworthy:** clear Azerbaijani labels, visible broker contact options, transparent price/date information, and readable booking states.
- **Sharing-first MVP:** a customer must be able to continue from a shared rental or booking link without navigating a heavy marketplace.
- **Progressive disclosure:** the homepage shows essential filters first; detailed filters open in a drawer or expandable panel.
- **API-aligned:** UI states correspond to the existing concepts: homes, media, contacts, amenities, booking date lists, deposits, booking statuses, and status history.
- **Accessible by default:** minimum 44 px interactive targets, visible focus styles, strong contrast, labelled form fields, and status text that does not depend on color alone.

Visual character: warm minimalism rather than a cold corporate dashboard. Cards use soft borders and subtle shadows; corners are moderately rounded, not pill-shaped everywhere.

## 2. Color palette

| Token | Color | Intended use |
|---|---:|---|
| `ink-900` | `#17202A` | Main text, navigation, strong headings |
| `ink-600` | `#5B6470` | Secondary text and metadata |
| `canvas` | `#F6F7F4` | Main page background |
| `surface` | `#FFFFFF` | Cards, forms, panels |
| `line` | `#E3E7E5` | Borders and separators |
| `primary-700` | `#0F5F59` | Primary buttons and active navigation |
| `primary-600` | `#0F766E` | Links, selected filters, highlights |
| `primary-100` | `#DDF4F0` | Soft selected backgrounds |
| `accent-500` | `#F59E0B` | Price emphasis, small attention markers |
| `success-600` | `#15803D` | Confirmed/paid states |
| `warning-600` | `#B45309` | Waiting deposit/deadline states |
| `danger-600` | `#B42318` | Errors, cancellation, destructive actions |

Rules:

- Use teal as the only dominant brand color.
- Use amber sparingly for price or deadline attention, never as a second primary color.
- Keep large surfaces neutral so property photos remain visually dominant.
- Every colored status badge must also include readable status text.

## 3. Typography direction

- Recommended future UI family: **Manrope** or **Inter**, with system sans-serif fallback.
- Wireframes use the system font stack and require no external assets.
- Body text: 16 px desktop, 15â€“16 px mobile, line height 1.5â€“1.65.
- Page title: 32â€“40 px desktop, 26â€“30 px mobile.
- Section title: 22â€“28 px desktop, 20â€“24 px mobile.
- Labels and metadata: 13â€“14 px, never below 12 px.
- Prices use semibold or bold weight and tabular numerals where available.
- Avoid uppercase paragraphs. Uppercase may be used only for compact eyebrow labels.

## 4. Page list

### Public/customer MVP

1. **Home / rental search** â€” featured or recent homes, simple filters, listing grid.
2. **Search results** â€” same listing system with filter drawer, result count, and sort.
3. **Rental detail** â€” gallery, facts, amenities, contacts, price, and booking action.
4. **Booking form** â€” selected date list, customer information, summary, and confirmation.
5. **Booking submitted** â€” reference number and next-step explanation.
6. **Shared booking/deposit status** â€” booking status, deposit deadline, masked card information, and receipt upload when enabled.
7. **OTP sign-in** â€” phone number, code confirmation, and resend state.
8. **Not found / unavailable rental** â€” clear recovery action back to available homes.

### Broker/admin placeholders

9. **Broker dashboard** â€” summary, recent bookings, deposit alerts, and managed homes.
10. **Home management placeholder** â€” list/create/edit entry points.
11. **Booking management placeholder** â€” filters, status changes, and history entry points.
12. **Admin placeholder** â€” brokers, dictionaries, all homes, bookings, and operational summaries.

## 5. Component list

### Navigation and structure

- Desktop header and mobile top bar
- Logo/wordmark placeholder
- Language switch placeholder (`AZ`, later `RU`/`EN`)
- Breadcrumbs
- Page container and section header
- Mobile bottom action bar for detail/booking pages
- Footer with essential links only

### Search and discovery

- Search field
- City/region select
- Date-list picker trigger
- Guest count stepper
- Price range fields
- Amenity chips
- Expandable â€œMore filtersâ€ panel
- Active filter chips with clear-all action
- Result count and sort select

### Rental content

- Rental card
- Image placeholder/gallery
- Favourite icon placeholder
- Price label (`120 â‚¼ / gecÉ™`)
- Location row
- Facts row (room, guest, optional pool/parking)
- Amenity chip/list
- Availability/status badge
- Broker contact card

### Booking and deposit

- Date-list picker/calendar
- Selected-date chips/list
- Contact information fields
- Booking price summary
- Deposit notice
- Masked payment card block
- Receipt upload drop zone
- Booking/deposit status timeline
- Success, error, empty, loading, and disabled states

### Broker/admin

- Sidebar/compact mobile navigation
- KPI summary card
- Data table that becomes cards on mobile
- Status filter tabs
- Deadline alert list
- Empty-state panel
- Confirmation dialog for destructive actions

## 6. Homepage layout

Desktop order:

1. **Header:** wordmark, `EvlÉ™r`, `NecÉ™ iÅŸlÉ™yir?`, `Daxil ol`, and primary `Elan É™lavÉ™ et` action for authenticated brokers.
2. **Hero/search area:** short heading, one supporting line, and a high-contrast search panel.
3. **Primary filters:** location, dates, guests, and `Axtar` button.
4. **Secondary filters:** price, room count, and amenities inside `Daha Ã§ox filtr`.
5. **Recent/available homes:** section title, result count, and 3-column card grid.
6. **Trust strip:** simple three-step explanation â€” choose, request, confirm.
7. **Footer:** contact, help, privacy, and language links.

The homepage should not begin with a large promotional carousel. The search action and first rentals must be visible quickly.

## 7. Listing card layout

Card hierarchy:

1. 4:3 image area with favourite action and optional `Yeni`/`Populyar` badge.
2. Price on the first content row: `120 â‚¼ / gecÉ™`.
3. Short rental title, maximum two lines.
4. Location: city and district.
5. Compact facts: room count and guest capacity.
6. One optional feature line: for example `Hovuz Â· Manqal Â· Wi-Fi`.

Interaction rules:

- The whole card opens the detail page; inner favourite action remains separately accessible.
- Card height stays consistent in a grid.
- Missing photos use a neutral branded placeholder, not a broken image icon.
- Do not overload cards with broker phone numbers, long descriptions, or every amenity.

## 8. Rental detail page layout

Desktop layout:

- Breadcrumbs and title/short location row.
- Gallery: one large image plus four smaller previews; mobile becomes a swipeable single image area.
- Main content column:
  - price and key facts;
  - description;
  - amenities;
  - location text;
  - booking rules/notes;
  - contact methods.
- Sticky side card:
  - nightly price;
  - date-list picker;
  - guest count;
  - estimated total;
  - primary `Rezervasiya sorÄŸusu gÃ¶ndÉ™r` button;
  - WhatsApp/phone secondary actions.

The API stores booking dates as individual rows, so the UI must show a visible list of selected dates. A continuous check-in/check-out range may be offered as a convenience later, but it must resolve to and display the actual selected date list.

## 9. Booking form layout

Use a focused single-column form with a summary panel on desktop. Recommended steps:

1. **Dates:** calendar/date-list selector and removable selected-date chips.
2. **Guests:** guest count with capacity hint.
3. **Contact:** full name, phone number, and optional note.
4. **Summary:** rental, selected nights/dates, daily price, total, and deposit explanation if applicable.
5. **Consent and submit:** short data/communication note and one primary action.
6. **Confirmation:** booking reference and `SorÄŸunuz brokerÉ™ gÃ¶ndÉ™rildi` message.

Validation must appear beside the field and in plain Azerbaijani. Keep entered values after a recoverable error.

## 10. Broker/admin placeholder layout

The placeholder demonstrates information architecture only; it is not a full dashboard implementation.

Desktop:

- Left navigation: `Ä°cmal`, `EvlÉ™rim`, `Rezervasiyalar`, `Beh Ã¶dÉ™niÅŸlÉ™ri`, `Mesajlar`.
- Top bar: current role, notifications placeholder, profile menu.
- KPI row: active homes, pending bookings, waiting deposits, deadlines in 24 hours.
- Main area: recent booking table and deadline alert panel.
- Primary action: `Yeni ev É™lavÉ™ et`.

Admin may later receive additional entries: `BrokerlÉ™r`, `Statuslar`, `RahatlÄ±qlar`, and system-wide reports. Broker and admin remain separate roles and should not be merged into a single ambiguous experience.

## 11. Mobile responsive behavior

- Breakpoints are implementation details; design around approximately 360, 768, and 1200 px widths.
- Header collapses to wordmark, favourites/profile icon, and menu button.
- Search fields stack; the primary `Axtar` button remains full width.
- Advanced filters open in a full-height bottom sheet/drawer with `TÉ™mizlÉ™` and `NÉ™ticÉ™lÉ™ri gÃ¶stÉ™r` actions.
- Listing grid becomes one column; cards retain 4:3 images.
- Detail gallery becomes horizontally swipeable.
- Detail booking side card becomes a sticky bottom bar showing price and `Rezervasiya` action; the full form opens on its own page or sheet.
- Tables in broker/admin views become stacked cards with the most important status and next action visible first.
- Date selections wrap as chips and remain removable by touch.
- Phone and WhatsApp actions may become two equal-width sticky buttons on shared-detail flows.
- Avoid horizontal page scrolling at 320 px width.

## 12. Azerbaijani UI text examples

### Navigation and search

- `EvlÉ™r`
- `NecÉ™ iÅŸlÉ™yir?`
- `SeÃ§ilmiÅŸlÉ™r`
- `Daxil ol`
- `Elan É™lavÉ™ et`
- `Hara getmÉ™k istÉ™yirsiniz?`
- `ÅžÉ™hÉ™r vÉ™ ya rayon seÃ§in`
- `TarixlÉ™ri seÃ§in`
- `Qonaq sayÄ±`
- `Daha Ã§ox filtr`
- `FiltrlÉ™ri tÉ™mizlÉ™`
- `Axtar`
- `24 uyÄŸun ev tapÄ±ldÄ±`

### Rental card/detail

- `120 â‚¼ / gecÉ™`
- `QÉ™bÉ™lÉ™, VÉ™ndam`
- `3 otaq`
- `6 qonaq`
- `BÃ¼tÃ¼n rahatlÄ±qlar`
- `Ev haqqÄ±nda`
- `MÉ™kan`
- `BrokerlÉ™ É™laqÉ™`
- `WhatsApp-da yaz`
- `ZÉ™ng et`
- `Rezervasiya sorÄŸusu gÃ¶ndÉ™r`

### Booking

- `Rezervasiya tarixlÉ™ri`
- `SeÃ§ilmiÅŸ tarixlÉ™r`
- `Tarix É™lavÉ™ et`
- `Ad vÉ™ soyad`
- `Telefon nÃ¶mrÉ™si`
- `Broker Ã¼Ã§Ã¼n qeyd (istÉ™yÉ™ baÄŸlÄ±)`
- `QiymÉ™t xÃ¼lasÉ™si`
- `SorÄŸunu tÉ™sdiqlÉ™`
- `SorÄŸunuz brokerÉ™ gÃ¶ndÉ™rildi`
- `Broker sorÄŸunu yoxladÄ±qdan sonra sizinlÉ™ É™laqÉ™ saxlayacaq.`
- `Bu tarix artÄ±q seÃ§ilib.`
- `Æn azÄ± bir tarix seÃ§in.`

### Deposit/status

- `Beh gÃ¶zlÉ™nilir`
- `Son Ã¶dÉ™niÅŸ vaxtÄ±`
- `Kart: **** **** **** 1234`
- `QÉ™bzi yÃ¼klÉ™`
- `Ã–dÉ™niÅŸ yoxlanÄ±lÄ±r`
- `Rezervasiya tÉ™sdiqlÉ™ndi`
- `LÉ™ÄŸv edildi`

### Broker/admin

- `Ä°cmal`
- `EvlÉ™rim`
- `GÃ¶zlÉ™yÉ™n rezervasiyalar`
- `Beh Ã¶dÉ™niÅŸlÉ™ri`
- `Son 24 saatda bitÉ™n mÃ¼ddÉ™tlÉ™r`
- `Statusu dÉ™yiÅŸ`
- `MÃ¼ddÉ™ti uzat`
- `Yeni ev É™lavÉ™ et`

## Wireframe files

Low-fidelity, standalone HTML/CSS wireframes are stored in `docs/wireframes/`. They intentionally use placeholders instead of production images, icons, fonts, or JavaScript behavior.

