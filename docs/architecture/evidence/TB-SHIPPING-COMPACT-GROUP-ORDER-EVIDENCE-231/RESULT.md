# TB-231 — compact per-group order evidence (storage + transfer slice)

Base: `5a3070567` (fast-forwarded; the three pre-existing Shipping line-ending drifts were never staged).
Status: **EXECUTED — PARTIAL**. The compact evidence value, its validation, its transfer through the EXISTING
draft-group → plan → immutable order-group materializer, and the additive storage/migration exist and are
proven. **No production writer composes evidence yet**, so captured checkout stays refused by the existing
`storefront.captured_preview.not_ready` guard. Nothing was activated, applied to the shared database, or
restarted. No user Visual ACCEPT is claimed.

## A — ONE cohesive Order-owned compact evidence value

`Order.Domain/Checkout/ShippingGroupAcceptedEvidence.cs` (new) is the only new type:

- **references**: `SnapshotId`, `DraftId`, `SourceRevision`, `FormatVersion` (only `1` accepted), `SelectionVersion`,
  matched `AreaGroupId`, matched `RateRowId`, optional `ShippingTypeId`, optional `Responsibility`
  (reuses the existing `OfferFulfillmentResponsibility`; unknown stays `null`, never inferred),
- **accepted money**: `FreightAmount`, `PackagingAmount`, `Currency`, `PayableAtCheckoutAmount`,
  `CarrierCollectAmount` — all exact decimals, never recomputed, never clamped, never defaulted to IRR,
- **accepted receiving**: `ReceivingStartDate`, `ReceivingEndDate`, `ReceivingTimeZone`,
  optional `MinimumDeliveryDays`/`MaximumDeliveryDays`.

All-or-none: the value is either `null` (legacy group) or complete. `Create` refuses, **before any mutation**:
empty snapshot/draft/area/rate id, `Guid.Empty` type id, `SourceRevision<=0`, unsupported `FormatVersion`,
`SelectionVersion<=0`, an undefined responsibility enum value, negative money, a currency that is not exactly
three upper-case letters, default/inverted receiving interval, empty timezone, and half/negative/inverted
delivery days. No id is generated, no timezone or owner is invented, no amount is clamped. Fault codes are stable
under `ShippingGroupEvidenceFault` (`shipping.group.evidence.*`).

Wiring (existing path, no parallel framework):

- `CartShippingDraftGroup`: nullable `AcceptedEvidence` + `ReplaceAcceptedEvidence(evidence, now)`, which also
  refuses a currency that does not match the group currency (`CurrencyMismatch`) or a checkout-payable that does
  not match the group's accepted `ShippingAmount` (`PayableMismatch`), and touches **only that group**
  (`UpdatedAt` included). Clearing is explicit `null`.
- `OrderShippingGroupPlan`: trailing optional `AcceptedEvidence`; `OrderShippingGroup.Create` applies the same
  currency/payable coherence and stores it with a private setter — **no public mutation path** after creation.
- `StorefrontShippingService.BuildOrderGroupPlans` now carries `group.AcceptedEvidence` into the plan, so the
  real draft → plan → materializer transfer is one field, not a second pipeline.
- No Shipping.Domain/Infrastructure reference, no new quote authority, no HTTP endpoint or DTO that accepts an
  accepted-evidence object from a browser, no Payment/Settlement payer change.

## B — storage: 19 nullable columns on the existing tables, one additive migration

`20261027150000_AddShippingGroupAcceptedEvidence` adds the same **19 nullable columns** to
`order.cart_shipping_groups` and `order.order_shipping_groups`:

`accepted_snapshot_id` (uuid), `accepted_draft_id` (uuid), `accepted_source_revision` (int),
`accepted_format_version` (int), `accepted_selection_version` (int), `accepted_responsibility` (int),
`accepted_area_group_id` (uuid), `accepted_rate_row_id` (uuid), `accepted_shipping_type_id` (uuid),
`accepted_freight_amount`, `accepted_packaging_amount`, `accepted_payable_at_checkout_amount`,
`accepted_carrier_collect_amount` (numeric(18,2)), `accepted_currency` (varchar(3)),
`accepted_receiving_start_date`, `accepted_receiving_end_date` (date), `accepted_receiving_time_zone` (varchar(64)),
`accepted_minimum_delivery_days`, `accepted_maximum_delivery_days` (int).

Plus exactly ONE small index for logical reference audit: `ix_order_shipping_groups_accepted_snapshot_id`.
No cross-module FK, no snapshot copy, no history subsystem, no generic event store, no backfill
(all legacy rows stay `NULL`).

Reused existing group fields instead of duplicating them: owner (`SellerPartyId`), stable method
(`MethodCode`/`ServiceCode`/`OptionCode`), accepted charge (`ShippingAmount`/`ShippingChargeSnapshot`), currency,
minimum delivery date, selected/requested delivery date and window, lead days. The evidence adds only what those
columns could not express.

**Measured DB cost** (not an estimate): 19 nullable scalar columns per group row in each table, zero
`json`/`jsonb` columns, verified by `information_schema` in the proof. Evidence size is constant per GROUP and
independent of cart/order line count.

Migration state: **WRITTEN, NOT APPLIED** to the shared local `tooba_alpha`/store-alpha or any shared instance.
It was applied only to the task-owned disposable PostgreSQL (below), with the normal Order migration chain.

