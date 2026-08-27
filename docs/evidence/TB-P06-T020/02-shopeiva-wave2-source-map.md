# 02 — Shopeiva Wave 2 UI source map (TB-P06-T020)

Reference root: `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva`

Scope: Vendor/Seller coupons·promotions·discounts, Vendor reviews, Vendor notifications, Customer Account notifications. Exact routes, components, CSS/Tailwind patterns, list/form/modal patterns, badges, responsive notes. Tooba port status under `D:\Users\User\source\repos\SarvNewVer\src\frontend`.

Shared brand/panel tokens used across these surfaces:

| Token | Value / pattern |
|---|---|
| Accent | `#E53935` (hover `#c62828`) |
| Card surface | `bg-white dark:bg-[#111] rounded-2xl border border-gray-200 dark:border-gray-800` |
| Panel page bg | `min-h-screen bg-gray-50 dark:bg-[#0a0a0a]` |
| Icon chip | `w-10 h-10 rounded-xl bg-[#E53935]/10` + lucide icon `text-[#E53935]` |
| Primary CTA | `bg-[#E53935] text-white rounded-xl … shadow-lg shadow-[#E53935]/30` |
| Focus ring | `focus:ring-2 focus:ring-[#E53935]` |
| Digits | local `toPersianDigits()` helper in each list component |

---

## 1. Vendor — Coupons / promotions / discounts

### Verdict

**PRESENT** as a single Vendor surface labeled **تخفیف‌ها** (coupons = discounts). There is **no separate** Vendor route for “promotions” or “marketing campaigns.” Storefront merchandising pages (`/coupons`, `/offers`, `/sale`) are **not** Vendor management UIs.

### Routes

| Route name | App file | Renders |
|---|---|---|
| `/vendor-panel/coupons` | `src/app/(vendor)/vendor-panel/coupons/page.jsx` | `CouponsList` |
| `/vendor-panel/coupons/new` | `src/app/(vendor)/vendor-panel/coupons/new/page.jsx` | `CouponForm` |
| `/vendor-panel/coupons/[id]` | `src/app/(vendor)/vendor-panel/coupons/[id]/page.jsx` | **Broken stub** — file body is a copy of `customers/[id]` (`CustomerDetail`); not a coupon editor |

Nav entry (sidebar): `src/app/(vendor)/vendor-panel/layout.jsx` → `{ id: 'coupons', label: 'تخفیف‌ها', icon: Tag, href: '/vendor-panel/coupons' }`.

### Components (canonical sources)

| Role | Path |
|---|---|
| List | `src/components/vendor/panel/coupons/couponsList.jsx` |
| Create form | `src/components/vendor/panel/coupons/couponForm.jsx` |
| Barrel | `src/components/vendor/panel/coupons/index.js` |
| Shared UI deps | `src/components/ui/customSelect` · `src/components/ui/persianCalendar/persianCalendar` |

### List pattern (`couponsList.jsx`)

- Header: Tag icon chip + title **تخفیف‌ها** + subtitle counts + CTA Link to `/vendor-panel/coupons/new`.
- Stats strip: `grid grid-cols-3` cards — کل / فعال (`text-emerald-500`) / منقضی (`text-red-500`).
- Search + filter: Fuse.js on `code|type|status`; filter dropdown `all | فعال | منقضی`; layout `flex-col sm:flex-row`.
- Cards: `grid grid-cols-1 md:grid-cols-2`; active coupons get `border-2 border-[#E53935]`; expired `opacity-70`.
- Per card: mono code + Copy; discount + type + uses/maxUses; usage progress bar; Edit/Delete icon buttons.
- Pagination: `ReactPaginate`, 4 items/page; active page `!bg-[#E53935]`.
- Delete: **browser `confirm()`** — no React modal.
- Edit navigation: `router.push(`/vendor-panel/coupons/${id}/edit`)` → **MISSING route** (`…/edit` does not exist; `[id]` is mis-wired). Closest working form pattern: `couponForm.jsx` + `…/coupons/new`.

