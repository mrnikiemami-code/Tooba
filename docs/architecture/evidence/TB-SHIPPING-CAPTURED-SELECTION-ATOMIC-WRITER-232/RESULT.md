# TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232 — RESULT

Status: **EXECUTED**, not accepted. No user Visual ACCEPT. No checkout, reservation, payment, cart
conversion, historical read, UI or runtime change. `not_ready` for final write stays.

Base read/verified at task start: `54b01303bde6b8732e75e8f76143305584926060` (branch `main`, clean parity
with `origin/main` before edits). This worker changed no shared/historical migration and rewrote no history.

## What the task asked (see TASK.md, verbatim artifact)

One real writer slice: the EXISTING `SaveSelectionAsync`/HTTP selection path must save
server-revalidated captured group choices plus 231 evidence, under a NEW explicit
`Tooba:StorefrontCapturedPreview:SelectionEnabled` option (default false, and it additionally requires
`Enabled` + a valid date policy). Read/selection must consume the SAME typed resolution. Intent from the
browser is untrusted keys + echo tokens only. Atomic replacement with a real concurrency token. Final
order/payment stay closed.

## A — ONE shared resolution, read and selection

`StorefrontCapturedPreviewPlanner.ResolveAsync` (added for this task) is now the ONLY resolution body:
current cart/access/version → one 228 assignment batch → real active types/bindings → one 219 pointer read
per distinct full owner+binding → ONE 218 `ComposeAsync` (which performs the one 217 whole-cart goods read
and one 216 quote per candidate) → typed `StorefrontCapturedSelectionContext`. `BuildAsync` (the read
projection) and `StorefrontCapturedSelectionWriter` both consume that SAME result; there is no second
planner, no parse-back of display money/labels and no re-pricing between the read the customer saw and the
write that is stored.

Coupon intent: the selection request now carries the optional `CouponCode` intent and it is forwarded into
the same 217 Pricing/Promotion/Tax command (trimmed; blank ⇒ absent — never a fabricated discount). The
captured READ still passes no coupon because its request model has none; that read/write difference is
limited to discounting and is disclosed below.

## B — Untrusted intent, server revalidation, destination identity

`StorefrontCapturedGroupSelection` carries only: complete server `GroupKey`, stable catalog binding
(`ServiceCode`/`OptionCode`), selected ISO date, and the expected confirmation identities
(`ExpectedSnapshotId`, `ExpectedSourceRevision`, `ExpectedSelectionVersion`, `ExpectedAreaGroupId`,
`ExpectedRateRowId`) plus `ExpectedSelectionRevision` on the request. No owner/seller/tenant/money/eligible
flag/accepted-evidence object is accepted from the browser.

Refusals are stable, typed `storefront.captured_selection.*` codes (FA+EN resources added):
`disabled`, `intent_invalid`, `unavailable`, `date_unavailable`, `conflict`, `destination_unverified`,
`evidence_present`.

- Option OFF (or planner missing) + captured intent ⇒ `disabled`; it never falls through to the legacy
  path. A mixture of captured intent with `ShippingMethodCode`/`SelectedDeliveryDate`/
  `SelectedDeliveryTimeWindow` ⇒ `intent_invalid` (no ambiguous hybrid).
- Every server group must be selected (`seenKeys.Count != Plans.Count` ⇒ `intent_invalid`), so an
  unknown/unassigned/unquoted group blocks the WHOLE write; the read view keeps showing it.
- Owner: only `ShippingCapturedQuoteOwnerKind.Store` is writable in this slice. Marketplace/Seller ⇒
  `unavailable` (read support unchanged; no owner is guessed or rewritten).
- Quote must be `Quoted` with a real payable amount and a real receiving slot; otherwise `unavailable`.
- Echo tokens are UNTRUSTED comparison: any mismatch ⇒ `conflict`. `ExpectedSelectionRevision` is parsed as
  exact decimal text before any expensive read; malformed ⇒ `conflict` (never a silent 0).
