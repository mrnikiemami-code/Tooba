# ممیزی بخش صفحات فروشگاه (`/admin/landing-pages`)

تاریخ: ۲۰۲۶-۱۱-۲۱  
محدوده: Backend صفحات فروشگاه + UI واقعی Admin + رندر عمومی Landing  
وضعیت: **نیازمند اصلاح پیش از Production**  
نوع سند: Report only؛ هیچ کد یا Schema تغییر نکرد.

## ۱. خلاصه اجرایی

مسیر اصلی UI کار می‌کند و سناریوی واقعی زیر با موفقیت طی شد:

`ایجاد پیش‌نویس → افزودن بخش → تکمیل SEO → انتشار → پیش‌نمایش ادمین → صفحه عمومی → حذف`

اما سه ایراد جدی وجود دارد:

1. **انتشار صفحه خالی/بدون SEO از API ممکن است.**
2. **جایگزینی ترکیب بخش‌ها غیراتمیک است و می‌تواند محتوای قبلی را پاک کند.**
3. **ساخت صفحه با نوع Home یک مسیر بن‌بست است.**

همچنین تست‌های Guard این بخش قرمز هستند و دادهٔ محیط فعلی شامل صفحات تستی منتشرشده و ایندکس‌پذیر است.

## ۲. آنچه واقعاً تست شد

| مورد | نتیجه |
|---|---|
| `GET /admin/landing-pages` روی 1440 | 200، فهرست ۶۰ صفحه، ۱۰ ردیف در صفحه |
| همان صفحه روی 390 | 200، بدون overflow افقی |
| ایجاد صفحه خالی از UI | موفق |
| افزودن بخش RichText از Wizard | موفق |
| تکمیل SEO و ذخیره | موفق |
| انتشار از UI | موفق؛ وضعیت Published شد |
| پیش‌نمایش ادمین | 200 |
| صفحه عمومی `/fa/landing/<slug>` | 200 با title/H1/canonical/robots صحیح |
| بازگشت به پیش‌نویس | 200؛ صفحه عمومی API 404 شد |
| حذف از UI | موفق؛ بازگشت به فهرست |
| پاک‌سازی صفحات تستی | انجام شد؛ `TEST_LEFT=0` |

صفحات تستی موقت در پایان حذف شدند. تعداد صفحات محیط به ۶۰ صفحه قبلی بازگشت.

## ۳. نتایج Automated Tests

| Suite | نتیجه |
|---|---|
| Backend focused landing-page tests | **34/35 pass** |
| Frontend landing-page guard tests | **97/105 pass، 8 fail** |
| `npx tsc --noEmit` | **0 error** |

### شکست Backend

`HostAdminAmcStoreLandingGuardTests.Host_Admin_count_15_StoreAppearance_evacuated`

- انتظار: `15`
- واقعی: `17`

این شکست مربوط به شمارش فایل‌های Admin است، نه رفتار صفحه. ولی Suite را قرمز می‌کند.

### شکست‌های Frontend

- `keeps every implemented Variant human-named`
- `template/variant previews: banner slots stay layout-aware; picker uses live components`
- `Banner editor geometry matches variant metadata`
- `admin-builder-ux-r4.guard.test.ts` → فایل قدیمی `Tooba.Catalog.Domain/TemplateCatalog.cs` را می‌خواند
- `admin-builder-ux-r7.guard.test.ts` → فایل قدیمی `Tooba.Media.Domain/MediaAsset.cs` را می‌خواند
- `section chooser has Persian labels and no raw JSON editor` → انتظار «بنر اصلی» دارد
- `TB-P10-T022 ordinary-user Admin UX...` → وجود `JSON.stringify` را رد می‌کند، ولی برای `sessionStorage` استفاده می‌شود
- `admin-store-pages-r10.guard.test.ts` → فایل‌های حذف‌شدهٔ `Host/Admin/StoreLandingPageComposer.cs` و `StoreLandingPageEndpoints.cs` را می‌خواند

بیشتر این‌ها تست‌های کهنه‌اند؛ اما در وضعیت فعلی نمی‌توان گفت Suite این بخش سبز است.

## ۴. یافته‌ها

### P0 — انتشار بدون آمادگی از API

**شناسه:** `LP-AUD-001`  
**محل:** `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/StoreLandingPageDirectory.cs:81`