### Form pattern (`couponForm.jsx`)

- Card shell `max-w-2xl mx-auto` with gradient header `from-[#E53935]/5`.
- `react-hook-form` + `zod` (`code`, `discount`, `type`, `expires`, `maxUses`).
- Fields: Tag-prefixed code input (`font-mono uppercase`); `PersianCalendar` for expiry; discount + `CustomSelect` type (`درصد` / `تومان`); maxUses with Users icon.
- Amber tip callout (`bg-amber-50 … border-amber-200`) — “not editable after create.”
- Actions: Back + primary Save; loading spinner on submit; toast + redirect to list.

### Badges

| Badge | Classes |
|---|---|
| Status فعال | `bg-emerald-100 dark:bg-emerald-900/20 text-emerald-600 … rounded-full` |
| Status منقضی | `bg-red-100 dark:bg-red-900/20 text-red-600 … rounded-full` |

### Responsive

- Stats always 3-col; coupon cards 1→2 at `md`.
- Search stacks above filter below `sm`.
- Form fields 1→2 at `md`; padding `p-4 md:p-6`.
- Lives inside Vendor layout: desktop sidebar `hidden lg:block`; mobile drawer `lg:hidden` overlay.

### Closest non-Vendor (do not confuse)

| Surface | Path | Note |
|---|---|---|
| Public coupons merchandising | `src/app/coupons/page.jsx` → `src/components/coupons/CouponsClient.jsx` | Storefront browse, not seller CRUD |
| Cart apply coupon | `src/components/cart/CartCoupon.jsx` | Checkout UX |
| Offers / sale listings | `src/app/offers/*`, `src/app/sale/*` | Product discount merchandising |

### Tooba port status

| Path | Status |
|---|---|
| `src/frontend/app/vendor-panel/coupons/page.tsx` | **Stub shell only** (`VendorCapabilityShell` — “این بخش فعلاً در دسترس نیست”) |
| `/vendor-panel/coupons/new` or edit | **MISSING** |
| Nav | Deferred deep-link only (`VENDOR_DEFERRED_NAV_HREFS` in `vendor-shell.tsx`); hidden from primary nav |

---

## 2. Vendor — Reviews

### Verdict

**PRESENT** as Vendor moderation list. No separate detail route, reply modal, or create form.

### Routes

| Route name | App file | Renders |
|---|---|---|
| `/vendor-panel/reviews` | `src/app/(vendor)/vendor-panel/reviews/page.jsx` | `ReviewsList` |

Nav: `layout.jsx` → `{ id: 'reviews', label: 'نظرات', icon: Star, href: '/vendor-panel/reviews' }`.

### Components

| Role | Path |
|---|---|
| List | `src/components/vendor/panel/reviews/reviewsList.jsx` |
| Barrel | `src/components/vendor/panel/reviews/index.js` |

Related (not Vendor panel management):

| Surface | Path |
|---|---|
| PDP reviews | `src/components/singleProduct/productReviews/*` |
| Seller public profile reviews | `src/components/seller-profile/SellerProfileReviews.jsx` |
| Vendor register “review status” | `src/components/vendor/register/reviewStatus/ReviewStatus.jsx` — onboarding, not product reviews |

### List pattern (`reviewsList.jsx`)

- Header: Star chip + **مدیریت نظرات** + emerald pill “N تایید شده”.
- Stats: `grid grid-cols-3` — تایید شده / در انتظار / رد شده.
- Search (Fuse on customer|product|comment|status) + status filter dropdown.
- Vertical list `space-y-3` (not card grid): product thumb 64×64, customer name, status pill, date, product line, amber stars, comment, action row.
- Actions when `در انتظار`: Approve (`bg-emerald-500`) / Reject (`bg-red-500`) + Delete gray; otherwise Delete only.
- Delete: **`confirm()`** — no modal.
- Pagination: same ReactPaginate pattern as coupons (4/page).

### Badges / borders

