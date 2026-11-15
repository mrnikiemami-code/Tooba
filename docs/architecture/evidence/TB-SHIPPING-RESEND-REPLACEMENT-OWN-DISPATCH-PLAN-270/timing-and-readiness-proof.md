# TB-270 — timing and readiness (B)

Status: **DESIGN / SOURCE ANALYSIS ONLY.** No behaviour was changed, so there is no re-measured output; what follows is
what the current source can and cannot answer.

## 1. Readiness of a NEW physical attempt — authoritative inputs that DO exist

| Attempt kind | Lawful readiness lower bound | Source (read at `8e9f42cf7`) |
| --- | --- | --- |
| Same-goods resend (usable returned units) | the instant the source parcel's physical origin receipt was recorded (`ShipmentReturnReceipt` → `ShipmentSnapshot.ReturnedAt`), and never earlier than the new parcel's own creation because allocation happens after the receipt | `FulfillmentDirectory.Returns.cs:56-108`, `AdminFulfillmentWorkUnitProjection.cs:215-217` (`shipment.Status == Returned && ResendableOf(...) > 0`) |
| Healthy-stock replacement | the instant its own purpose-bound healthy hold exists; the linked parcel is created only **after** the hold is durably committed, so the replacement parcel's `CreatedAt` is a conservative, authoritative lower bound | `FulfillmentDirectory.Damaged.cs:92-165`, `DamagedGoodsResolution.cs:174-232` |

A damaged receipt is never resend capacity (`AdminFulfillmentWorkUnitProjection.cs:218-221`,
`DamagedGoodsResolution.Outcome`), so a replacement can never inherit a resend readiness instant and vice versa.

Deliberately **not** used as readiness: the original order creation instant, the original dispatch/`DispatchedAt`
instant, the original accepted `PreparationStartUtc`, and any browser clock. None of them is evidence that the new
goods exist.

## 2. Scheduling inputs — where they live and where they are missing

Outputs of the accepted plan are stored (handoff window + zone + transport days + prep/handoff minutes + the three
deadlines); the **week shapes** that produced them are not stored on the order at all (see
`ownership-and-plan.md` §3). Consequently:

* Forward calendar (`BuildReceivingCalendar`) and backward deadlines (`ResolvePreparationDeadlines`, policy 3) **cannot**
  be re-run from Order data;
* the only lawful source is Shipping's captured authoring payload, reachable today only through the rate-quote seam that
  demands a destination + measurement inputs Order no longer owns.

## 3. Meaning of the three instants (no double counting) — as the existing authority defines them

| Field | Meaning (must be preserved verbatim) |
| --- | --- |
| `PreparationStartUtc` | latest instant at which physical preparation must **start**; it is a deadline, never "packing finished" |
| `TransferDepartureUtc` | latest instant at which the parcel must leave the seller toward the carrier |
| `HandoffDeadlineUtc` | carrier acceptance **close** instant, exclusive; equals the dispatch deadline |

There is exactly one preparation allowance (`PreparationMinutes`, elapsed or counted inside the explicit processing week
per `PreparationMode`) and one transfer allowance (`HandoffMinutes`). The authority already counts them once
(`ShippingDispatchSchedulingAuthority.ResolvePreparationDeadlines`), so the new attempt must reuse that call rather than
add a second allowance; the source-ready instant is an **input** to it, not an extra duration.

## 4. Original customer date reuse rule

The original accepted `ReceivingDate`/window may be proposed only when it is still feasible from the new readiness
(the forward calendar contains it). Otherwise the review must present genuinely feasible dates and the operator must
choose one deliberately; no silent clamp, no "recompute and save", no assertion of customer consent, no carrier booking
claim. This is the same rule the storefront already applies (`StorefrontPlannedDispatchResolver.TryResolve` +
`StorefrontCapturedGroupCompositionCore`), reused rather than duplicated.

## 5. Why nothing could be proven this run

Because no code path was added, the only truthful statement is the current, already-verified behaviour: a resend or
replacement parcel is classified without a plan (`AdminFulfillmentDispatchTimingAuthority.Classify(pending, inTransit,
[], nowUtc)` → `Unspecified`, `HasPlan = false`, no handoff/customer date) and the queue therefore shows it under
«موعد ارسال مشخص نشده» instead of borrowing the shipped parcel's expired promise. Any "new plan" number in this result
would have been invented, so none is reported.
