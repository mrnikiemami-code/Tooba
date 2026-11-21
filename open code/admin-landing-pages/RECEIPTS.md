# Receipts — ممیزی صفحات فروشگاه

تاریخ: ۲۰۲۶-۱۱-۲۱  
HEAD زمان بررسی: `d3de64c2c`  
Host: `http://127.0.0.1:5088`  
Frontend: `http://127.0.0.1:3000`

## 1. Live UI — فهرست

```text
GET http://127.0.0.1:3000/admin/landing-pages
=> 200

DOM:
  data-testid="admin-landing-pages" = true
  h1 = "صفحات فروشگاه"
  rows = 10
  total = 60
  action nodes = 30
  overflow = false
```

در 390:

```text
docWidth = 390
view = 390
overflowX = false
gridClient = 356
gridScroll = 356
list action buttons = 32x32
create button height = 36
```

## 2. Live UI — Create → Section → SEO → Publish

```text
POST create via UI
=> routed to /admin/landing-pages/{pageId}
pageId = 01a1fc1a-ecc6-7000-9e07-a2033d6664b3* (temporary, deleted)
status = Draft (after save-as-draft)
sections = 0
```

افزودن بخش:

```text
wizard choice = add-section-text (RichText)
variant = selected
settings = defaults
save section => 1 section
readiness = 2/4
```

SEO + save:

```text
SEO title = "LP Review UI SEO"
SEO description = "Automated admin landing-page review draft."
readiness = 4/4
save => 200
```

Publish from UI:

```text
status badge = "منتشرشده"
page.status = Published
```

Preview page:

```text
GET /fa/admin/landing-pages/{id}/preview
=> 200
has preview banner = true
has RichText content = true
```

Public page:

```text
GET /fa/landing/lp-review-ui-1795230908408-u6dnh
=> 200
title = "LP Review UI SEO"
h1 = "LP Review UI 1795230908408"
canonical = "/fa/landing/lp-review-ui-1795230908408-u6dnh"
robots = "index, follow"
body contains RichText = true
document overflow = false
```

در 390:

```text
docWidth = 390
view = 390
overflowX = false
```

حذف از UI:

```text
Delete dialog shown
confirm delete
=> redirect to /admin/landing-pages
temporary page removed
```

## 3. Backend API — Publish Bypass

Temporary page created via API, then:

```text
PUT /v1/admin/pages/{id}/status
body: {"status":"Published"}
=> 200

GET /v1/admin/pages/{id}/sections
=> 200
=> []

GET /v1/storefront/pages/{slug}?locale=fa
=> 200
```

نتیجه: صفحهٔ بدون بخش و بدون SEO از API منتشر شد.

## 4. Backend API — Composition Non-Atomicity

```text
create page => 200
add RichText section => 200
GET sections => count 1

PUT /v1/admin/pages/{id}/sections/composition
body:
[
  {"sectionType":"RichText","config":"{\"title\":\"new\",\"text\":\"new\"}"},
  {"sectionType":"Hero","config":"{}"}
]
=> 400

GET sections
=> count 0
```

نتیجه: بخش قبلی حذف شد، بخش‌های جدید ذخیره نشدند.

## 5. Backend API — Home Dead End

```text
POST /v1/admin/pages
body: {"title":"...","slug":"...","locale":"fa","pageType":"Home"}
=> 200
pageType = Home
status = Draft

PUT /v1/admin/pages/home
body: {"homePageId":"{same pageId}"}
=> 400
```

نتیجه: صفحهٔ Home ساخته می‌شود، ولی نمی‌تواند خانه شود.

## 6. Automated Tests

Backend:

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Release \
  --filter "FullyQualifiedName~StoreLandingPage|FullyQualifiedName~StorePagesFoundation|FullyQualifiedName~PromotionCampaignSource|FullyQualifiedName~HostAdminAmcStoreLandingGuard"

Result:
  Failed: 1
  Passed: 34
  Total: 35
```

شکست:

```text
HostAdminAmcStoreLandingGuardTests.Host_Admin_count_15_StoreAppearance_evacuated
Expected: 15
Actual: 17
```

Frontend:

```text
node --experimental-strip-types --test app/admin/landing-pages/*.test.ts

tests 105
pass 97
fail 8
```

Typecheck:

```text
npx tsc --noEmit
=> exit 0
```

## 7. Data Snapshot

```text
GET /v1/admin/pages

total = 60
published = 41
draft = 19
indexablePublished = 40
home = 1
testLike = 53

sitemap.xml loc count = 95
contains r21-amazing = true
contains landing-demo = true
```

## 8. Public Payload Size

صفحه‌ای با فقط یک بخش RichText:

```text
GET /v1/storefront/pages/lp-review-ui-1795230908408-u6dnh?locale=fa
status = 200
bytes = 16369
categories = 116
sections = 1
products = 0
brands = 0
articles = 0
reviews = 0

public page HTML bytes = 108675
```

## 9. Live Preview Latency Observation

در Wizard:

```text
after ~3s: 6 preview tiles still show "در حال آماده‌سازی پیش‌نمایش…"
after ~10s: loading = []
Network failure observed:
  GET http://127.0.0.1:3000/api/customer/wishlist/membership
  => 500
```

## 10. Cleanup

تمام صفحات تستی زیر حذف شدند:

```text
lp-review-*
TOTAL=60
TEST_LEFT=0
```

## 11. Auth Probe

```text
GET /v1/admin/pages بدون actor => 401
GET /v1/admin/pages با seller actor => 403
```

## 12. Source References

- `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/StoreLandingPageDirectory.cs:81`
- `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/StoreLandingPageDirectory.cs:193`
- `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/StoreLandingPageDirectory.cs:207`
- `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/StoreLandingPageWorkspace.cs:441`
- `src/frontend/app/admin/landing-pages/admin-landing-page-composer.tsx:866`
- `src/frontend/app/admin/admin-shell.tsx:79`
- `src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/ICatalogAdminAuthorizer.cs:16`
- `src/backend/Host/Tooba.Host/Admin/Access/AdminPanelAccess.cs:41`
- `src/frontend/design-system/app-data-grid/app-grid-row-actions.tsx:55`
