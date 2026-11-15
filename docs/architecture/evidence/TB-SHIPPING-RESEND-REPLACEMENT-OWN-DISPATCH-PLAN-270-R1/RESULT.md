# TB-270-R1 — RESULT — PASS (one additive read-only Shipping Contracts seam)

Decision string:
`ONE_ADDITIVE_READ_ONLY_SHIPPING_CONTRACTS_SEAM_EXPOSES_THE_EXACT_CAPTURED_SCHEDULING_INPUTS_WITHOUT_DESTINATION_MEASUREMENTS_CURRENCY_RATE_SELECTION_OR_PROVIDER_AND_WITH_TYPED_REFUSALS_AND_ZERO_ORDER_OR_FRONTEND_CHANGE`

Status: **PASS** within the issued 270-R1 scope. NOT Architect ACCEPT, NOT user acceptance, NOT "shipping complete", no
plan/feasibility/provider/financial claim. One worker, one task, no delegation, no historical agent resumed.

Work base == issued Expected-Base-SHA == local `origin/main`: `1b1f8b68f0a1fa0278293f2ca91998d477943e2b`.

## 1. Root the seam answers

270 established (and this run re-read in source at the base) that a resend/replacement attempt has no lawful way to obtain
its own plan because the accepted evidence stores the plan's outputs but not the week shapes, and the only Contracts read
that returns the weeks is the rate-quote lookup — which requires a Destination plus optional measurements and yields weeks
only when a rate row matched. Additionally, the transport duration is not payload-level: `MinDeliveryDays`/
`MaxDeliveryDays` live inside `Groups[].RateRows[]`, so a read that "just returns the weeks" cannot supply transport.

## 2. Delivered (code)

| Area | Path | What it does |
| --- | --- | --- |
| Contracts | `src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Scheduling/ShippingAttemptSchedulingContracts.cs` | `IShippingAttemptSchedulingLookup`, `ShippingAttemptSchedulingLookupRequest`, `ShippingAttemptSchedulingInputs`, `ShippingAttemptReceivingWeekView`, `ShippingAttemptSchedulingResult`, `ShippingAttemptSchedulingVerdict`, and the stable reason/input codes. Contracts-only references |
| Adapter | `src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Scheduling/ShippingAttemptSchedulingLookup.cs` | exact `GetCapturedRevisionAsync` + `ReadPayload()`; canonical service/option comparison; exact group + rate-row resolution; loss-less week mapping; typed refusals; no write |
| Registration | `src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/DependencyInjection/ShippingModule.cs` | one scoped registration beside the other read-only captured seams, commented as having no caller yet |

Request identity is explicit: owner kind + owner tenant/seller, draft, source revision, service/option, exact `GroupId` and
exact `RateRowId`, optional caller-bound `ExpectedSnapshotId`. The result is a typed verdict + stable reason code; only
`CapturedInputs` carries a payload, every refusal carries none. The payload maps, without inventing: that row's own stored
`MinDeliveryDays`/`MaxDeliveryDays`, the stored preparation/handoff minutes, the stored processing mode, the receiving week
(with its own slot duration), the carrier week and the processing week — each with its own stored timezone — plus the
stable codes of inputs the capture never stored.

## 3. Refusals (all proven, all payload-free)

`OwnerScopeUnavailable`, `CapturedRevisionUnavailable` (absent revision / foreign owner / corrupt body / body headers
disagreeing with the row), `SnapshotMismatch`, `ServiceOptionMismatch`, `GroupUnavailable`, `GroupAmbiguous`,
`RateRowUnavailable`, `RateRowAmbiguous`. Unexpected dependency exceptions and caller cancellation propagate unchanged —
they are never disguised as "capture unavailable".

## 4. Focused proof (run once, after the final change)

```
dotnet test Modules\Shipping\Tooba.Shipping.Tests\Tooba.Shipping.Tests.csproj -c Release \
  --filter "FullyQualifiedName~ShippingAttemptSchedulingLookupTests"
  -> Passed!  Failed: 0, Passed: 7, Skipped: 0, Total: 7  (578 ms, net8.0)
```

Seven facts cover the issued A–F cases: exact mapping with empty `MissingInputs` and a read-only assertion (zero tracked
entities, one capture row); two rows in one group answering only their own duration plus a count/amount-only row read with
no destination/measurement/currency, and a group id not being a ticket to a foreign row; wrong tenant/edition, absent
revision, foreign-owner capture and snapshot mismatch; wrong service/option, canonical trim+case acceptance, unknown
group, duplicate group, duplicate rate row; legacy format 1 (no carrier week/processing week/slot duration), format 3
(processing week only missing), absent timing pair and unspecified processing mode reported as missing, corrupt format
version and mismatched headers refused; dependency failure and cancelled token propagating unchanged; the real module
registering exactly one scoped instance.

## 5. Builds

```
dotnet build Tooba.slnx -c Release   -> 0 Error(s), 379 Warning(s)
```

No publish, no restart, no migration, no DDL, no business SQL, no fixture, no provider call, no FE change, no FE rebuild.
Host 5088 and FE 3000 were not touched by this round.

## 6. Boundaries preserved

No Order/Fulfillment/Inventory/Storefront/queue/operational-detail/frontend file changed; no caller wired; no table,
column, migration, command, endpoint or projection; no change to the captured-quote or authoring behaviour; the existing
`ShippingDispatchSchedulingAuthority` remains the only scheduling authority and is not duplicated. `HasCompleteInputs`
explicitly means only "all inputs this seam declares are stored" — never feasibility, activation, coverage or eligibility.

## 7. Unproven / deliberately out of scope

* No attempt plan exists and no parcel gained a plan; a resend/replacement row still reads `Unspecified`
  («موعد ارسال مشخص نشده») — unchanged and truthful.
* No Order-owned `shipment_dispatch_plans` table/migration, no `plan_shipment_dispatch` command, no preview calendar, no
  queue/detail wiring, no review step: all of that is **270-R2, NOT issued**.
* No live caller, so the seam has no runtime behaviour to observe; proof is the focused suite on the real directory and
  the real payload with EF InMemory, not a relational or hosted proof.
* 271 (job-stage filters) remains **NOT ISSUED**.

## 8. Review surface

No new action exists, therefore there is no review URL for this increment: the served UI is byte-identical to TB-269's
build (`BUILD_ID UmAnnlHYHCf-Sa5-dTMkm`) and shows the same truthful `Unspecified` state for resend/replacement parcels.
The previous read-only reference surfaces remain `http://localhost:3000/fa/admin/fulfillments` and
`http://localhost:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations`.