UI چک‌لیست آمادگی دارد، اما endpoint زیر هیچ بررسی سروری انجام نمی‌دهد:

`PUT /v1/admin/pages/{pageId}/status`

شاهد واقعی:

- صفحه‌ای بدون بخش و بدون SEO ساخته شد.
- درخواست `{"status":"Published"}` ارسال شد.
- پاسخ: **200**
- `GET .../sections`: `[]`
- `GET /v1/storefront/pages/<slug>?locale=fa`: **200**

نتیجه: UI جلوی انتشار ناقص را می‌گیرد، ولی API این محافظ را ندارد. هر کلاینت دیگر یا اسکریپت می‌تواند صفحهٔ خالی را عمومی کند.

**پیشنهاد:** همان قواعد `buildLandingPublishReadiness` باید به یک validator/خدمت سروری مشترک منتقل شود و قبل از `Publish` در backend اجرا شود. برای Home نیز حداقل یک بخش فعال و SEO الزامی باید سروری بررسی شود.

---

### P0 — جایگزینی ترکیب بخش‌ها غیراتمیک است

**شناسه:** `LP-AUD-002`  
**محل:** `StoreLandingPageDirectory.cs:193` و حذف قبلی در خط `207`

`ReplaceCompositionAsync` ابتدا بخش‌های قبلی را حذف و `SaveChanges` می‌کند، سپس بخش‌های جدید را می‌سازد و اعتبارسنجی می‌کند.

شاهد واقعی:

- صفحه با ۱ بخش معتبر.
- درخواست composition با دو عضو:
  - `RichText` معتبر
  - `Hero` نامعتبر (بدون title)
- پاسخ: **400**
- تعداد بخش‌ها: **۱ → ۰**

نتیجه: یک درخواست نامعتبر می‌تواند ترکیب قبلی صفحه را از بین ببرد. این برای «اعمال قالب» یک ریسک داده‌ای جدی است.

**پیشنهاد:** کل عملیات باید در یک تراکنش باشد: اعتبارسنجی کامل همهٔ payloadها قبل از حذف، یا حذف و درج در یک `SaveChanges`/Transaction با rollback.

---

### P1 — ساخت Home یک مسیر بن‌بست است

**شناسه:** `LP-AUD-003`  
**محل UI:** `src/frontend/app/admin/landing-pages/admin-landing-page-composer.tsx:866`  
**محل Backend:** `StoreLandingPageDirectory.cs:134`

UI گزینهٔ «خانه (Home)» را در ساخت صفحه نشان می‌دهد. Backend هم `pageType=Home` را می‌پذیرد. اما:

- `SetHome` فقط صفحهٔ `Landing` منتشرشده را قبول می‌کند (`IsEligibleHome`).
- صفحهٔ ساخته‌شده با نوع `Home` نه به‌عنوان خانه انتخاب می‌شود، نه از مسیر `/landing/{slug}` عمومی می‌شود.
- UI ویرایشگر دکمهٔ «تنظیم به عنوان صفحه اصلی» را فقط برای `Landing` نشان می‌دهد.

شاهد واقعی:

- ساخت صفحه با `pageType=Home` → 200
- `PUT /v1/admin/pages/home` با همان id → **400**

**پیشنهاد:** یا گزینهٔ ساخت Home حذف شود و فقط «Landing → Publish → Set as Home» مسیر رسمی باشد، یا ساخت Home مستقیماً selection خانه را انجام دهد و برای آن invariant سروری وجود داشته باشد.

---

### P1 — تست‌های Guard این بخش سبز نیستند

**شناسه:** `LP-AUD-004`

- Backend: ۱ شکست شمارش فایل.
- Frontend: ۸ شکست Guard.

مشکل اصلی، نگهداری تست‌هاست: چند تست هنوز فایل‌های حذف‌شدهٔ Host/Media/Catalog را می‌خوانند و چند تست به متن/مارکرهای قدیمی وابسته‌اند.

**پیشنهاد:** تست‌های Guard باید به مالک جدید (Catalog/Endpoints/Infrastructure) و مارکرهای فعلی UI منتقل شوند. شکست شمارش فایل Host هم باید با وضعیت واقعی Admin هم‌راستا شود.

---

### P1 — دادهٔ محیط فعلی شامل صفحات تستی منتشرشده است

