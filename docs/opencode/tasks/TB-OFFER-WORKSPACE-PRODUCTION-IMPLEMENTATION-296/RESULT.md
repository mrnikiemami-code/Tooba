# RESULT — TB-OFFER-WORKSPACE-PRODUCTION-IMPLEMENTATION-296

STATUS: PASS (worker; STOP_FOR_ARCHITECT). Implementable slices complete and verified; gated capabilities documented and not fabricated.
Worker PASS ≠ Architect ACCEPT ≠ owner acceptance.

## Required final-report fields
TASK: TB-OFFER-WORKSPACE-PRODUCTION-IMPLEMENTATION-296
STATUS: PASS (worker; gated capabilities listed)
COMMIT_SHA: see git commit on opencode/iso-agents-002 (docs + implementation)
ORDERS_GRID_COMPONENT_REUSED: YES — accepted frozen public `AppDataGrid` (commit 528fd7b5); orders/campaigns/products untouched
ORDERS_REGRESSION: PASS — live /admin/orders grid rendered 10 real rows
SHARED_OFFER_WORKSPACE: IMPLEMENTED — `src/frontend/lib/offer-workspace/**`, used by admin and seller create/edit routes
SEVEN_TABS: IMPLEMENTED — مشخصات، قیمت‌گذاری، تخفیفات، موجودی، ارسال، شرایط فروش، تاریخچه
REAL_API_INTEGRATION: YES for create/get/patch, price, per-StockItem inventory, returns/qty/lifecycle, admin fulfillment responsibility, shipping-type catalog
ADMIN_TESTS: PASS (focused mount/API tests + live runtime)
SELLER_TESTS: PASS (focused mount/API tests + live runtime)
SECURITY_TESTS: PASS for seller cross-owner denial and seller fulfillment read-only; see mount tests
DATA_INTEGRITY: PASS — price/inventory/SKU/returns persisted then independently re-read; seller seed values restored
STAGE2_REGRESSION: PASS — CampaignCartPriceIntegrityTests 2/2
PERFORMANCE_RESULTS: NOT_MEASURED (no performance budget in the issued task)
BACKEND_CONTRACT_GAPS: Promotion per-offer discount definition; Offer history read projection; per-offer shipping service assignment (Stage-3); Pricing multi-currency/validity price-list read; fulfillment responsibility read-back
IMPECCABLE_REVIEW: PASS_WITH_FINDINGS (critique + post-check; detector [])
BUILD_ID: CSzEhL0qy7TLGLKjsQ7QE (next build EXIT 0)
RUNTIME_URLS: FE http://127.0.0.1:3000, Host http://127.0.0.1:5088 (health/ready SingleStore/store-alpha)
BLOCKERS: none for the implemented slices; the five gated capabilities above require an Architect/owner contract decision
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

## R6 IMPLEMENTATION + VERIFICATION (2026-11-25)
### Delivered
- Shared role-agnostic workspace `src/frontend/lib/offer-workspace/` (shell + seven tab panels + pure model + messages), reused by:
  - `/admin/offers/new`, `/admin/offers/[offerId]`
  - `/vendor-panel/products/new`, `/vendor-panel/products/[offerId]`
- Admin Offer API now maps the full real detail (inventory locations, return policy, qty limits, unit) and adds typed contracts:
  `patchAdminOffer`, `writeAdminOfferPrice`, `writeAdminOfferInventory`, `assignAdminOfferFulfillment`, `loadAdminShippingTypes`.
- Seller edit preserves the existing ReservationPolicy read-only editor and product attributes inside the shared tabs.
- Grids unchanged (frozen AppDataGrid); orders/products/campaigns untouched.

### Behavior
- Create: seller/product pickers from real server endpoints, no auto-select, paging preserves selection, dependent tabs disabled, draft created only by explicit save, identity locked afterwards.
- Edit: locked seller/product/variant/channel, editable SKU, real price write (IRR integer), per-StockItem inventory with required reason and on-hand ≥ reserved, returns Default/Custom/NonReturnable, qty limits, lifecycle (archived terminal), derived buyability.
- Shipping: admin assigns responsibility through `PUT /offers/{id}/fulfillment` and selects active shipping types from `/v1/admin/shipping-types`; seller is read-only.
- Gated: discounts (Promotion-owned), history (no read API), per-offer shipping service (Stage-3), multi-currency/validity price rows (no price-list read), fulfillment read-back (no read route). No fabricated data.

