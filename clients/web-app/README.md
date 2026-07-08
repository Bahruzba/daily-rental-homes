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

Broker `/broker` dashboard rezervasiya siyahısında sadə filterlər var:

- status
- başlanğıc tarix
- bitiş tarix

Live rejimdə status və tarix filterləri `GET /api/broker/bookings` endpoint-inə query param kimi göndərilir. Mock rejimdə eyni filterlər lokal demo rezervasiyalar üzərində tətbiq olunur. Tarix filterində yalnız bir tarix seçilərsə və ya başlanğıc tarixi bitiş tarixindən sonra olarsa, frontend API çağırmadan oxunaqlı xəta göstərir. “Təmizlə” düyməsi filterləri sıfırlayır və bütün rezervasiyaları yenidən yükləyir.

Siyahı kartları booking ID, ev adı, müştəri adı/telefonu, tarix aralığı, gecə sayı, status və toplam məbləği göstərir. Status chip-ləri hazırda yüklənmiş nəticələrin qısa xülasəsidir; pagination və inkişaf etmiş axtarış bu mərhələyə daxil deyil.

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

## Broker booking xərcləri

Broker `/broker/bookings/:id` səhifəsində “Xərclər” bölməsindən rezervasiya üzrə daxili xərcləri idarə edə bilər. UI göstərir:

- xərc siyahısı
- xərc növü, başlıq, məbləğ, qeyd və yaradılma vaxtı
- xərc əlavə/redaktə formu
- xərc redaktə və silmə düymələri
- rezervasiya məbləği, cəmi xərclər və təxmini mənfəət

Live rejimdə istifadə olunan endpoint-lər:

- `GET /api/broker/bookings/{bookingId}/expenses`
- `POST /api/broker/bookings/{bookingId}/expenses`
- `PUT /api/broker/bookings/{bookingId}/expenses/{expenseId}`
- `DELETE /api/broker/bookings/{bookingId}/expenses/{expenseId}`

Mövcud xərc üçün “Düzəliş et” seçildikdə xərc məlumatları eyni forma yüklənir, submit düyməsi “Yadda saxla” olur və “Ləğv et” ilə edit rejimindən çıxmaq mümkündür. Mock rejimdə əlavə/redaktə/silmə lokal demo state/localStorage ilə simulyasiya olunur. Detallı report dashboard, cədvəl, chart-lar və inkişaf etmiş xərc kateqoriya lüğəti ayrıca mərhələdə əlavə olunmalıdır.

## Broker hesabat xülasəsi

Broker `/broker` panelində “Hesabat xülasəsi” bölməsi ümumi bron məbləği, ümumi xərclər, təxmini mənfəət, bron sayı, gəlirə daxil bronlar, təmizlik xərci, ev sahibinə ödəniş və digər xərcləri kart formatında göstərir.

Live rejimdə istifadə olunan endpoint:

- `GET /api/broker/reports/summary`

Tarix filteri `from` və `to` query parametrlərini dəstəkləyir. Hər iki tarix boş olduqda bütün xülasə yüklənir. Yalnız bir tarix seçilərsə və ya başlanğıc tarixi bitiş tarixindən sonra olarsa, frontend API çağırmadan oxunaqlı Azərbaycan dilində xəta göstərir. “Təmizlə” düyməsi filterləri sıfırlayır və bütün xülasəni yenidən yükləyir.

Mock rejimdə dashboard demo rezervasiya və xərc məlumatları əsasında hesabat xülasəsini lokal hesablayır. Bu mərhələ yalnız summary kartlarını əhatə edir; detallı report cədvəli, chart-lar və export funksiyaları hələ yoxdur.

## Admin notification outbox

Admin `/admin` panelində “Bildirişlər” kartı `/admin/notifications` səhifəsini açır. Bu ekran WhatsApp/SMS üçün növbəyə alınmış notification outbox mesajlarını read-only siyahı kimi göstərir.

Live rejimdə istifadə olunan endpoint:

- `GET /api/admin/notifications`

Filterlər:

- `status`
- `type`
- `bookingId`

Boş filterlər endpoint-i query param olmadan çağırır. `bookingId` yalnız rəqəm olmalıdır; səhv dəyər yazılarsa frontend API çağırmadan Azərbaycan dilində validation mesajı göstərir. “Təmizlə” düyməsi filterləri sıfırlayır və bütün bildirişləri yenidən yükləyir.

Mock rejimdə Admin UI backend olmadan demo outbox data göstərir: pending `booking_created`, pending `deposit_requested`, sent `deposit_approved`, failed `booking_status_changed`. Mock filter status, type və bookingId üzrə işləyir.

Limit: ekran read-only-dir. Real WhatsApp/SMS provider, retry/send worker və manual resend əməliyyatı bu mərhələyə daxil deyil.

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