**شناسه:** `LP-AUD-005`

وضعیت واقعی در `tooba_alpha` در زمان ممیزی:

- کل صفحات: **60**
- منتشرشده: **41**
- ایندکس‌پذیر: **40**
- صفحات شبیه تست: **53**
- `sitemap.xml`: حدود **95 loc**، شامل `r21-amazing` و `landing-demo`

نتیجه: صفحات تستی در sitemap و ویترین عمومی قابل مشاهده‌اند. این یک مشکل داده/عملیات است، نه باگ کد، اما پیش از Production باید پاک‌سازی یا آرشیو شود.

---

### P2 — همهٔ دسته‌بندی‌ها در هر Landing برگردانده می‌شوند

**شناسه:** `LP-AUD-006`  
**محل:** `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/StoreLandingPageWorkspace.cs:441`

صفحهٔ تستی فقط یک بخش `RichText` داشت، ولی پاسخ عمومی شامل **۱۱۶ دسته** بود:

- API bytes: **16,369**
- Public HTML bytes: **108,675**

`ToPublicAsync` همیشه `_shell.ListCategoriesAsync()` را صدا می‌زند، حتی وقتی هیچ `CategoryGrid` وجود ندارد. این برای payload و رندر مقیاس‌پذیر نیست.

**پیشنهاد:** مانند `needsBrands` و `needsArticles`، دسته‌ها فقط وقتی بارگذاری شوند که یک بخش به آن‌ها نیاز دارد؛ یا اگر Shell/Fotter واقعاً به آن‌ها نیاز دارد، در یک endpoint/کامپوننت جدا و کش‌شده تأمین شود.

---

### P2 — مجوز API با مجوز منو هم‌راستا نیست

**شناسه:** `LP-AUD-007`

- منوی UI برای این صفحه: `pagecomposition.view` در `src/frontend/app/admin/admin-shell.tsx:79`
- Gate واقعی endpoint: `ICatalogAdminAuthorizer` → `IAdminPanelAccess` → فقط `tenant view`

یعنی از نظر Source، هر کاربری که فقط `tenant view` داشته باشد می‌تواند API صفحات را صدا بزند، حتی اگر `pagecomposition.view/manage` نداشته باشد.

**پیشنهاد:** یا Gate سروری به `pagecomposition.view`/`pagecomposition.manage` منتقل شود، یا اگر Admin Panel عمداً سطح دسترسی گسترده‌تری دارد، این تصمیم صریح مستند و تست شود.

---

### P2 — اندازهٔ کنترل‌ها در موبایل

**شناسه:** `LP-AUD-008`

اندازه‌های واقعی در 390:

- دکمه‌های Actions فهرست: **32×32**
- دکمه‌های اصلی ویرایشگر: حدود **38px** ارتفاع
- بخشی از کنترل‌های داخل بخش: 44px

برای معیار 44px، Actions فهرست و Toolbar کوتاه‌ترند.  
**محل:** `src/frontend/design-system/app-data-grid/app-grid-row-actions.tsx:55` (`size-8`).

---

### P2 — Previewهای Wizard کند و ناپایدارند

**شناسه:** `LP-AUD-009`

در باز کردن Wizard:

- ۶ پیش‌نمایش بیش از ۳ ثانیه در حالت «در حال آماده‌سازی پیش‌نمایش…» ماندند.
- پس از حدود ۱۰ ثانیه کامل شدند.
- یک درخواست `GET /api/customer/wishlist/membership` پاسخ **500** داد.

مانع اصلی ایجاد بخش نبود، اما UX و پایداری Preview خوب نیست. این مورد نیاز به تفکیک دارد: خطای Wishlist ممکن است از Storefront preview بیاید و مستقل از Landing باشد.

---

### P2 — نبود Optimistic Concurrency برای ویرایش صفحه/بخش

**شناسه:** `LP-AUD-010`

هیچ Version/RowVersion/ExpectedUpdatedAt در مسیر Update صفحه یا بخش وجود ندارد. دو ادمین هم‌زمان می‌توانند تغییرات یکدیگر را بازنویسی کنند (Last-Write-Wins).

**پیشنهاد:** اگر ویرایش چندکاربره واقعی است، `UpdatedAt` یا یک Version صریح به عنوان concurrency token اضافه شود.

