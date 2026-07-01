# Daily Rental Homes — Web App

React, TypeScript və Vite ilə hazırlanmış frontend MVP. Tətbiq hazırda mock ev məlumatlarından istifadə edir; API qatı backend-ə keçid üçün ayrıca saxlanılıb.

Tələb olunan mühit: Node.js `20.19+` və ya `22.12+`.

## Lokal işə salma

```bash
npm install
npm run dev
```

Vite terminalda göstərilən lokal ünvanı açın (adətən `http://localhost:5173`).

Production build:

```bash
npm run build
```

## Struktur

- `src/api/client.ts` — mock/API keçidi və sorğu funksiyaları
- `src/components/` — təkrar istifadə olunan UI komponentləri
- `src/data/homes.ts` — altı demo kirayə evi
- `src/pages/` — ana səhifə, detal, rezervasiya, broker paneli və 404
- `src/styles.css` — ümumi və responsive stillər
- `public/images/` — demo şəkillər

Canlı API-yə keçmək üçün `.env.local` faylında `VITE_USE_LIVE_API=true` və `VITE_API_BASE_URL` dəyərlərini təyin etmək kifayətdir.
