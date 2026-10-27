# TB-229 — real Storefront group cards read connection (PARTIAL, honest)

Base `02250f55f` (fast-forwarded from `1e466786d`; the three pre-existing Shipping line-ending drifts were
never staged and remain unmodified). Task status: **EXECUTED — PARTIAL**, not accepted, no live page trial,
no migration applied, no Host restart.

## What is actually connected now

`StorefrontShippingService.ProjectAsync` (the existing Order projection behind the real Storefront shipping
GET) now builds a **read-only** captured preview in the same request:

real cart/access/version → **one** batch `IOfferFulfillmentAssignmentLookup.GetAssignmentsAsync` over the
distinct offer ids → explicit partition (responsibility + real assigned type + real offer seller) → catalog
binding identities from the existing `IShippingCatalogReader` (active service with `NULL` option, or each
active option) → **one** `IShippingSelectedCapturedRevisionLookup.ReadAsync` per distinct full-owner +
canonical service/option (cached by owner+binding inside the request) → **one**
`StorefrontCapturedGroupProjectionComposer.ComposeAsync` (218) for the complete cart partition, which itself
performs the single whole-cart 217 goods read and one 216 quote per candidate.

`StorefrontShippingProjection` gained ONE additive nullable field `CapturedPreview`. It is `null` whenever the
new option is OFF, so the legacy response shape/flow is unchanged.

## Option (checked in, default OFF)

`Tooba:StorefrontCapturedPreview:Enabled` — `StorefrontCapturedPreviewOptions.SectionName`. Absent section =
`false` = zero Offer/Fulfillment/Shipping calls from this path and the previous projection exactly as before.

## Write refusal ships with the read preview

When the option is ON, both existing paths refuse **before any write**, with the same stable code
`storefront.captured_preview.not_ready` (FA/EN resources added to `OrderErrors*.resx`):

- `StorefrontShippingService.SaveSelectionAsync` → `EnsureCapturedPreviewIsNotCheckoutReady()` is the first
  statement, before `RequireCartAsync`, address preparation, selection or draft persistence.
- `StorefrontShippingService.CommitAsync` → the same guard before the draft read, so no selection/order/
  reservation/payment/cart conversion can start. Disabling the FE button alone is therefore not the gate.

## Fail-closed rules proven by the new class

- offer missing from the batch → whole preview `assignment_missing`, no catalog/219 call;
- batch row with a different `OfferId`/`SellerPartyId` than the cart line → `assignment_contradictory`;
- `Responsibility = null` → group **stays visible** with `unassigned` and no methods (never guessed);
- assigned active type missing from the real catalog → `type_unavailable`;
- `Seller` responsibility on SingleStore → `seller_owner_unsupported` (never reinterpreted as Store, and 219
  is not even read);
- selected pointer whose captured `ShippingTypeId` is not the assigned active type, or a `Cleared`/`Absent`
  pointer → no candidate, group visible with `no_selected_capture`, composer never invoked;
- zero candidates across the whole cart → early return with per-group reasons and **zero** 218/217/216 calls.

## Existing Storefront cards (additive, same DOM/CSS)

New `storefront-shipping-captured-preview.tsx` renders inside the existing `StorefrontShippingGroupCards`
block (one optional prop, default `null`). It reuses the current card classes: per group it shows the real
seller name (from the cart's own groups), the real type label from Fulfillment Contracts, the group's own
post-discount/tax goods amount, each method's own freight + packaging (rendered separately, exact server
strings via `formatOfferAmount`, no float recompute), that method's own eligible dates, and — on date change —
**that date's own** `subLabel` interval, never a union of weekdays. Unavailable groups stay visible with one
short localized reason. Nothing here is selectable-for-checkout: there is no save/commit action, and the
server refuses anyway. `capturedPreview.enabled === false` renders nothing.

## Proof executed

- `dotnet build` `Tooba.Order.Application` → 0 errors; `Tooba.Order.Infrastructure` → 0 errors.
- ONE new affected class `StorefrontCapturedPreviewPlannerTests` → **9/9 passed**, covering the default-OFF
  control (zero assignment reads), missing/contradictory assignment, unassigned group visibility, inactive
  type, SingleStore Seller unsupported (zero 219 reads), one 219 read per distinct catalog binding with the
  Store owner+tenant derived from trusted context, foreign captured type rejected, cleared pointer rejected,
  and “no candidate ⇒ composer/217 never called” (`FixedGoodsFactsReader(null)` would throw if invoked).
- `npx tsc --noEmit` on the frontend → clean after the final source change.

## NOT delivered / honest limits (this is why it is PARTIAL)

- **No live page trial.** No Host rebuild/restart, no real Storefront URL read, no desktop/390 screenshots.
  The running Host (5088) and FE (3000) were **not** refreshed for this task, so the new projection field and
  the ON-option path have not been observed in a browser.
- **Migration APPLIED.** `20261027120000_AddOfferFulfillmentAssignment` was applied on the real local
  `tooba_alpha`/store-alpha through the normal `Tooba.MigrationRunner` after a plan showed exactly one pending
  module: `Migration Apply module=Offer ... current=20261027120000_AddOfferFulfillmentAssignment pendingCount=0
  result=ok` (Shipping stayed `20261025130021_SelectedCapturedRevision pendingCount=0`, every other module
  `pendingCount=0`). No reset/backfill/reseed. Legacy rows keep `NULL` in both new columns; that state was not
  additionally queried by SQL in this task.
- **No positive composition executed.** The new class proves everything up to the composer boundary; the
  real 216/217/218 monetary+calendar path is covered only by its own earlier classes, not re-run here. Two
  groups keeping different fees/week schedules and the real 99/198 per-group goods are therefore **not**
  proven in this task.
- No Offer API/migration integration class was added (section 1), so the admin endpoint's deny-before-body,
  malformed/null/active/inactive-type facts remain source-reviewed only from 228.
- Elapsed ≈70 min vs the HARD45 box (implementation + two self-inflicted fixture/corruption detours) —
  recorded as NONCOMPLIANT, NOT a process PASS.
- Not run: Offer/Shipping/194/188/solution suites, browser, migration, Host.

## Next (Architect)

1. Apply `20261027120000_AddOfferFulfillmentAssignment` through the normal runner after a real pending-set plan,
   and record nullable legacy assignment state.
2. Run one live Storefront shipping GET with the option ON in a task-owned browser (real cart + one real DEV
   assignment) and store desktop/390 screenshots of the existing group cards.
3. Then the parity slice: compact shared-snapshot order evidence + selected-captured-revision commit, replacing
   the `storefront.captured_preview.not_ready` refusal only when a captured candidate is actually committable.