- Chosen date must exist in that group's own current 213 calendar ⇒ else `date_unavailable`. The stored
  window is derived from the server slot (`HH:mm-HH:mm`), never from a client interval.
- Destination: reuse the EXISTING `PrepareShippingAsync` server-bound saved-address/recipient handling and
  `IGeographyLookup.ResolveLegacyAsync` in the existing supported country context; unresolved ⇒
  `destination_unverified`. The verified ids are persisted as four scalar UUIDs on the existing draft root
  (no geography JSON, no new Geography data/schema).

## C — 231 evidence composed onto the ACTUAL draft groups

`ShippingGroupAcceptedEvidence.Create` is fed exclusively from the current server resolution: full owner,
`SnapshotId`, `DraftId`, `SourceRevision`, format (must be a positive server value), `SelectionVersion`,
matched `AreaGroupId`/`RowId`, optional real type, and freight/packaging/payable/collect exactly as the 216
quote reports them, plus the chosen date and the slot's timezone/ticks and min/max days. Nothing is
recomputed, clamped or inferred. Evidence is attached to the real `CartShippingDraftGroup` created by the
EXISTING `CartShippingGroupPlanner.Plan` (also given the prior groups for stable identity), and every group
must receive evidence or the whole write refuses. Legacy `ShippingAmount` keeps its existing meaning
(payable at checkout); carrier collect is stored only inside evidence.

## D — ONE atomic replacement + real CAS

`IStorefrontShippingDraftStore.ReplaceCapturedSelectionAsync(draft, groups, isNew, expectedSelectionRevision)`
is the single named captured-selection member of the existing store. It opens ONE Order-owned transaction
and performs delete-then-insert inside it, so a partial deletion can never become durable:

- `isNew` requires `expectedSelectionRevision == 0` (absence/legacy token), else `conflict`.
- existing draft: explicit `draft.SelectionRevision != expected` ⇒ `conflict`; the root's
  `SelectionRevision` is a real EF `.IsConcurrencyToken()` on `order.cart_shipping_drafts`, so a concurrent
  writer also fails the UPDATE (no ABA, no auto retry, no reset).
- `BumpSelectionRevision` is monotonic and only on a successful attempt.
- On `DbUpdateConcurrencyException`, an exact root uniqueness violation, or any other failure, the
  transaction is rolled back and `RollbackOwnChangesAsync` restores/detaches ONLY this attempt's graph
  (root scalars back to original, inserted groups detached, deleted groups back to `Unchanged`). No
  `Clear`/whole-context flush: unrelated caller work is never flushed or lost, and a later `SaveChanges`
  cannot leak a rejected selection.

Migration (additive, Order-owned, `20261027180000_AddCapturedSelectionRevisionAndDestination`):
`selection_revision bigint NOT NULL DEFAULT 0`, `destination_country_id`,
`destination_administrative_area_id`, `destination_city_id`, `destination_service_area_id` (all `uuid
NULL`), plus `ck_cart_shipping_drafts_captured_selection_shape` (revision ≥ 0 and every present id not the
empty GUID). No prior migration edited, no backfill, no FK, no cross-module transaction.

Captured drafts cannot be silently downgraded: a legacy `SaveSelectionAsync` (no captured intent) against a
draft that already has evidence refuses `storefront.captured_selection.evidence_present` instead of
repricing/dropping evidence.

## E — Final order and payment remain closed

- `CommitAsync` refuses `storefront.captured_preview.not_ready` for any stored captured draft even with all
  options OFF, before revalidation/order.
- `ProjectPaymentDraftAsync` now applies the SAME refusal, so a legacy-repriced captured draft is never
  presented as payment-ready.
- `StorefrontStartPaymentComposer` reaches final order only through `CommitAsync` (source fact), so the
  normal entry is closed too.
- No captured checkout/reservation/cart conversion/payment/historical endpoint was added; nothing advertises
  `CanCheckout`/activation/eligibility from a draft save.