---

### P3 — مشاهدهٔ Stale SSR پس از تغییر مستقیم API

**شناسه:** `LP-AUD-011`

در تست مستقیم API:

- `PUT /status` برای `Draft` → 200
- `GET /v1/storefront/pages/<slug>` → 404
- ولی `GET /fa/landing/<slug>` تا زمان Revalidate همچنان **200** با محتوای قبلی بود.

در مسیر UI ادمین، `invalidatePublicStorePageCache` صدا زده می‌شود و حذف/انتشار از UI کش را می‌شکند. اما اگر API مستقیم یا کلاینت دیگری تغییر دهد، SSR تا TTL (۶۰ ثانیه) می‌تواند stale بماند.

این یک محدودیت عملیاتی است، نه لزوماً باگ مسیر UI.

---

### P3 — Truncate بی‌صدای فیلدهای متنی

**شناسه:** `LP-AUD-012`

در Domain، طول‌های Title/SEO/OG/H1 به‌جای رد کردن، بریده می‌شوند:

- `NormalizeTitle` → `value[..TitleMaxLength]`
- `NormalizeOptional` → `value[..max]`

UI هیچ MaxLength یا هشدار متناظری نشان نمی‌دهد. اپراتور ممکن است متن بلند وارد کند و بدون پیام، متن کوتاه‌شده ذخیره شود.

**پیشنهاد:** یا MaxLength در UI و Validator اعمال شود، یا Truncate به یک خطای قابل‌فهم تبدیل شود.

## ۵. محدودیت‌های این ممیزی

- بررسی روی محیط محلی `tooba_alpha` انجام شد، نه Production.
- `Set as Home` روی صفحات واقعی اجرا نشد تا وضعیت خانهٔ فروشگاه تغییر نکند.
- تست Drag/Drop و اعمال قالب از UI انجام نشد؛ اما رفتار Backend آن با API بررسی شد.
- صفحات تستی ایجادشده در پایان حذف شدند و هیچ صفحهٔ جدیدی باقی نماند.
- هیچ فایل Landing Page تغییر نکرد.

## ۶. اولویت پیشنهادی اصلاح

1. `LP-AUD-001` — Gate سروری آمادگی انتشار.
2. `LP-AUD-002` — اتمیک کردن Replace Composition.
3. `LP-AUD-003` — رفع تناقض ساخت Home.
4. `LP-AUD-005` — پاک‌سازی/آرشیو صفحات تستی و sitemap.
5. `LP-AUD-004` — اصلاح یا حذف تست‌های Guard کهنه.
6. `LP-AUD-006` — کاهش payload دسته‌بندی‌ها.
7. `LP-AUD-007` — هم‌راستایی مجوز سرور با `pagecomposition.*`.
8. `LP-AUD-008` — اندازهٔ کنترل‌ها در موبایل.
9. `LP-AUD-009` — پایدارسازی Previewهای Wizard.
10. `LP-AUD-010` — افزودن Concurrency Guard در صورت نیاز چندکاربره.

## ۷. فایل‌های کلیدی بررسی‌شده

- `src/frontend/app/admin/landing-pages/admin-landing-pages-screen.tsx`
- `src/frontend/app/admin/landing-pages/admin-landing-page-composer.tsx`
- `src/frontend/app/admin/landing-pages/admin-landing-pages-api.ts`
- `src/frontend/app/admin/landing-pages/admin-landing-page-readiness.ts`
- `src/frontend/app/admin/landing-pages/admin-section-wizard.tsx`
- `src/frontend/app/admin/landing-pages/admin-landing-section-forms.tsx`
- `src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/StoreLandingPages/CatalogStoreLandingPageAdminEndpoints.cs`
- `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/StoreLandingPageWorkspace.cs`
- `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/StoreLandingPageDirectory.cs`
- `src/backend/Modules/Catalog/Tooba.Catalog.Domain/StoreLandingPages/StoreLandingPage.cs`
- `src/backend/Modules/Catalog/Tooba.Catalog.Domain/StoreLandingPages/StoreLandingPageSectionConfig.cs`
- `src/backend/Modules/Catalog/Tooba.Catalog.Application/StoreLandingPages/**`
- `src/frontend/app/admin/landing-pages/admin-*.guard.test.ts`
