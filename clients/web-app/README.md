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

Public ev siyahısında `Sırala` dropdown-u var: `Standart`, `Yeni elanlar`, `Qiymət (artan)`, `Qiymət (azalan)` və `Ad (A-Z)`. Sıralama frontend-də yüklənmiş nəticələr üzərində tətbiq olunur, mövcud search/filter dəyərlərini dəyişmir və seçilmiş dəyər localStorage-da `daily-homes-public-property-sort` açarı ilə saxlanılır. `Standart` seçimi sıralamanı sıfırlayır.

Public ev kartlarında və property detail səhifəsində ürək düyməsi ilə lokal seçilmişlər idarə olunur. Seçilmiş property ID-ləri localStorage-da `daily-homes-favorite-property-ids` açarı ilə saxlanılır, səhifə açılışında bərpa olunur, invalid JSON təhlükəsiz boş siyahı kimi qəbul edilir və duplicate ID saxlanmır. Public list-də `Seçilmişlər` toggle-u cari search/filter/sort dəyərlərini sıfırlamadan yalnız seçilmiş elanları göstərir; boş nəticədə `Seçilmiş elan yoxdur.` mesajı çıxır. Login tələb olunmur, mock və live rejimlərdə eyni işləyir.

Date availability davranışı backend qaydasına uyğundur: manual broker blokları və aktiv/blocking booking-lər seçilmiş tarix aralığında evi siyahıdan çıxarır. Rejected və cancelled booking-lər tarixləri bloklamır; pending booking-lər hələ mövcud qaydaya əsasən bloklayır.

Public property detail səhifəsində `Paylaş` düyməsi var. Dəstəklənən cihazlarda browser-in Web Share API-si ilə ev başlığı və cari səhifə URL-i paylaşılır. Web Share dəstəklənmirsə, frontend cari URL-i clipboard-a kopyalayır və `Link kopyalandı.` mesajı göstərir. Linkə əlavə property məlumatı və query parametri yazılmır.

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

Customer `/account` rezervasiya siyahısında status filteri var. Seçilmiş status localStorage-da `daily-homes-customer-booking-filters` açarı ilə saxlanılır və müştəri səhifəyə qayıdanda bərpa olunur. `Filtrləri sıfırla` düyməsi UI filterini təmizləyir, saxlanmış dəyəri silir və tam rezervasiya siyahısını yenidən yükləyir. Mock və live rejimlərdə filter frontend-də yüklənmiş siyahı üzərində tətbiq olunur.

Customer booking detail səhifəsində `Linki kopyala` düyməsi cari browser URL-ni clipboard-a yazır. Uğurda `Link kopyalandı.` mesajı göstərilir; clipboard icazəsi alınmasa oxunaqlı xəta göstərilir. Linkdə əlavə booking məlumatı və query parametri yaradılmır.

Customer `/account/bookings/:id` səhifəsində aktiv rezervasiyalar üçün sadə ləğv sorğusu UI göstərilir. Göründüyü statuslar:

- `pending`
- `waiting_deposit`
- `confirmed`
- `paid`

`completed`, `rejected` və `cancelled` statuslarında ləğv sorğusu göstərilmir. Müştəri istəyə bağlı `Səbəb` yaza bilər; limit 1000 simvoldur. Mock rejimdə sorğu lokal state/localStorage ilə simulyasiya olunur və düymə göndərildikdən sonra bağlanır. Live rejimdə frontend `POST /api/account/bookings/{id}/cancellation-requests` endpoint-ini customer JWT ilə çağırır, uğurdan sonra booking detail-i yeniləyir və backend-dən gələn `cancelRequestSent` dəyərinə görə düyməni bağlı saxlayır.

Live rejimdə `/broker` mövcud JWT-ni Bearer token kimi göndərərək broker summary, ev və booking endpoint-lərindən real məlumat yükləyir. Broker yalnız öz evlərini və həmin evlərin rezervasiyalarını görür.

