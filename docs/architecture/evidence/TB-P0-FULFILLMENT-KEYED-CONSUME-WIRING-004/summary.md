# TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004 — Evidence Summary

Mode: `IMPLEMENT_ONE_BOUNDED_CROSS_MODULE_WIRING_WITH_STOP_GATES`
Parent: `TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003`
Program: `TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY`

## Preflight

| Item | Value |
| --- | --- |
| Expected-Starting-HEAD | `e7d0bf978fcf8b3f90bd0300474ab23e486d3289` |
| Starting HEAD | `e7d0bf978fcf8b3f90bd0300474ab23e486d3289` |
| Starting `origin/main` | `e7d0bf978fcf8b3f90bd0300474ab23e486d3289` |
| Tracked worktree | clean on all paths this task owns |
| Live Host writes / deploy / migration / SQL / dispatch / cancel | **none** (git-only change set) |

Read: `AGENTS.md`, `.cursor/skills/tooba-backend-feature/SKILL.md`, Journal-003 evidence,
parent `TB-P0-FULFILLMENT-PACKAGE-DISPATCH-ATOMICITY-001` Gate-B evidence, and the exact
caller/port/adapter types listed below.

## A. Trace — which stable IDs actually exist

| Identity | Available at the consolidated-package call site? | Source |
| --- | --- | --- |
| `packageId` | yes | `package.ConsolidatedPackageId` (`DispatchConsolidatedPackageAsync`) |
| `shipmentId` | yes | `step.Shipment.ShipmentId` (per member shipment) |
| `reservationId` | yes | `FulfillmentItem.ReservationId` |
| `orderLineId` | yes | `FulfillmentItem.OrderLineId` / `shipmentLine.OrderLineId` |

The order-line ID **is** available on `FulfillmentItem` and on `ShipmentItem`, so no weaker
`reservationId+shipmentId+packageId` fallback was needed.

## B. Consumer key format, provenance, retry behaviour

Single owner of the format — `FulfillmentInventoryConsumptionKey.For(...)`
(`Tooba.Fulfillment.Application.Fulfillments.Ports`):

```text
pkg:{packageId:N}:shp:{shipmentId:N}:line:{orderLineId:N}
```

* **Length**: 78 characters (`4 + 32 + 5 + 32 + 6 + 32`), safely under the 128-character
  ceiling of `inventory.reservation_consumptions.consumer_key`.
* **Provenance**: three persisted GUID identities already present in the Fulfillment model.
  No clock, no randomness, no hashing — therefore no collision rationale is needed.
* **Determinism / retry**: for the same package, member shipment and order line the key is
  byte-identical on every attempt. A retry after a Fulfillment `SaveChangesAsync` failure
  reproduces the same key, so Inventory's keyed `ConsumeAsync` recognises the identical
  logical consumption and returns success without deducting stock twice.
* **Uniqueness**: each reservation is bound to exactly one order line, and order lines are not
  duplicated within a single shipment, so the triple is unique per reservation.

## C. Routing — only consolidated-package dispatch is keyed

* `FulfillmentDirectory.ConsumeInventoryForShipmentAsync` gained an **optional**
  `Guid? consolidatedPackageId = null` parameter and delegates the single-line consume to the
  new cohesive helper `ConsumeReservationAsync(fulfillmentItem, shipment, consolidatedPackageId, ct)`.
* `DispatchConsolidatedPackageAsync` passes `consolidatedPackageId: package.ConsolidatedPackageId`;
  every other caller (individual `DispatchShipmentAsync`, returns, cancel, release) keeps the
  parameter unset and therefore stays on the **unkeyed** seam unchanged.
* `IFulfillmentInventoryGateway` gained an additive overload
  `ConsumeReservationAsync(Guid, string consumerKey, CancellationToken)` with a **default
  interface implementation** that falls back to the unkeyed member. This keeps all existing
  test doubles and any other implementation compiling without change.
