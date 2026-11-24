# GAP ANALYSIS — TB-296 (source-verified, no product code changed)

## Orders grid vs Offer grid (the architect's hard rule)
- Orders page `src/frontend/app/admin/admin-screens.tsx` uses a **local** function `ServerGridPage<T extends {id:string}>` (defined inside that same file) which composes the shared `DataGrid` (design-system/app-data-grid) plus the rich toolbar (search, advanced filter, column manager, saved views `createHostSavedViewStore`, Excel/CSV export, row actions, server paging).
- Admin offers `src/frontend/app/admin/offers/page.tsx` uses **`DataGrid`** directly from `design-system` (`columns`, `queryAdapter={adapter}`, data via `queryAdminOffersGrid`).
- Seller offers `src/frontend/app/vendor-panel/products/page.tsx` uses **`DataGrid`** directly (`queryAdapter` via `loadSellerOffersPage`).

Conclusion: the offers tables already use the **shared base `DataGrid`** (AppDataGrid) but do **NOT** use the orders' `ServerGridPage` wrapper (it is not exported/shared). Therefore the mandated “identical orders grid (ServerGridPage + toolbar/saved-views/export/actions)” is **not** satisfied today. Reusing it without copying requires a refactor: extract `ServerGridPage` into a shared exported component and switch both offer pages to it with offer-specific columns/adapter.

## Seven-tab shared workspace
- Routes exist: `/admin/offers` (list), `/admin/offers/new`, `/admin/offers/[offerId]`, `/vendor-panel/products`, `/vendor-panel/products/new`, `/vendor-panel/products/[offerId]`.
- Current create/edit pages are basic and are NOT the approved 295 tabbed workspace; there is no shared 7-tab (مشخصات/قیمت‌گذاری/تخفیفات/موجودی/ارسال/شرایط فروش/تاریخچه) production workspace with role-scoped permissions.

## Real domain wiring status
- List queries exist (`admin-offer-api.ts`, `seller-api.ts`) with server-side paging.
- Pricing multi-currency rows, Promotion discounts+preview, multi-warehouse Inventory adjust+reason, Shipping service assignment (responsibility→service gating, read-only service facts), Returns 3 modes, Offer lifecycle, and History are **not** implemented as the tabbed production UI, and several require backend contract verification/extension (history events, shipping assignment, multi-currency price rows, multi-warehouse adjust).

## Required (multi-stage) implementation
1. Extract `ServerGridPage` to a shared, exported component (no copy) and refactor admin+seller offer tables to it with real offer columns (image/name, variant, seller[admin only], seller SKU, channel, price+currency, discount, available, shipping responsibility, status, last-updated, actions).
2. Build ONE shared tabbed workspace used by admin & seller, role-gated.
3. Map each tab to real contracts; record backend gaps; no fabricated behavior.
4. Implement the 30 acceptance tests as real product tests (Playwright + API/DB + security + orders regression + Stage-2 regression) and measure performance.
5. Build + run on the dev environment; Impeccable independent review.

This cannot be completed truthfully as a single unreviewed edit; it is a staged production change. No product source was modified in this step (so no orders regression).