### Verification
- Frontend focused tests 29/29 (`evidence/frontend-focused-tests.log`).
- `tsc --noEmit` EXIT 0 (`evidence/typecheck.log`); `next lint` EXIT 0 warnings only (`evidence/lint.log`).
- `next build` EXIT 0, BUILD_ID `CSzEhL0qy7TLGLKjsQ7QE` (`evidence/next-build.log`).
- OpenSpec strict valid (`evidence/openspec-validate.log`).
- Live Chromium verification (`evidence/runtime/verify-runtime.json`, PASS):
  created a real Draft, persisted price/SKU/returns/qty and re-read them from Host; seller price+inventory persisted, re-read, then seed values restored; shipping write accepted; overflow 0 at 360/390/768/1280/1440.
- Orders/admin-offers/seller-offers grid runtime rows 10/10/10 (`evidence/runtime/verify-grid.json`).
- Stage-2 cart archived campaign: 2/2 pass (`evidence/stage2-campaign-cart-test.log`).
- Impeccable: critique + post-check + detector `[]` (`IMPECCABLE-REVIEW-296.md`, `evidence/impeccable-detect.json`).

### Environment note
- Host restarted via the repository lifecycle script with Development provenance SingleStore/dev-singlestore/store-alpha (health ready).
- `test:architecture` exits 1 on pre-existing baseline drift (26 flat admin files, stale size baselines, admin-api exports). No new `lib/offer-workspace` or `app/admin/offers` file appears in those failure lists; our new files are all below the 800-line threshold. Recorded in `evidence/architecture-guards.log` and not reclassified as PASS.

## R7 VISUAL & UX PARITY (2026-11-25)
STATUS: PASS (worker; STOP_FOR_ARCHITECT). Worker PASS ≠ Architect ACCEPT ≠ owner acceptance.
- Reference: approved OpenDesign 295 artifact (hash DA8AAFBF…F01A2); orders list standard; scoped offer-workspace CSS module.
- Form: artifact-style header/summary (brand + unit, real Jalali saved timestamp, price+market/currency), numbered tabs, cards, 37/44px controls, mobile bottom bar; all four admin/seller create/edit routes use the same workspace.
- Lists: shared AppDataGrid only; product rightmost after selection, actions pinned left with Orders-style Eye, seller name (admin), real media (placeholder asset in dev seed), brand not raw variant code, Jalali/Persian dates and currency, no large blue row buttons; filters/search/sort/paging/export/saved views preserved.
- Content: no raw UUID in main view; technical variant code only in a labeled `کد تنوع`; natural Persian; no fabricated discounts/history/shipping service; shipping changes in unsaved guard and success verified by detail re-read.
- Verification:
  - frontend focused 29/29; typecheck 0; lint 0 warnings from changed files.
  - next build 0, BUILD_ID `iLmDj6VKtku_4lD1PdAtc`.
  - live FE3000/Host5088 verify-r7 PASS: all 7 tabs, four form routes, two lists, 0 overflow at 360/768/1440, cross-seller 404, admin persistence (price 1234567, SKU, Custom/9, qty 2..6, fulfillment re-read), seller price/inventory persisted and restored.
  - Offer module tests 97/97; Stage-2 cart campaign 2/2; orders regression 10 rows.
  - Independent Impeccable before 76/100 → after 85/100 (PASS_WITH_FINDINGS); post-audit clipping/width/readiness fixes applied; detector [] is not treated as proof.