## F — Executed proof (focused only)

Isolated target: the existing disposable `postgres:17-alpine` container `tb232-order-pg` at
`127.0.0.1:15433/order_tb231` (user `tb231`), normal Order migration chain, identity asserted before DDL.
No shared `tooba_alpha`/store-alpha write, no migration apply to shared, no reset/reseed.

`dotnet test Tooba.Order.Tests` (filtered):
`StorefrontCapturedSelectionWriterTests` (13) + `StorefrontCapturedSelectionStorageTests` (6) +
`StorefrontCapturedPreviewWriteGuardTests` (5) ⇒ **24/24 passed**:

- Writer (13): option OFF refuses captured intent; missing/malformed intents; malformed expected revision
  is a conflict; unresolved destination refused before any quote; positive SINGLE-group save persists one
  evidence group with the verified destination and expects revision 0; existing draft forwards the observed
  revision; two-group selection persists complete per-group evidence with independent money; pointer/version/
  row/date mismatch refuses with no write; mismatched address refuses; Marketplace assignment refuses with
  zero writes; coupon intent reaches the same 217 promotion path.
- Storage (6, real PostgreSQL): first selection persists revision + destination + compact evidence
  atomically; a second selection replaces the graph and increments the revision; zero expected revision on
  an existing selection is a conflict and writes nothing; a concurrently advanced selection is a conflict
  even with the same expected revision (old selection fully intact, one complete graph); a replacement
  leaves no duplicate lines and no mixed graph; **a forced failure BETWEEN the delete and insert stages**
  (duplicate cart-line insert) rolls back to the full old selection, leaves zero Added/Deleted group and
  line entries, restores `SelectionRevision` to the observed value and writes nothing on the next
  `SaveChanges`.
- Guards (5): stored captured draft stays blocked with flags OFF for Commit and for the payment-draft
  projection; the three existing preview guards still hold.

This storage fact found a REAL defect in the first version of the rollback and it was fixed here: after a
mid-stage failure the draft's root kept the bumped `SelectionRevision` (its `OriginalValues` had already been
accepted by the first `SaveChanges`) and the detached groups' lines stayed tracked as `Added`, so a later
`SaveChanges` could have flushed a rejected selection. `RollbackOwnChangesAsync` now restores the revision
explicitly (`RestoreSelectionRevision` + EF original/current values) and ends with a deterministic per-draft
group AND line state reset (kept ⇒ `Unchanged`, everything else ⇒ detached).

Also: `Tooba.Order.Tests` builds 0 errors after the change and `Tooba.Host` builds 0 errors. Suite run over
`FullyQualifiedName~Storefront|~Shipping` (312 facts): 310 pass, 2 fail and both are **PRE-EXISTING** at
base `54b01303b`, not caused by this task:

- `OrderStorefrontArchitectureGuardTests.Application_storefront_uses_contracts_only_no_foreign_application`
  — `StorefrontCapturedPreviewPlanner.cs` (created by 229/231, untouched here) already contained
  `DateTimeOffset.UtcNow` at base (6 hits, verified with `git grep` against `HEAD`). This task's new files
  use the injected `IClock` only.
- `StorefrontShippingPackagingChargeBoundaryTests.Packaging_is_confined_to_the_default_off_operational_shadow`
  — the guard expects 3 files while base `HEAD` already has 8 production files mentioning `Packaging`
  (captured preview planner/models, composer, 231 evidence, `OrderDbContext`). This task adds one more
  legitimate consumer (the writer that composes 231 evidence). Relaxing/repairing that guard was NOT done
  here: it is a 229/231 surface, out of this task's scope, and silently widening a guard would be worse
  than a disclosed red.

## HONEST limits

- Rollback is proven for both reviewed failure points: a concurrency/uniqueness failure at the delete stage
  and a forced failure between the delete and insert stages. Both leave the OLD selection intact, restore
  the revision, and leave nothing for a later `SaveChanges` to flush.