* Only `FulfillmentInventoryGateway` overrides the overload and forwards to Journal-003's
  existing additive `IFulfillmentInventoryLifecyclePort.ConsumeAsync(reservationId, consumerKey, ct)`.
  Inventory journal, migration and schema were **not touched**.

## D. Preserved parent guarantees

* Two-phase package validation (`BuildDispatchStepsAsync` writes nothing) and the single
  Fulfillment `SaveChangesAsync` per package remain exactly as delivered in `04e3aa79`.
* No mutation of pre-dispatched members; the `NeedsDispatch` filter is unchanged.
* Held/consumed reconciliation W6 was **not** touched; no tracking-generation change.
* No global cross-module transaction was claimed. A keyed-consume failure still propagates out
  of `DispatchConsolidatedPackageAsync` before the Fulfillment commit, so no member is
  persisted as dispatched (proved by test 2).

## Files changed (production 3 + test 1, inside the hard cap)

| File | Change |
| --- | --- |
| `.../Fulfillment.Application/Fulfillments/Ports/IFulfillmentInventoryGateway.cs` | additive keyed overload (default DIM) + `FulfillmentInventoryConsumptionKey` owner |
| `.../Fulfillment.Infrastructure/Gateways/FulfillmentInventoryGateway.cs` | override forwarding the key to Inventory |
| `.../Fulfillment.Infrastructure/Directories/FulfillmentDirectory.cs` | optional `consolidatedPackageId` on the consume seam |
| `.../Fulfillment.Infrastructure/Directories/FulfillmentDirectory.Packages.cs` | pass the package id + cohesive keyed-consume helper |
| `.../Fulfillment.Tests/Behavior/FulfillmentCharacterizationTests.cs` | two targeted tests + recording/throwing seams |

`FulfillmentDirectory.cs` 725 → 726 LOC and `FulfillmentDirectory.Packages.cs` 477 → 500 LOC,
both under the ARCH-SIZE-001 800-LOC ceiling enforced by
`Fulfillment_directory_stays_under_the_arch_size_ceiling`.

## Verification

1. **Focused compile** — `Tooba.Fulfillment.Infrastructure` → `Build succeeded. 0 Warning(s) 0 Error(s)`.
2. **Focused test command (run once)** — `Tooba.Fulfillment.Tests`:
   `Passed! - Failed: 0, Passed: 72, Skipped: 0, Total: 72` — includes the two new tests:
   * `Consolidated_package_dispatch_forwards_deterministic_consumer_key`
   * `Consolidated_package_dispatch_propagates_keyed_consume_failure`
3. **Host compile** — `Tooba.Host.Tests` compiles (`0 Error(s)`), proving the default interface
   implementation kept all other gateway doubles compiling untouched.

## Pre-existing failures (not caused by this change)

`Tooba.Host.Tests.ConsolidatedPackageTests` reports 6 failures — all **domain/exception-shape
assertions** in an unmodified file (e.g. `Assert.Throws<InvalidOperationException>` while the
domain throws the typed `ContractOperationException`, and `Domain_assign_tracking_only_while_created`
expecting a different message). They were reproduced **identically** on a pristine
`e7d0bf978` detached worktree with none of this task's edits, so they are baseline failures of
the parent program and out of this task's bounded scope. Per the strict no-loop budget they
were recorded, not debugged or re-run.

## Remaining P0 risks / unresolved gaps

* R1 — Cross-module atomicity is still **not** claimed: the Fulfillment commit and the Inventory
  consume remain two transactions. This task only makes the Inventory side *replay-safe*; W6
  (held/consumed reconciliation) is explicitly out of scope.
* R2 — The key is only as stable as the member shipment identity. If a package is cancelled and
  its shipment re-created, the new `shipmentId` yields a new key, which is the correct semantic
  (a new physical shipment is a new logical consumption) but means cancellation-then-redispatch
  does not reuse the old journal row.
* R3 — Journal-003's migration is still **undeployed** in the live Host; this wiring is
  therefore dormant in production until the architect authorizes that deployment.
