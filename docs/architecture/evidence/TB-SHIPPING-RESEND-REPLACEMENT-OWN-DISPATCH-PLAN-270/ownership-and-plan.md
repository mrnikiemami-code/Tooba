# TB-270 — ownership and plan (A): what exists, what is missing, chosen seam

Status: **DESIGN ONLY — no source code was changed by this run.** This document is the source-verified result of
requirement A ("decide narrowly before coding"). Every claim below was read from the current tree at work base
`8e9f42cf72af54101c102a60d6fdfce7be01e473`; line numbers are from that commit.

## 1. Current call path (verified)

| # | Step | File (line) |
| --- | --- | --- |
| 1 | `POST /v1/admin/orders/{checkoutId}/operations` binds `AdminOrderOperationRequest`, maps `resend_shipment` / `resolve_damaged_goods` to MediatR commands | `src/backend/Modules/Order/Tooba.Order.Endpoints/Admin/Operations/AdminOrderOperationsEndpoints.cs:131-134` |
| 2 | Resend: `ResendShipmentCoreAsync` forwards `originalShipmentId`, selected lines and the client idempotency key. **No scheduling intent of any kind is carried.** | `.../Admin/Operations/Services/AdminOrderOperationsOrchestrator.Fulfillment.cs:374-393` |
| 3 | Damaged: `RunDamagedResolutionAsync` → exact-occurrence reconcile → `ResolveDamagedGoodsCoreAsync` | same file `:397-490` |
| 4 | Resend owner: `FulfillmentDirectory.CreateResendShipmentAsync` — payment authorization, serializable transaction, occurrence replay, returned-capacity allocation, new `Shipment` with `ResendOfShipmentId` | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Directories/FulfillmentDirectory.Returns.cs:111-194` |
| 5 | Replacement owner: reserve the purpose-bound healthy hold, then create the linked replacement parcel (`MarkFulfilled`) | `.../Directories/FulfillmentDirectory.Damaged.cs:92-165` + `Domain/Aggregates/DamagedGoodsResolution.cs:203-232` |
| 6 | Queue read: `BuildParcelRow` gives **every** `ResendOfShipmentId != null` parcel `plans = []`, so its timing is classified `Unspecified` with no date | `.../Queries/AdminFulfillmentWorkUnitProjection.cs:203-208`, `AdminFulfillmentDispatchTiming.cs:297-311` |

The empty-plan branch is **correct today**: the attempt really has no accepted plan, and the alternative (reusing
`PlansOf(sellerOrderId, shippingGroupId)`) would lend the original group's expired promise to a new physical attempt.
The gap is not the branch — it is that there is **no lawful way to give the attempt its own plan**.

## 2. Facts already stored (owner + exact location)

| Fact | Owner | Where |
| --- | --- | --- |
| Group accepted evidence: `SnapshotId`, `DraftId`, `SourceRevision`, `SelectionVersion`, `FormatVersion`, `Owner` (kind/tenant/seller), `AreaGroupId`, `RateRowId`, amounts, `ReceivingDate`/`ReceivingOpenTicks`/`ReceivingCloseTicks`/`ReceivingTimeZone`, `MinimumDeliveryDays`, `MaximumDeliveryDays` | Order | `Domain/Checkout/ShippingGroupAcceptedEvidence.cs:214-272` |
| Original planned dispatch **outputs**: `HandoffDate`, open/close ticks, `HandoffTimeZone`, `TransportWorkingDays`, `PolicyVersion`, `ScheduleSnapshotId`, `ScheduleSourceRevision`, `PreparationStartUtc`, `TransferDepartureUtc`, `HandoffDeadlineUtc`, `PreparationMinutes`, `HandoffMinutes` | Order | same file `:552-610` |
| Usable physical readiness evidence: origin receipt + `ReturnedAt` on the source parcel | Fulfillment | `Shipment`/`ShipmentReturnReceipt` (projected as `ShipmentSnapshot.ReturnedAt`) |
| Replacement readiness: decision quantity/outcome/attempt, purpose-bound **non-expiring** hold `damage-replacement:{resolutionId:N}`, linked parcel id | Fulfillment (+ Inventory hold) | `DamagedGoodsResolution.cs:52-77,160-232` |
| Attempt lineage of the new parcel (`ResendOfShipmentId`) | Fulfillment | `Shipment` |
| Captured authoring payload per `(owner, draft, revision)`: `MethodSettings` (`PreparationMinutes`, `HandoffMinutes`, `PreparationMode`), customer `WeeklySchedule` (+ slot duration), `CarrierAcceptance` week, `ProcessingHours` week | Shipping | `Domain/Authoring/ShippingAuthoringRevisionSnapshot.cs:202-218` |
| Pure scheduling policy: forward calendar, latest handoff for a date, backward preparation/transfer/handoff deadlines (policy 1/2/3) | Shipping | `Contracts/Scheduling/ShippingDispatchSchedulingAuthority.cs:266,347,426,540,965` |

## 3. Facts genuinely missing for a NEW commitment (the exact blocker)

The accepted evidence stores the plan's **outputs** but not the **calendar inputs** that produced them: it has no
carrier working-week shape, no customer working-week shape, no processing-week shape and no slot duration. A new
attempt therefore cannot be rescheduled from Order's own rows.

The only existing Contracts read that returns those weeks is
`IShippingCapturedRevisionQuoteLookup.QuoteAsync(ShippingCapturedQuoteLookupRequest)`:

* it **requires** `Destination` (`DestinationIdentifiers`: country + optional area/city/service-area) and optionally
  group weight / item count / amount (`Contracts/Rates/ShippingCapturedRevisionQuoteContracts.cs:283-291`);
* it returns `Settings`, `CarrierWeek`, `ReceivingWeek`, `ProcessingWeek` **only when `Verdict == Quoted`** — the whole
  `Selection` object is `null` otherwise (`Infrastructure/Rates/ShippingCapturedRevisionQuoteLookup.cs:680-745`).

Order does **not** own those inputs any more:

* `OrderShippingGroup` has no destination columns (`Domain/Checkout/OrderShippingGroup.cs:12-79`);
* the destination ids live on the seller's **current** shipping draft (`Domain/Checkout/CartShippingDraft.cs:87-96`),
  which is current configuration, not the accepted snapshot;
* the group's weight / item count are cart-facts at submission time and are not persisted on the group.

So passing `null` measurements makes a weight/count rate mode answer `MeasurementRequired` and return **no weeks**; using
the current draft's destination would silently combine a historical acceptance with current configuration, which the task
forbids. There is **no** existing seam that yields the scheduling weeks alone.

## 4. Chosen seam (minimal, additive, no second engine)

Add ONE narrow Shipping Contracts read that resolves the **scheduling subset** of an already captured revision, with no
rate/coverage/destination involvement:

```
IShippingAttemptSchedulingLookup.ReadAsync(
    ownerKind, ownerTenantId, ownerSellerPartyId, draftId, sourceRevision, serviceCode, optionCode)
 -> { SnapshotId, DraftId, SourceRevision, FormatVersion, ServiceCode, OptionCode,
      MinDeliveryDays, MaxDeliveryDays,
      CarrierWeek, ReceivingWeek, ProcessingWeek, ProcessingContinuous,
      PreparationMinutes, HandoffMinutes }