Honest note on `OrderDbContextModelSnapshot.cs`: `dotnet ef migrations add` re-scaffolded the ENTIRE schema
because the checked-in snapshot is pre-existing stale, so that generated migration was deleted and the snapshot
restored byte-for-byte (`git checkout`). The delivered migration is hand-written additive SQL, matching the
precedent of `20261016090000_AddCartShippingGroups` / `20261016100000_AddOrderShippingGroups`, which also carry no
Designer file and do not rewrite the snapshot. No existing migration was edited and no baseline was regenerated.

## C — focused proof (no whole-suite loops)

New fact classes only, on ONE task-owned disposable PostgreSQL `127.0.0.1:15433 / order_tb231 / tb231`
(`postgres:17-alpine`, container `tb231-order-pg`), identity verified (`current_user=tb231`,
`current_database=order_tb231`) **before** DDL. The normal Order migration chain was applied there
(`dotnet ef database update`, ending at `20261027150000_AddShippingGroupAcceptedEvidence`).

`TOOBA_TB231_ORDER_POSTGRES` is required; a missing/foreign host, port, database or role **fails closed**
(no skip, no InMemory, no fabricated success).

Executed facts (21/21 passed, `Tooba.Order.Tests`):

1. **Two groups, ONE `SnapshotId`** with different area/row ids, freight/packaging and receiving dates keep their
   own accepted values after the real materializer and after persist/readback in a fresh context; group and line
   membership identity is unchanged.
2. **Draft replacement never rewrites a stored order**: the draft group's evidence is replaced by a new snapshot /
   amount / selection version in the database, and the previously materialized `order_shipping_groups` row keeps
   the exact old `SnapshotId`, freight, packaging, receiving start date and selection version. The read uses only
   Order storage — no Shipping draft or publication pointer is consulted.
3. **Legacy groups** (no evidence) still materialize and read back with `NULL` evidence and their original charge.
4. **Invalid/partial evidence** (15 refusal cases) and **incoherent evidence** (currency/payable mismatch on the
   draft group and on the order plan) raise the expected stable fault and leave the target group unchanged
   (`AcceptedEvidence` still null, `UpdatedAt` untouched) — no partial mutation.
5. **Storage shape**: 19 `accepted_*` columns in each table, all nullable, **0 json/jsonb**, exactly one
   `accepted_snapshot_id` index.
6. Replacing/clearing one group's evidence leaves the sibling group's evidence untouched.

Also re-executed unchanged `OrderShippingGroupMaterializerTests` **9/9**. Builds: Order.Infrastructure 0 errors,
Order.Tests 0 errors, Host 0 errors. Not run: 119/188/194/full Order/frontend/guard wildcard loops.

## D — first user handoff (bounded)

`230-R1` is accepted FOR READ-ONLY user retest. The local preview runtime was left exactly as-is (Host `5088`
PID 24660, FE `3000` PID 28288, preview ON / date count 7) — no rebuild, restart, migration or data write.

ONE visible installed-Chrome window was opened for the user with the **existing task-owned profile**
`D:\temp\tb229r2-chrome` on `http://127.0.0.1:3000/fa/shipping` (browser PID 34788). Before launching it, ONLY
that verified profile's own headless browser process (PID 33524, port 9336) was stopped — never a user browser
and never by image name. Its existing normal logged-in session/cart is what the user inspects. That window is
the dedicated REVIEW window and was not automated, reloaded or enumerated after handoff; no credentials or tokens
were printed or committed, and no destination/address/cart/rate was changed.

All 231 proof afterwards used the isolated database above, never the review window or the live runtime.

## HONEST GAPS

- **No production writer yet.** Nothing in the live request path composes accepted evidence, so every real
  draft/order group stores `NULL`. The transfer is proven end-to-end by tests, not by a live checkout.
- Captured checkout remains refused (`storefront.captured_preview.not_ready`); no selection/commit/order/reservation/
  payment path was opened, and `SaveSelectionAsync`/`CommitAsync` were not modified.
- No historical reader/API exposes the evidence yet; "no mutable Shipping draft needed to display history" is
  proven as a storage fact, not as an implemented read endpoint.
- The compact evidence deliberately stores no week/region/rate-condition/translation copy, so it cannot by itself
  reconstruct a full authored plan — by design.
- Retained, NOT fixed here: the 230-R1 empty-key (`key ""`) destination failure/retry gap and in-flight new-key
  wait, cross-owner label map/decoder id-coercion limits, Offer API proof gaps, 227 mobile empty-note residual and
  the unrecovered 225-R1 user LOCAL tab edit loss. SingleStore trial does not certify Marketplace.
- The Order model snapshot remains pre-existing stale (unchanged by this task); a future `migrations add` will
  re-scaffold until that separate baseline problem is addressed.
- Elapsed ≈16 min vs HARD35, one correction round (currency length, test fixture SQL). Still NOT a process PASS.

## NEXT (Architect)

The parity slice: a trusted server composer that revalidates the selected captured revision + cart/version +
Offer/type/quote/date and writes this evidence inside the existing atomic draft/order boundary, plus the
historical read that uses only Order storage. Captured checkout stays `not_ready` until that lands.

STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. TB185 paused, default-OFF, PendingAllocation preserved.