| Status | Badge + card border |
|---|---|
| تایید شده | emerald pill + `border-emerald-200` |
| در انتظار | amber pill + `border-amber-200` |
| رد شده | red pill + `border-red-200` |

Stars: `fill-amber-400 text-amber-400` vs `text-gray-300`.

### Responsive

- Header `flex-wrap`; card body `flex items-start gap-4` (thumb + content).
- Search/filter `flex-col sm:flex-row`.
- Same Vendor shell responsive breakpoints (`lg` sidebar / mobile drawer).

### Modals

**MISSING** for approve/reject/delete (inline buttons + `confirm`/`toast` only). Closest Vendor modal pattern to derive from if needed: `src/components/vendor/panel/wallet/addCardModal.jsx` or `orders/returnDetailModal.jsx`.

### Tooba port status

| Path | Status |
|---|---|
| `src/frontend/app/vendor-panel/reviews/page.tsx` | **Stub shell only** (`VendorCapabilityShell`) |
| Nav | Deferred deep-link; hidden from primary nav |
| Storefront PDP reviews | Separate live surface (`storefront-pdp-reviews.tsx`) — not this Vendor list |

---

## 3. Vendor — Notifications

### Verdict

**MISSING** as a Vendor inbox / list route. No `/vendor-panel/notifications` under `src/app/(vendor)/vendor-panel/`. Sidebar has no Bell/notifications item.

### Closest Vendor patterns to derive from

| Need | Closest Shopeiva source | Why |
|---|---|---|
| Notification **preference toggles** (not inbox) | `src/components/vendor/panel/settings/settings.jsx` — tab `notifications` | Toggle rows for order/product/marketing; peer checkbox switch `peer-checked:bg-[#E53935]` |
| Settings shell / tabs | same file + route `/vendor-panel/settings` | Horizontal scroll tabs, gradient header |
| Full **inbox list** UX | Customer Account: `src/components/dashboard/notifications/notifications.jsx` | Typed list, unread pulse, mark-read/delete, filter chips — best visual source if Vendor inbox is added |
| Alternate Vendor list chrome | `src/components/vendor/panel/tickets/ticketsList.jsx` | Same panel list/search/filter/paginate vocabulary |

### Tooba port status

| Path | Status |
|---|---|
| Vendor notifications route | **MISSING** (no `vendor-panel/notifications`) |
| Vendor settings notifications prefs | Not ported as Shopeiva toggle UI; seller settings is a different live/partial operational page |

---

## 4. Customer Account — Notifications

### Verdict

**PRESENT** under User Panel (Shopeiva Account).

### Routes

| Route name | App file | Renders |
|---|---|---|
| `/user-panel/notifications` | `src/app/user-panel/notifications/page.jsx` | `NotificationsComponent` |

Nav entries:

- `src/app/user-panel/layout.jsx` → `{ id: 'notifications', label: 'اطلاعیه‌ها', icon: Bell, href: '/user-panel/notifications' }`
- Duplicate menu data: `src/components/dashboard/sidebar/sidebar.jsx` (same href)

### Components

| Role | Path |
|---|---|
| Inbox UI | `src/components/dashboard/notifications/notifications.jsx` |

### List / filter pattern

- Header: Bell chip + **اطلاعیه‌ها** + “همه خوانده شد” CTA when unread > 0.
- Stats: `grid grid-cols-4` — کل / خوانده نشده (`text-[#E53935]`) / سفارشات / تخفیف‌ها.
- Filter toggle + chip panel: `all | unread | read | order | offer | ticket`.
- Items: vertical cards; unread uses tinted `bgColor` + `border-[#E53935]/30` + pulse dot `w-2 h-2 rounded-full bg-[#E53935] animate-pulse`.
- Per-type icon colors (emerald/blue/amber/rose/red/purple) with matching soft backgrounds.
- Actions: mark-read link + delete; **no modal**.

### Badges

- Unread: red pulse dot (not a text pill).
- Active filter chip: filled `#E53935`.

### Responsive