```

Why this is small and lawful: `ShippingAuthoringRevisionPayload` already carries every one of those fields per
`(draft, revision)` and is destination-independent (`ShippingAuthoringRevisionSnapshot.cs:202-218`); the adapter reuses
the existing `IShippingAuthoringDraftDirectory.GetCapturedRevisionAsync(owner, draftId, sourceRevision)` +
`ReadPayload()` path that `QuoteAsync` itself uses (`ShippingCapturedRevisionQuoteLookup.cs:76-92`), and maps to the
existing `ShippingSchedulingWeekView`. No rate row, no coverage, no destination, no money, no new policy. The only new
Shipping code is the Contracts record/interface + one adapter + DI registration.

## 5. Ownership and persistence decision

* **Order** owns the new customer delivery commitment and the immutable attempt plan
  (`"order".shipment_dispatch_plans`, PK `shipment_id`): binding (checkout / seller order / group / seller),
  `attempt_kind` (Resend | Replacement), `source_shipment_id`, `damaged_resolution_id`, method/service/option,
  readiness instant + readiness kind + the source instants it was derived from, the plan triple
  (`PreparationStartUtc`, `TransferDepartureUtc`, `HandoffDeadlineUtc`), handoff date/open/close/zone,
  `TransportWorkingDays`, `PolicyVersion`, customer receiving date/window/zone, the exact schedule provenance
  (snapshot / draft / source revision / selection version / format version), `PreparationMinutes`, `HandoffMinutes`,
  `occurrence_key` (unique), reviewed actor, `created_at`, and `Status` (Pending | Committed).
* **Fulfillment** keeps owning receipt/readiness, attempt/parcel, lineage and the existing occurrence keys. The parcel id
  must be supplied **by Order** to `CreateResendShipmentAsync` (new optional `Guid? shipmentId`) so the plan row and the
  parcel share one identity.
* **Inventory** keeps owning the healthy-stock hold/readiness evidence; the plan never releases or re-reserves it.
* **Shipping** stays a pure policy provider; it persists nothing for the attempt.

## 6. Write ordering and recovery (parcel can never exist without a plan)

1. Resolve the reviewed selection against the current (authoritative) scheduling inputs; refuse if no longer feasible.
2. Insert the immutable plan row **first**, `Status = Pending`, keyed by the (new) parcel id, unique on
   `occurrence_key` and on `shipment_id`.
3. Call the owning Fulfillment create with that exact parcel id (its existing occurrence replay returns the same parcel).
4. Flip the row to `Committed`.

| Interruption | Recovery |
| --- | --- |
| Crash after (2), before (3) | `Pending` + no parcel = inert row; the exact same occurrence re-runs (3) with the same id and commits |
| Crash after (3), before (4) | the parcel exists and its `Pending` plan is already authoritative; the exact occurrence reconciles to `Committed` and answers the recorded facts |
| Changed date/window/source/quantity under the same occurrence key | typed conflict, zero write, no second allocation |
| Replacement without healthy stock | still no parcel and **no plan row at all** — a pending decision or hold never acquires an actionable promise |

## 7. Read path

`IOrderShipmentDispatchPlanReader.ListForShipmentsAsync(ids)` (Contracts) + one bridge; `AdminFulfillmentWorkUnitContext`
gains `AttemptPlansByShipmentId`, and `BuildParcelRow` becomes
`shipment.ResendOfShipmentId is null ? GroupPlans(sellerOrderId, groupId) : AttemptPlan(shipment.ShipmentId)`.
First dispatch keeps the group plan; no sibling lending; no seller-order earliest-date fallback; a completed parcel stays
`NotQueued` because the plan is only read for a live row.

## 8. What this run did NOT do

No Contracts seam, no table/migration, no command, no queue/detail projection change, no frontend review step, no tests.
Section 3 is the reason: the durable write path has no lawful source for the scheduling weeks today, and inventing them
(original date, browser now, current draft settings, or a fabricated weekly shape) is exactly what the task forbids.
