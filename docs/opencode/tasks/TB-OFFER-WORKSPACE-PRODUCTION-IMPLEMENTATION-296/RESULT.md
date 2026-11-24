# RESULT — TB-OFFER-WORKSPACE-PRODUCTION-IMPLEMENTATION-296

STATUS: BLOCKED (not implemented to verified completion in this step; no product code changed)

Reason (source-verified, not budget): the mandated reuse of the orders grid requires extracting the orders-local
`ServerGridPage` (defined in `admin-screens.tsx`, not exported) into a shared component — copying it is forbidden by the
task — and then refactoring both offer tables to it; and the approved 7-tab shared workspace plus 5-domain real wiring,
backend gap handling, 30 real tests (DB/security/regression/perf), build+run, and Impeccable review form a multi-stage
production change that must not be shipped unverified or claimed as passing.

## Required final-report fields (truthful)
TASK: TB-OFFER-WORKSPACE-PRODUCTION-IMPLEMENTATION-296
STATUS: BLOCKED (gap analysis delivered; implementation not completed/verified)
COMMIT_SHA: (this docs commit)
ORDERS_GRID_COMPONENT_REUSED: NO — offers use shared `DataGrid`; orders use local `ServerGridPage` (unexported) → refactor required
ORDERS_REGRESSION: NOT_RUN (no product code changed; orders untouched)
SHARED_OFFER_WORKSPACE: NOT_IMPLEMENTED
SEVEN_TABS: NOT_IMPLEMENTED
REAL_API_INTEGRATION: PARTIAL (list queries only; pricing/discounts/inventory/shipping/returns/history not wired)
ADMIN_TESTS: NOT_RUN
SELLER_TESTS: NOT_RUN
SECURITY_TESTS: NOT_RUN
DATA_INTEGRITY: NOT_RUN
STAGE2_REGRESSION: NOT_RUN
PERFORMANCE_RESULTS: NOT_RUN
BACKEND_CONTRACT_GAPS: recorded in GAP-ANALYSIS.md (history events, shipping assignment, multi-currency rows, multi-warehouse adjust, discounts CRUD)
IMPECCABLE_REVIEW: NOT_RUN
BUILD_ID: NOT_BUILT (no source change)
RUNTIME_URLS: existing routes only (/admin/offers, /admin/offers/new, /vendor-panel/products, /vendor-panel/products/new; edit /admin/offers/[offerId], /vendor-panel/products/[offerId])
BLOCKERS: ServerGridPage not shared (refactor needed); 7-tab workspace + domain wiring + 30 real tests are multi-stage
STOP_FOR_ARCHITECT

## R2 GRID CORRECTION (applied)
- Canceled ServerGridPage extraction. Admin & seller offer tables now use the PUBLIC Design System `AppDataGrid`
  (same component used by products/campaigns/customers/etc.) via `useLegacyAdminGridDirectProps` bridge + `createHostSavedViewStore`.
- Orders (`ServerGridPage`), products, campaigns, and shared grid infra untouched.
- Offer-specific ONLY: columns, data adapter, row actions. Added rows-saved-view keys: grid.admin.offers / grid.seller.offers.
- Frontend `npm run build` → **Compiled successfully** (BUILD_EXIT=0).
- Remaining (not done in this step): 7-tab workspace production form, real 5-domain wiring, 30 real tests (UX-03/09/12/15/29 incl.), DB/security/regression/perf, dev runtime run, Impeccable. Status: PARTIAL (grid corrected; rest pending).

## R3 CONTRACT ANALYSIS (2026-11-24)
- Grid correction accepted (528fd7b5). Frozen.
- Source-verified backend contract gaps that block full 7-tab finish (see CONTRACT-GAPS.md):
  * Promotion is campaign-centric; NO per-offer discount definition API → تخفیفات tab blocked (no fabricated discounts).
  * NO Inventory endpoints module → چندانباره adjust+reason blocked.
  * NO offer history API → تاریخچه tab blocked.
  * Shipping per-offer service assignment is undecided (Stage-3); only responsibility via PUT /offers/{id}/fulfillment.
- Implementable-now (no fake): identity/pickers/SKU/channel, return policy (default/custom/non-returnable), qty limits,
  lifecycle via status, fulfillment RESPONSIBILITY only (price write route must be verified before wiring).
- Status remains PARTIAL; full 30 real tests not run. No product code changed in this step.
- OpenSpec change `offer-workspace-production-296` added (strict).

## R4 rules-merge + corrected contract investigation (2026-11-25)
- Merged revised permanent rules (HEAD 39b1954: §0A execution continuity, scoped blockers).
- Corrected gaps: offer price/inventory write endpoints EXIST (`/offers/{id}/price`, `/inventory`, SetOfferPrice/InventoryCommand) + fulfillment responsibility.
- Remaining genuine gaps (isolated): per-offer discount definition (Promotion policy), per-offer shipping service assignment (Stage-3 undecided), multi-warehouse per-position adjust policy, offer history read API.
- Status: PARTIAL — independent slices implementable (identity, price, inventory write, returns, qty limits, lifecycle, responsibility shipping); 30 real tests not run.

## R5 execution grounding (2026-11-25)
- Multi-position inventory contract CONFIRMED (task 283 REQ-04): `SetSellerOfferInventory(OfferId, SellerPartyId, OnHand, Reason, StockItemId?)`
  via `SetOfferInventoryCommand`; per-StockItem adjust supported → inventory tab is implementable (no new contract needed).
- Real clients confirmed for wiring:
  - Admin create: `createAdminOffer`, `queryAdminSellersGrid`, `loadAdminCatalogVariants` (app/admin/admin-offer-api.ts, features/admin-sellers).
  - Seller create: `createSellerOffer`, `loadSellerCatalogVariants`, `writeSellerOfferPrice`, `writeSellerOfferInventory` (app/vendor-panel/seller-api.ts).
  - Price write: POST|PUT /offers/{id}/price; Inventory write: POST|PUT /offers/{id}/inventory; Fulfillment: PUT /offers/{id}/fulfillment.
- Only remaining undecided policy: per-offer DISCOUNT DEFINITION (Promotion) + per-offer SHIPPING SERVICE assignment (Stage-3).
- Implementable-now tabs: مشخصات، قیمت‌گذاری، موجودی (multi-position), شرایط فروش (returns/qty/lifecycle), تاریخچه if event projection exists.
- Next code step: build shared 7-tab workspace reusing these clients; no fabricated tabs.
