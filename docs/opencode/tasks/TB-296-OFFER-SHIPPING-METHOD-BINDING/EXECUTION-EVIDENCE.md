# TB-296 Offer Shipping Method Binding — Execution Evidence (partial)

Status: **IMPLEMENTED + PARTIALLY VERIFIED; 26-ID matrix NOT fully executed.**
Base HEAD: `8e0e850b182fd6191031603709a5f85630c5cbba`.
Environment: existing Host 5088, Next 3000, PostgreSQL `tooba_alpha` (postgres:17 in `postgres-db`).

## Backup / identity
- Pre-change backup: `C:\Users\User\AppData\Local\Temp\opencode\tb296\tooba_alpha_pre.dump`
  SHA256 `EB31EC270B3AE15B5FB0FB523E358B4DFB67A2CC90BC57878DE996AB87561262` (matches in-container dump).
- Migration applied to `offer.__ef_migrations_history`: `20261128120000_AddOfferShippingMethods`.
  New column `offer.offers.shipping_selection_mode`; new table `offer.offer_shipping_methods` (offer_id, method_id).
- Candidate Host: `.tmp-shipping-host/Tooba.Host.exe`, `Tooba.Host.dll` SHA256
  `95BEA3C90ACC09194A86BF5F044E862061F57388CD152367A601FFA93EEA1AF2`, listening on 5088.
- Candidate Next: `next start -p 3000`, BUILD_ID `M5FJG_m-m7I8VZovBmzTt`.

## Implemented (source)
Backend:
- Offer domain: `ShippingSelectionMode`, `OfferShippingMethod` link entity, `SellerOffer.ShippingSelectionMode`.
- Own-schema migration/config/DbSet; `OfferStore` link read/replace; snapshot + admin detail expose mode/ids.
- Presence-aware `AssignOfferFulfillmentCommand`/endpoint: explicit mode+method set, one SaveChanges, revision guard,
  duplicate/empty/unavailable rejection, reconciliation guard for legacy owner/type change on a Specific offer,
  deterministic history old/new, truthful no-op on identical intent.
- Shipping public read seam `IOfferShippingMethodLookup` (Contracts) + implementation: selected published captures
  only, same real shipping type, immutable draft id as stable method id, active-language label from DB translations.
- Admin queries/endpoints: `GET /v1/admin/offers/{offerId}/shipping-methods`, `GET /v1/admin/offers/shipping-methods`
  (create surface), both behind `fulfillment.manage`, no writes.
- Order eligibility: captured-preview group intersection (never union) and legacy served-path allowlist filter
  (fail-closed; Automatic unaffected; no owned-rate/engine redesign).
Frontend:
- Approved shipping tab presentation extracted to `shipping-tab.tsx` + local module CSS (segmented mode, chips,
  SearchableSelect search, empty/loading/error/stale states), wired through typed adapter and shared Save.
- Draft recovery persists mode/selected ids; create-and-apply shares the same typed orchestration and read-back.
- Files kept under the 800-line AQ-07 guard (`offer-workspace.tsx` 799; `messages.ts` 794).

## Connected checks actually executed (PASS)
- `GET /v1/admin/offers/55c91cc9.../shipping-methods?language=fa` → 200, real published methods for Platform/standard:
  `post` (`پیشتاز`) and `tipax:express` (`TB264R2 تیپاکس اکسپرس آزمایشی`), with stable draft ids.
- `PUT /v1/admin/offers/ba979a98.../fulfillment` Specific `[01a16eeb...]` → 200; independent GET → mode Specific,
  same id, new revision; SQL `offer_shipping_methods` one row; history old/new recorded.
- Duplicate ids → 400 `offer.shipping.method_duplicate` (localized FA title).
- Unknown/foreign method id → 400 `offer.shipping.method_unavailable` (localized FA title), no write.
- Legacy owner/type change on a Specific offer → 409 `offer.shipping.reconciliation_required`, no write.
- Restored that offer to Automatic and then restored the exact pre-test row state
  (`shipping_selection_mode` NULL, original `updated_at`, 0 link rows, 0 new history rows).
- `dotnet build` Host / Offer / Shipping / Order: 0 errors. `npm run typecheck`: 0 errors. `npm run build`: success.
- `dotnet test Tooba.Offer.Tests`: 144/144 passed.

## Not executed / environment-gated (NOT_RUN)
- SHIP-01..26 full immutable matrix is **not** fully executed. Executed subsets above are not a full PASS.
- Shipping/Order relational suites fail only because the required disposable instances
  `127.0.0.1:55432/shipping_tb190r1`, `127.0.0.1:15432/shipping_tb214`, `127.0.0.1:15433/order_tb231`
  are not provisioned; new DB/Testcontainers are not authorized by the DEV policy.
- Two Order unit guards (`ShippingGroupAcceptedEvidenceTests.Partial...`, `StorefrontShippingPackagingChargeBoundaryTests...`)
  fail outside the changed Offer/Shipping paths; not proven to be caused by this change.
- Real Playwright geometry/hit-testing (SHIP-21/22), full storefront cart→checkout eligibility/group intersection
  (SHIP-15..20), CAS races (SHIP-12..14), and AQ-01..08 independent evidence are **NOT_RUN**.
- No Impeccable audit, no screenshots, no Reviewer.

## Remaining work for a full PASS
Execute the issued 26-ID matrix through real browser/HTTP/PostgreSQL with fixtures and negative controls, complete
AQ-01..08 source evidence, and run the frontend architecture suite against the pre-existing baseline (our two new
oversize violations were removed; remaining 27 are pre-existing/unrelated files).
