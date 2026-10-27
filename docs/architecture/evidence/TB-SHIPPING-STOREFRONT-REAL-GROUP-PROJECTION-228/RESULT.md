# TB-SHIPPING-STOREFRONT-REAL-GROUP-PROJECTION-228 — RESULT (PARTIAL)

Base: `d292e88b6` (fast-forwarded from `a32d99ee4`; only the three pre-existing backend line-ending drifts
were left unstaged). Status: **EXECUTED — PARTIAL**. Slice A (explicit stored responsibility + type) is
implemented with focused facts; slices B (server plan → 219 → 218 compose) and C (existing Storefront cards)
are **NOT implemented**. No live trial, no shared migration apply, no Host restart.

## What is really implemented (A — explicit assignment authority)

1. **Offer is the only assignment point.** `SellerOffer` now carries
   `FulfillmentResponsibility?` (`Seller | Platform`, `Domain/ValueObjects/FulfillmentResponsibility.cs`) and a
   nullable real `FulfillmentShippingTypeId`. Both are `private set`; the only mutation is
   `AssignFulfillment(responsibility, shippingTypeId, now)` which:
   - leaves a legacy/unassigned listing `null` (no backfill, no inference from seller id/channel/name);
   - refuses an archived offer (`offer.archived.cannot_assign_fulfillment`);
   - refuses a type id without an explicit responsibility (`offer.fulfillment.type_without_responsibility`);
   - refuses `Guid.Empty` as a type (`offer.fulfillment.type_invalid`);
   - clears both parts only on an explicit `null, null` call.
   Nothing in the aggregate ever derives "Seller" from `SellerPartyId` — that identity stays the offer owner,
   not a responsibility statement.
2. **Persistence (additive, nullable, no backfill).** `20261027120000_AddOfferFulfillmentAssignment` adds
   `offer.offers.fulfillment_responsibility` (`varchar(16)`, the enum stored as text) and
   `offer.offers.fulfillment_shipping_type_id` (`uuid`). Only the stable id is stored: no type code/label copy,
   no foreign key, no join into Fulfillment's schema. `SellerOfferConfiguration` and
   `OfferDbContextModelSnapshot` are updated to match. **The migration has NOT been applied to any database**
   (no runner execution, no schema write in this task).
3. **Batch Contracts read.** `Tooba.Offer.Contracts/Ports/IOfferFulfillmentAssignmentLookup` returns
   `IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>` for the distinct real offer ids of a cart in
   ONE query (implemented on the existing scoped `OfferStore`, registered to the same instance). A missing id is
   simply absent — the consumer must refuse an unknown offer instead of defaulting.
4. **Admin-only write entry.** `PUT /v1/admin/offers/{offerId}/fulfillment` →
   `AssignOfferFulfillmentCommand`. Order inside the endpoint is fixed: **authorization before body read**, then a
   presence-aware JSON body reader (`responsibility` absent ⇒ `offer.fulfillment.body_malformed`; explicit `null`
   ⇒ clear), then `ISender`. The authorizer reuses the **existing** admin panel gate and the **existing**
   `fulfillment.manage` permission — no new permission code, no seller route, so a seller gains no right to
   declare Platform responsibility. A requested type is validated against the real active Fulfillment taxonomy
   through its Contracts reader (`IShippingTypeReader.GetByIdsAsync`), and an infrastructure failure stays an
   exception rather than becoming "unknown type".
5. Module hygiene: new error codes + FA/EN resources + catalogue descriptors; `Admin` removed from the Offer
   endpoints manifest forbidden folders with an explicit justification (Storefront still forbidden); the
   endpoint-validator manifest now classifies seven requests.

## What is NOT implemented (honest PARTIAL)

- **B — no server plan builder.** Nothing yet loads the cart's offer ids, partitions the cart by
  (responsible owner + real type), discovers candidate bindings from the real catalog, reads the 219 selected
  pointer, and calls the 218 composer. `StorefrontCapturedGroupProjectionComposer`, the 219 reader and the 220
  publication API remain **unconsumed**.
- **C — no Storefront wiring.** `storefront-shipping.tsx` / group cards / delivery card are untouched; no
  additive transport, no per-group dates from the stored method week, no read opt-in flag.
- **Selection/commit** therefore still has no captured-plan path to refuse: `SaveSelectionAsync`/`CommitAsync`
  only accept the legacy draft group keys, and no captured candidate is reachable from any transport. The
  intended explicit refusal is consequently **not needed yet and not added** — it must land together with C so a
  quoted-but-uncommittable candidate can never be disguised as checkout-ready.
- No live/mounted proof, no real GET/POST receipts, no 1440/390 storefront screenshots, no trial cart or offer
  assignment written, no migration applied, no Host restart, no user draft/pointer touched.

## Proof actually executed

- `dotnet build Tooba.Offer.Endpoints` — 0 errors (one duplicate `using` warning removed).
- `dotnet build Host/Tooba.Host` — **0 errors** (48 pre-existing warnings from other modules).
- New focused facts `Tooba.Offer.Tests/Domain/OfferFulfillmentAssignmentTests.cs` — **8/8 pass**
  (unassigned-by-default, both families, Platform with type id, clearing, type-without-responsibility refusal,
  empty id refusal, archived refusal).
- `Tooba.Offer.Tests` full project — **92/93 pass**. The single failure is
  `OfferArchitectureGuardTests.Extracted_host_surfaces_use_offer_query_gateway_not_persistence`, which reads
  `Modules/ProductWorkspace/.../GetProductWorkspaceHandler.cs`; that path does not exist in git at all
  (`git ls-files` has no such file, last touched by `592c346e9`), so the guard fails on a **pre-existing
  missing-file drift unrelated to this task** — recorded, not repaired, not hidden.
- Not run: 119/188/194/full-frontend/backend/solution suites, migration application, browser.

## Elapsed / limits

Actual ≈82 minutes vs the HARD40 box (source stop 25) — **recorded as NONCOMPLIANT, not a process PASS**.
The assignment authority itself (A) consumed the box; B and C were not started rather than half-invented.

## Next exact step (for the Architect)

1. Apply `20261027120000_AddOfferFulfillmentAssignment` through the existing MigrationRunner plan/apply on
   `store-alpha` (explicit target, no reset/reseed), after inspecting the real pending set.
2. Build the server plan from the live cart: distinct real offer ids → one batch assignment read → partition by
   (responsibility + real type + seller boundary) → discovery of real catalog bindings including a service with
   `NULL` option → one 219 read per distinct owner+binding → ONE 218 `ComposeAsync`.
3. Feed each group's own result into the existing storefront group cards behind a narrowly scoped, default-OFF
   read opt-in, and add the explicit captured-plan refusal in `SaveSelectionAsync`/`CommitAsync` in the same
   change.

STOP_FOR_ARCHITECT. Automatic-Next-Task NONE. No task 229 envelope. TB185 paused, default-OFF,
PendingAllocation preserved; no money/rate/week/publication/geography change.