- `DestinationServiceAreaId` is always NULL: the trusted resolver is called without a service-area name
  (the prepared address carries none), so no service area is invented. Destination ids are persisted, but
  the later commit-time revalidation that consumes them does not exist yet.
- The captured READ (`ProjectAsync`) still prices without a coupon because its request model has none; the
  selection write does pass the coupon it is given. A couponed read/selection pair can therefore differ in
  discounted goods facts until the read model carries the coupon.
- No HTTP binding was added or changed: the existing `PUT /v1/storefront/shipping/selection` binds the
  existing request record, whose new fields are additive and model-bound, so the writer is reachable
  through the same single selection route — but it was NOT exercised over HTTP/live in this task (no
  runtime refresh, no frontend edits, no Host restart, no browser trial).
- EF `OrderDbContextModelSnapshot.cs` still does not model the draft table at all (pre-existing stale
  baseline, disclosed by 231); the new migration is hand-written additive SQL, and the runtime model was
  proven equal to the migrated columns by the real relational tests. No future-scaffolding claim.
- Migration 231 and this 232 migration remain WRITTEN / shared NOT APPLIED.
- Not claimed: checkout/parity, historical read, per-order compact evidence at order time, activation,
  price/eligibility/readiness, Marketplace/Seller write, user Visual ACCEPT.

## Preserved / untouched

User's dedicated visible review window (never enumerated/automated/reloaded/closed), live Host/FE, local
preview ON/count 7, runtime DB data, buyer/cart, admin draft/rates/week/pointer/assignments, checked-in
`Enabled=false` default, TB185 paused/default-OFF/PendingAllocation. No frontend edit, no Host/FE restart,
no breadcrumb of unrelated tracked work committed.

Retained unfixed and NOT repaired here: 230-R1 empty-key destination failure/retry gap and in-flight
new-key wait; cross-owner label map / decoder id-coercion; Offer API proof gaps; 227 mobile empty-note
residual; 225-R1 lost unsaved user LOCAL tab edits (no recovery).

## Changed paths

- `src/backend/Modules/Order/Tooba.Order.Domain/Checkout/CartShippingDraft.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Models/StorefrontOrderModels.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Models/StorefrontCapturedPreviewModels.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Ports/StorefrontOrderPorts.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/CapturedPreview/StorefrontCapturedPreviewOptions.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/CapturedPreview/StorefrontCapturedPreviewPlanner.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/CapturedPreview/StorefrontCapturedSelectionWriter.cs` (new)
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/CapturedGroupProjection/StorefrontCapturedGroupProjectionContracts.cs`
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs`
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/Persistence/OrderDbContext.cs`
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/Persistence/Migrations/20261027180000_AddCapturedSelectionRevisionAndDestination.cs` (new)
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/Storefront/StorefrontOrderStores.cs`
- `src/backend/Modules/Order/Tooba.Order.Endpoints/Resources/OrderErrors.resx`, `OrderErrors.fa.resx`
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Ports/IShippingSelectedCapturedRevisionDirectory.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Rates/ShippingSelectedCapturedRevisionContracts.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingCapturedOwnerScopeResolver.cs`
- tests: `Storefront/StorefrontCapturedSelectionWriterTests.cs` (new), `Checkout/StorefrontCapturedSelectionStorageTests.cs` (new),
  `Storefront/StorefrontCapturedPreviewWriteGuardTests.cs`, `Checkout/ShippingGroupGoodsFactsHarness.cs`,
  plus the five existing fakes updated for the new store member.

## Elapsed

Task window measured by this worker: ≈16:23 → ≈17:05 (≈42 min) against a HARD 40 box, with one in-module
correction round (payment-draft guard proof + coupon threading + a pre-existing-guard investigation).
Recorded **NONCOMPLIANT** — not a process PASS.

STOP_FOR_ARCHITECT. Automatic-Next-Task NONE. No 233 envelope.