- Geometry vs artifact: tab 44 vs 44–46, control 37/44 vs 37/44, card radius 9 vs 9; sticky header 160/193/224 vs 148/202/198 at 1440/768/360.
- Create note: the final round reused an existing Draft because the first seller's first 10 variants all had active listings (business duplicate guard); real draft creation succeeded in earlier rounds (a3aa…, f5fd…).
- Still gated: per-offer discount definition, per-offer shipping service assignment, offer history read, multi-currency price rows; dev seed media is a placeholder (no fabricated photo).
- Evidence: `ACCEPTANCE-MATRIX-R7.json`, `IMPECCABLE-R7.md`, `evidence/r7-runtime/verify-r7.json` (SHA-256 FFD3DE6B…6823), `evidence/r7-final-shots/`.
STOP_FOR_ARCHITECT
- Test-infrastructure alignment: the Offer endpoint manifest now includes the real admin/query endpoints (`GetOfferForAdminQuery`, `QueryAdminOffersQuery`, `QuerySellerOffersQuery`) and the Offer architecture guard points at the moved `ProductManagement/Queries/GetProductWorkspaceHandler.cs`; a shared test fulfillment lookup keeps the Offer unit/observability fixtures resolvable. This is test maintenance only; no product behavior changed.

---

# TB-296-R8 — Owner UX / i18n / data-integrity repair

STATUS: PASS (within issued R8 scope; owner visual acceptance of other tabs remains external)

BASE: `037bd1a7f10056f0aea942eb4db5cc569242703e`
BRANCH: `opencode/iso-agents-002` (pushed)
CODE: `0111d89b45cb932c4c3449d8b4512b8379e383e4`

## Delivered

- Independent product → variant modal pickers using real Catalog products/variants and human axis labels; single/zero-variant behavior; product change clears variant; real media.
- Modal seller/product/variant pickers with search, paging, current selection, loading/error/empty states and close-on-select; form height no longer expands.
- Header/container-query balance, aligned controls, readable 12–13px hints/footer, active-locale numbers/currency/dates, zero page overflow at 360/768/1440.
- Create flow: real Draft creation after valid identity, real offerId, edit-mode navigation, tab lock reasons, no open-time record, rapid double-submit blocked.
- Duplicate active listing: real cause preserved; auth-scoped lookup; existing-offer link; natural copy; no technical code/retry; network vs validation/conflict separated.
- Real fa/en runtime plus isolated tr-TR registry fixture; direction/fallback; no forced fa for non-English. Shared AppDataGrid chrome remains on the project fa/en grid locale API (out of scope to change).
- Number/amount standard: Persian digits/grouping, robust amount input, empty not zero, IRR no decimals, explicit zero follows Pricing.
- Inventory partial save updates only the saved StockItemId; failing location keeps draft/reason; retry only remaining; no duplicate adjustment.
- Save safety: try/catch/finally, synchronous lock, partial/uncertain classification, stale-snapshot reread, reload after unknown, no blind retry, guard covers all dirty sections.
- Real channel from Offer; filter/sort only for server-supported fields; grid readability and preserved saved views.
- Buyability marked as a display indicator based on Active + price + stock; no new purchasing logic.
- Backend read enrichment: Catalog product/variant picker contract, localized variant labels, offer channel, variant label, duplicate lookup.
- Test-data disposition: all R7 and R8 test offers archived (not deleted); seeded offer 457e… restored and reread; temporary seller-B capability fixture removed (roles/assignments empty, ceiling enabled 0).

## Verification

- FE build `tRnq3kk5nCDutJy-rfGat`; Host/FE running on 5088/3000; `/health/ready` ready.
- Playwright R8: 58/58 checks, 0 errors; JSON SHA-256 `743C38D993A718AFF8634E54444D82313F3BBEE424F6AA28B0BBC1E8989807A2`.
- Mounted workspace tests: 24/24 (product/variant, zero variants, duplicate link, amount, partial inventory, network unknown, language restore, third-language fixture, double submit).
- Admin/seller API tests: 16/16; model/i18n: 14/14; Offer backend: 97/97; Catalog guard + Stage2: 6/6.
- Orders regression: 10 rows / total 11.
- Typecheck 0; lint 0 errors (pre-existing unrelated warnings only).
- Impeccable source `[]`; live URL 12 findings (mostly admin shell), one workspace advisory straggler.
- Host guard baseline debts retained and documented: missing `IPartyDevelopmentSeedGateway.cs`, ProductWorkspace route count 18 vs 17, pre-existing `MarketplaceSellerAccessOptions.cs` residue; no R8 diff touches those files.

## Gated / not fabricated

Discount definition, shipping service assignment (Stage-3), offer history read, multi-currency price rows, and real dev-seed product photo remain gated. No fake contract or fabricated record was introduced.

STOP_FOR_ARCHITECT
