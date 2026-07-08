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

Homepage/public listing filterləri:

- açar söz (`q`)
- şəhər (`city`)
- rayon/qəsəbə (`district`)
- qonaq sayı (`guests`)
- minimum/maksimum qiymət (`minPrice`, `maxPrice`)
- uyğun tarix aralığı (`startDate`, `endDate`)

Filterlər URL query param-larında saxlanılır. Live rejimdə frontend bu param-ləri `GET /api/rental-homes` endpoint-inə göndərir. Mock rejimdə eyni əsas filterlər lokal demo data üzərində simulyasiya olunur. Tarix filterində yalnız bir tarix seçilərsə və ya başlanğıc bitişdən sonra olarsa, frontend API çağırmadan oxunaqlı xəta göstərir.

Date availability davranışı backend qaydasına uyğundur: manual broker blokları və aktiv/blocking booking-lər seçilmiş tarix aralığında evi siyahıdan çıxarır. Rejected və cancelled booking-lər tarixləri bloklamır; pending booking-lər hələ mövcud qaydaya əsasən bloklayır.

Customer `/account` səhifəsi müştərinin öz rezervasiyalarını kart formatında göstərir: ev adı, şəhər/rayon, tarix sayı, toplam məbləğ, booking statusu, beh/qəbz statusu və növbəti addım. `/account/bookings/:id` səhifəsi ev xülasəsi, seçilmiş tarixlər, qonaq sayı, qiymət, status izahı və beh məlumatlarını daha detallı göstərir.

Customer-visible status davranışı:

- Pending: broker təsdiqi gözlənilir.
- Confirmed: broker rezervasiyanı təsdiqləyib.
- Rejected/cancelled: rezervasiya aktiv deyil, əlavə əməliyyat yoxdur.
- Deposit requested: qəbz yükləmək lazımdır.
- Receipt uploaded: broker qəbzi yoxlayır.
- Deposit approved: beh qəbul edilib.
- Deposit rejected: `allowReupload` true olduqda yeni qəbz yükləmək mümkündür.

Mock rejimdə account və qəbz upload flow-u lokal state ilə simulyasiya olunur. Live rejimdə frontend `/api/account/bookings`, `/api/account/bookings/{id}` və `/api/account/bookings/{id}/deposit/receipt` endpoint-lərini çağırır.

Live rejimdə `/broker` mövcud JWT-ni Bearer token kimi göndərərək broker summary, ev və booking endpoint-lərindən real məlumat yükləyir. Broker yalnız öz evlərini və həmin evlərin rezervasiyalarını görür.

Broker booking detail səhifəsində pending rezervasiyanı təsdiqləmək, rədd etmək və ya ləğv etmək olur. Confirmed və waiting-deposit rezervasiyaları ləğv edilə bilər. Live rejimdə bu düymələr `/api/broker/bookings/{id}/accept`, `/reject`, `/cancel` endpoint-lərini çağırır; mock rejimdə eyni status keçidləri lokal simulyasiya olunur. Rədd edilmiş və ləğv edilmiş rezervasiyalar availability-ni bloklamır; pending davranışı hələ backend qaydasına uyğun olaraq bloklayır.

## Broker ev idarəetməsi

Broker panelində `Ev əlavə et` düyməsi `/broker/rental-homes/new` səhifəsini açır. Mövcud ev kartına klik `/broker/rental-homes/:id/edit` idarəetmə səhifəsinə aparır.

Mock rejimdə yaratma, redaktə, publish/unpublish və media əməliyyatları lokal yaddaşda simulyasiya olunur. Live rejimdə frontend bu endpoint-ləri çağırır:

- `POST /api/broker/rental-homes`
- `GET /api/broker/rental-homes/{id}`
- `PUT /api/broker/rental-homes/{id}`
- `PATCH /api/broker/rental-homes/{id}/publish`
- `PATCH /api/broker/rental-homes/{id}/unpublish`
- `POST /api/broker/rental-homes/{id}/media`
- `PATCH /api/broker/rental-homes/{id}/media/{mediaId}/main`
- `DELETE /api/broker/rental-homes/{id}/media/{mediaId}`
- `GET /api/broker/rental-homes/{id}/availability-blocks`
- `POST /api/broker/rental-homes/{id}/availability-blocks`
- `DELETE /api/broker/rental-homes/{id}/availability-blocks/{blockId}`

Şəkil upload-u JPG, PNG və WebP faylları üçün 5 MB limitlə işləyir. Live rejimdə yüklənmiş fayllar backend-in lokal `/uploads/rental-homes/...` URL-ləri ilə göstərilir. Production üçün private object storage, resize/compression və daha sərt content validation ayrıca mərhələdir.

Broker edit səhifəsində sadə “Uyğun olmayan tarixlər” bölməsi var. Start/end date və broker qeydi ilə tarix aralığı bloklana bilər; qeyd public müştəri ekranında göstərilmir. Mock rejimdə bloklar lokal state-də saxlanır, live rejimdə backend endpoint-ləri çağırılır. Public booking form backend-dən gələn unavailable ranges əsasında uyğun olmayan tarixləri boz/disabled göstərir və backend overlap xətasını oxunaqlı mesajla göstərir.

## Beh axını

Broker `/broker/bookings/:id` səhifəsində məbləğ, gələcək son tarix, maskalanmış kart, bank və qeyd ilə beh istəyə bilər. Customer `/account` bölməsində öz telefonuna/hesabına bağlı rezervasiyaları, `/account/bookings/:id` səhifəsində beh təlimatını görür və JPG/PNG/WebP qəbz şəkli yükləyir. Broker yüklənmiş qəbzi təsdiq və ya rədd edə bilər. Mock rejimdə eyni ekranlar və state keçidləri backend olmadan işləyir.

Bu MVP real ödəniş etmir. Live receipt faylları backend-in lokal `wwwroot/uploads/deposit-receipts` qovluğunda saxlanılır. Production üçün private object storage və authorization-aware download tələb olunur. Frontend və backend yalnız maskalanmış kart dəyəri qəbul edir; tam PAN yazılmamalıdır.

Beh əməliyyatlarından sonra frontend bildirişin növbəyə alındığını göstərir. Mock rejimdə bu mesaj lokal simulyasiya olunur; live rejimdə backend `outbound_messages` cədvəlinə qeyd yazır. Bu MVP-də tam notification UI və real WhatsApp/SMS provider yoxdur.

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
