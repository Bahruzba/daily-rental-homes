# Daily Rental Homes — Web App

React, TypeScript və Vite ilə hazırlanmış frontend MVP. Tətbiq hazırda mock ev məlumatlarından istifadə edir; API qatı backend-ə keçid üçün ayrıca saxlanılıb.

Tələb olunan mühit: Node.js `20.19+` və ya `22.12+`.

## Lokal işə salma

```bash
npm install
npm run dev
```

Vite terminalda göstərilən lokal ünvanı açın (adətən `http://localhost:5173`).

Bu əmrlər mock rejimində işləyir və backend tələb etmir.

Canlı API rejimi üçün backend-i `http://127.0.0.1:5099` ünvanında başladın və `.env.local` yaradın:

```env
VITE_USE_LIVE_API=true
VITE_API_BASE_URL=
```

Boş `VITE_API_BASE_URL` lokal Vite `/api` proxy-sindən istifadə edir. Ayrı hostda yerləşən API üçün tam base URL yazıla bilər.

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
