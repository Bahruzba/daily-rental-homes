# Daily Rental Homes — Web App

React, TypeScript və Vite ilə hazırlanmış frontend MVP.

Tələb olunan mühit: Node.js `20.19+` və ya `22.12+`.

## Quraşdırma və build

```bash
npm install
npm run dev
npm run build
```

Vite development server adətən `http://127.0.0.1:5173` ünvanında açılır.

## Mock rejim

Mock rejim standartdır və backend tələb etmir. `/login` səhifəsində Admin, Broker və ya Customer rolunu seçin və demo OTP kimi `123456` yazın.

Rol üzrə yönləndirmələr:

- Admin: `/admin`
- Broker: `/broker`
- Customer: `/account`

Broker paneli mock rejimdə demo xülasə, evlər və rezervasiyalar göstərir. Rezervasiya detail səhifəsi `/broker/bookings/:id` marşrutundadır və təhlükəsiz demo status əməliyyatlarını backend olmadan sınamağa imkan verir.

## Live API və OTP

Backend-i Development rejimində `http://127.0.0.1:5099` ünvanında başladın. Sonra `.env.local` yaradın:

```env
VITE_USE_LIVE_API=true
VITE_API_BASE_URL=http://127.0.0.1:5099
```

və ya Windows PowerShell-də:

```powershell
$env:VITE_USE_LIVE_API="true"
$env:VITE_API_BASE_URL="http://127.0.0.1:5099"
npm run dev
```

`/login` səhifəsi əvvəl `/api/auth/send`, sonra `/api/auth/confirm` çağırır. Development backend OTP cavabında yalnız lokal yoxlama üçün `devPin` qaytarır. Live rejimdə rol frontend-dən seçilmir; backend mövcud istifadəçinin rolunu qaytarır, yeni istifadəçi isə Customer olur.

Booking səhifəsi giriş tələb etmir və həm mock, həm live rejimdə işləməyə davam edir.

Live rejimdə `/broker` mövcud JWT-ni Bearer token kimi göndərərək broker summary, ev və booking endpoint-lərindən real məlumat yükləyir. Broker yalnız öz evlərini və həmin evlərin rezervasiyalarını görür. `waiting_deposit` yalnız beh sorğusu ilə, `confirmed` isə qəbz təsdiqi ilə yaranır; generic status əməliyyatı yalnız ləğv üçün istifadə olunur.

## Beh axını

Broker `/broker/bookings/:id` səhifəsində məbləğ, gələcək son tarix, maskalanmış kart, bank və qeyd ilə beh istəyə bilər. Customer `/account` bölməsində öz telefonuna/hesabına bağlı rezervasiyaları, `/account/bookings/:id` səhifəsində beh təlimatını görür və JPG/PNG/WebP qəbz şəkli yükləyir. Broker yüklənmiş qəbzi təsdiq və ya rədd edə bilər. Mock rejimdə eyni ekranlar və state keçidləri backend olmadan işləyir.

Bu MVP real ödəniş etmir. Live receipt faylları backend-in lokal `wwwroot/uploads/deposit-receipts` qovluğunda saxlanılır. Production üçün private object storage və authorization-aware download tələb olunur. Frontend və backend yalnız maskalanmış kart dəyəri qəbul edir; tam PAN yazılmamalıdır.

## Auth saxlanması

JWT və istifadəçi məlumatı MVP üçün `localStorage`-da saxlanılır. Bu, yalnız ilkin MVP yanaşmasıdır; production təhlükəsizliyi üçün daha sonra HttpOnly cookie və uyğun sessiya strategiyası nəzərdən keçirilməlidir.

## Struktur

- `src/api/` — mock/live API keçidi, booking və auth sorğuları
- `src/auth/` — auth context, rol tipləri və qorunan route
- `src/components/` — təkrar istifadə olunan UI komponentləri
- `src/data/homes.ts` — altı demo kirayə evi
- `src/pages/` — siyahı, detal, booking, login və rol panelləri
- `src/styles.css`, `src/auth-styles.css` — ümumi və auth/dashboard stilləri
- `public/images/` — demo şəkillər