- Header `flex-wrap`; stats always 4-col (tight on mobile).
- Filter chips `flex-wrap`.
- User-panel shell mirrors Vendor: sticky header 65px, `lg` sidebar, mobile drawer overlay.

### Tooba port status

| Path | Status |
|---|---|
| `src/frontend/app/customer-panel/notifications/page.tsx` | **Stub shell only** (`CustomerCapabilityShell` — “اعلان‌ها”) |
| Nav | Deferred deep-link (`CUSTOMER_DEFERRED_NAV_HREFS`); hidden from primary nav |
| Settings prefs | `customer-panel/settings/page.tsx` marks notification preferences **honestly unavailable** (no fake save) — not a port of the Shopeiva inbox |

---

## 5. Shared panel chrome (apply when porting)

| Concern | Vendor | Customer Account |
|---|---|---|
| Layout | `src/app/(vendor)/vendor-panel/layout.jsx` | `src/app/user-panel/layout.jsx` |
| Active nav | `bg-[#E53935] text-white shadow-md shadow-[#E53935]/20` | same |
| Main padding | `p-4 md:p-6 lg:p-8` | same |
| Mobile drawer | `fixed inset-0 z-50` + `w-[280px]` from right | same |
| Page loading | `DashboardSkeleton` (~300ms artificial delay in page wrappers) | same |

Tooba already mirrors this shell structure in `vendor-shell.tsx` / `customer-panel-shell.tsx` with accent **blue `#2563EB`** (documented minor deviation from Shopeiva red).

---

## 6. MISSING / gap summary

| Surface | Shopeiva | Closest derive-from | Tooba today |
|---|---|---|---|
| Vendor coupons list + create | PRESENT | — | Stub page only |
| Vendor coupon edit | **MISSING** (list links to `/coupons/:id/edit`; `[id]` mis-wired to CustomerDetail) | `couponForm.jsx` | MISSING |
| Vendor promotions (separate) | **MISSING** | Coupons **تخفیف‌ها** | N/A |
| Vendor reviews list | PRESENT | — | Stub page only |
| Vendor review reply/detail modal | **MISSING** | Wallet/orders modals | MISSING |
| Vendor notifications inbox | **MISSING** | Customer `notifications.jsx` (+ Vendor settings toggles for prefs) | MISSING |
| Customer notifications inbox | PRESENT (`/user-panel/notifications`) | — | Stub `/customer-panel/notifications` |

---

## 7. Port priority file list (exact absolute paths)

### Must-read Shopeiva sources

1. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\components\vendor\panel\coupons\couponsList.jsx`
2. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\components\vendor\panel\coupons\couponForm.jsx`
3. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\app\(vendor)\vendor-panel\coupons\page.jsx`
4. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\app\(vendor)\vendor-panel\coupons\new\page.jsx`
5. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\components\vendor\panel\reviews\reviewsList.jsx`
6. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\app\(vendor)\vendor-panel\reviews\page.jsx`
7. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\components\dashboard\notifications\notifications.jsx`
8. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\app\user-panel\notifications\page.jsx`
9. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\components\vendor\panel\settings\settings.jsx` (Vendor notification **prefs** only)
10. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\app\(vendor)\vendor-panel\layout.jsx`
11. `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\src\app\user-panel\layout.jsx`

### Existing Tooba stubs (not full ports)

1. `D:\Users\User\source\repos\SarvNewVer\src\frontend\app\vendor-panel\coupons\page.tsx`
2. `D:\Users\User\source\repos\SarvNewVer\src\frontend\app\vendor-panel\reviews\page.tsx`
3. `D:\Users\User\source\repos\SarvNewVer\src\frontend\app\customer-panel\notifications\page.tsx`
4. `D:\Users\User\source\repos\SarvNewVer\src\frontend\app\vendor-panel\vendor-capability-shell.tsx`
5. `D:\Users\User\source\repos\SarvNewVer\src\frontend\app\customer-panel\customer-capability-shell.tsx`