Broker dashboard-un yuxarısında beş kompakt summary kartı göstərilir: `Evlər`, `Aktiv elanlar`, `Aktiv rezervasiyalar`, `Gözləyən behlər`, `Ləğv sorğuları`. Kartlar `/api/broker/summary` cavabındakı saylardan istifadə edir. `Gözləyən behlər` kartı booking siyahısını `waiting_deposit` statusuna filterləyir; ləğv sorğuları üçün ayrıca list filteri olmadığı üçün kart rezervasiya siyahısına aparır. Mock rejim realistik demo saylar qaytarır.

Broker booking detail səhifəsində pending rezervasiyanı təsdiqləmək, rədd etmək və ya ləğv etmək olur. Confirmed və waiting-deposit rezervasiyaları ləğv edilə bilər. Live rejimdə bu düymələr `/api/broker/bookings/{id}/accept`, `/reject`, `/cancel` endpoint-lərini çağırır; mock rejimdə eyni status keçidləri lokal simulyasiya olunur. Rədd edilmiş və ləğv edilmiş rezervasiyalar availability-ni bloklamır; pending davranışı hələ backend qaydasına uyğun olaraq bloklayır.

Broker `/broker/bookings/:id` səhifəsində backend detail response-da `cancellationRequest` varsa “Müştəri ləğv sorğusu göndərib” paneli göstərilir. Panel statusu, səbəbi, göndərilmə vaxtını və istəyə bağlı `Qərar qeydi` sahəsini göstərir. Broker `Təsdiqlə` seçəndə live rejimdə `POST /api/broker/bookings/{bookingId}/cancellation-requests/{requestId}/approve` çağırılır, sorğu paneldən çıxır və booking statusu `cancelled` kimi yenilənir. `Rədd et` seçəndə `POST /api/broker/bookings/{bookingId}/cancellation-requests/{requestId}/reject` çağırılır, sorğu paneldən çıxır və booking statusu dəyişmir. Hər iki action-dan əvvəl browser confirmation göstərilir; qeyd limiti 1000 simvoldur. Mock rejimdə approve/reject lokal state-də simulyasiya olunur. Avtomatik refund/payment davranışı yoxdur.

Broker `/broker` dashboard rezervasiya siyahısında sadə filterlər var:

- status
- başlanğıc tarix
- bitiş tarix

Live rejimdə status və tarix filterləri `GET /api/broker/bookings` endpoint-inə query param kimi göndərilir. Response-da `hasPendingCancellationRequest` true olduqda kartda `Ləğv sorğusu` badge-i göstərilir. Mock rejimdə eyni filterlər lokal demo rezervasiyalar üzərində tətbiq olunur və bir demo rezervasiya ləğv sorğusu badge-i ilə göstərilir. Tarix filterində yalnız bir tarix seçilərsə və ya başlanğıc tarixi bitiş tarixindən sonra olarsa, frontend API çağırmadan oxunaqlı xəta göstərir. “Təmizlə” düyməsi filterləri sıfırlayır və bütün rezervasiyaları yenidən yükləyir.

Siyahı kartları booking ID, ev adı, müştəri adı/telefonu, tarix aralığı, gecə sayı, status və toplam məbləği göstərir. Status chip-ləri hazırda yüklənmiş nəticələrin qısa xülasəsidir; pagination və inkişaf etmiş axtarış bu mərhələyə daxil deyil.

Broker dashboard-da `Təqvim` düyməsi `/broker/calendar` səhifəsini açır. Live rejimdə frontend `GET /api/broker/calendar?from=YYYY-MM-DD&to=YYYY-MM-DD` endpoint-indən ay görünüşü üçün rezervasiya və manual availability block eventlərini yükləyir. Booking eventinə klik mövcud booking detail səhifəsinə, manual block eventinə klik həmin property edit səhifəsinə aparır. Mock rejimdə təqvim mövcud demo booking və availability block məlumatlarından yaradılır.

## Broker ev idarəetməsi

Broker panelində `Ev əlavə et` düyməsi `/broker/rental-homes/new` səhifəsini açır. Mövcud ev kartına klik `/broker/rental-homes/:id/edit` idarəetmə səhifəsinə aparır.

Broker property siyahısında elan adı/şəhər/ünvan üzrə axtarış və yayın statusu filteri var. Bu filterlər localStorage-da saxlanılır və broker panelinə qayıdanda bərpa olunur. `Filtrləri sıfırla` düyməsi filterləri təmizləyir və saxlanmış dəyərləri silir. Arxiv statusu hazırda frontend list response-da ayrıca field kimi gəlmədiyi üçün bu filter əlavə edilməyib.

Mock rejimdə yaratma, redaktə, publish/unpublish və media əməliyyatları lokal yaddaşda simulyasiya olunur. Live rejimdə frontend bu endpoint-ləri çağırır:

- `POST /api/broker/rental-homes`
- `GET /api/broker/rental-homes/{id}`
- `PUT /api/broker/rental-homes/{id}`
- `POST /api/broker/rental-homes/{id}/duplicate`
- `PATCH /api/broker/rental-homes/{id}/publish`
- `PATCH /api/broker/rental-homes/{id}/unpublish`
- `POST /api/broker/rental-homes/{id}/media`
- `PATCH /api/broker/rental-homes/{id}/media/{mediaId}/main`
- `DELETE /api/broker/rental-homes/{id}/media/{mediaId}`
- `GET /api/broker/rental-homes/{id}/availability-blocks`
- `POST /api/broker/rental-homes/{id}/availability-blocks`
- `DELETE /api/broker/rental-homes/{id}/availability-blocks/{blockId}`

Broker property list kartlarında `Duplikat yarat` düyməsi var. Live rejimdə frontend `POST /api/broker/rental-homes/{id}/duplicate` endpoint-ini çağırır və uğurdan sonra yeni qaralama elanın edit səhifəsinə keçir. Mock rejimdə property lokal demo state-də kopyalanır, booking və availability blokları kopyalanmır.

Şəkil upload-u JPG, PNG və WebP faylları üçün 5 MB limitlə işləyir. Live rejimdə yüklənmiş fayllar backend-in lokal `/uploads/rental-homes/...` URL-ləri ilə göstərilir. Production üçün private object storage, resize/compression və daha sərt content validation ayrıca mərhələdir.

Broker edit səhifəsində şəkilə klik full-screen önbaxış açır. Önbaxışda əvvəlki/növbəti şəkil düymələri, `Escape` ilə bağlama, sol/sağ oxlarla keçid, backdrop-a kliklə bağlama və `3 / 12` formatında mövqe göstəricisi var. Cari əsas şəkil ayrıca işarələnir; əsas olmayan şəkil üçün `Əsas şəkil et` düyməsi mövcud set-main endpoint/mock funksiyasını çağırır və media siyahısını yeniləyir.

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

Admin notification səhifəsində “Bildirişləri göndər” paneli var. Admin `Batch sayı` dəyərini 1-100 aralığında seçib `Pending bildirişləri emal et` düyməsi ilə vaxtı çatmış pending mesajları fake provider vasitəsilə emal edə bilər. Live rejimdə frontend `POST /api/admin/notifications/process-pending` endpoint-ini çağırır və uğurdan sonra siyahını yeniləyir. Mock rejimdə due pending demo mesajları lokal in-memory state-də `sent` olur, `FAIL_FAKE_PROVIDER` marker-li mesaj `failed` olur, future scheduled mesaj isə pending qalır.

Siyahı delivery nəticə sahələrini göstərir: `Göndərilmə vaxtı`, `Provider ID` və `Xəta`. Bu hələ fake-provider MVP-dir; real WhatsApp/SMS provider, retry/backoff və production delivery worker UI bu mərhələdə yoxdur.

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
