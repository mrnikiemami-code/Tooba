# TB-P0-KEYED-CONSUME-FAIL-CLOSED-004R — Evidence Summary

Mode: `IMPLEMENT_MINIMAL_FAIL_CLOSED_CORRECTION_STOP_FOR_ACCEPTANCE`
Parent: `TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004`
Program: `TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY`

## Preflight

| Item | Value |
| --- | --- |
| Expected-Starting-HEAD | `5f9e4d0fcb332317ce5c4d0089a6b43f301e9765` |
| Starting HEAD | `5f9e4d0fcb332317ce5c4d0089a6b43f301e9765` |
| Starting `origin/main` | `5f9e4d0fcb332317ce5c4d0089a6b43f301e9765` |
| Tracked worktree | clean (`git status --porcelain --untracked-files=no` empty) |
| DB / migration / deploy / live Host mutation / restart / dispatch | **none** |

No reset, rebase, stash or force-push. No deferred architecture findings opened.

## Correction 1 — fail-closed default interface member

`IFulfillmentInventoryGateway.ConsumeReservationAsync(Guid, string consumerKey, CancellationToken)`
previously delegated silently to the unkeyed `ConsumeReservationAsync(Guid, CancellationToken)`,
which cannot prove replay identity and could deduct inventory without consumer identity.
The default member now throws:

```text
NotSupportedException
"fulfillment.inventory.keyed_consume_not_supported: this IFulfillmentInventoryGateway
 implementation does not support keyed reservation consumption; keyed consume must never
 fall back to the unkeyed path."
```

* Method signatures unchanged; no error-catalog code invented (a `NotSupportedException` with
  stable explicit text, consistent with the repository's use of `NotSupportedException` for
  unsupported-capability seams).
* A missing implementation now **fails closed** and can never deduct inventory.
* Non-consolidated-package flows never call this member, so their unkeyed semantics are unchanged.

## Correction 2 — accurate key-length claim

The comment claiming `pkg:{packageId:N}:shp:{shipmentId:N}:line:{orderLineId:N}` is 78 characters
was wrong. Corrected to **111** characters (`4 + 32 + 5 + 32 + 6 + 32`), still `<= 128`, the
`inventory.reservation_consumptions.consumer_key` ceiling. **The actual key format is unchanged.**

## Mandatory gates

| Gate | Result |
| --- | --- |
| Concrete `FulfillmentInventoryGateway` overrides the keyed interface method and calls the Inventory port keyed overload | **PASS** — `FulfillmentInventoryGateway.ConsumeReservationAsync(Guid, string, ct)` → `IFulfillmentInventoryLifecyclePort.ConsumeAsync(reservationId, consumerKey, ct)` |
| Narrow search for other implementations using the keyed member | `RecordingInventory` and `ThrowingKeyedInventory` (this module's tests) and `LegacyUnkeyedOnlyInventory` (new regression double) — the latter deliberately does **not** override it. No production implementation besides `FulfillmentInventoryGateway`. No sweeping refactor performed. |
| One focused test command | `dotnet test ... --filter "FullyQualifiedName~FulfillmentCharacterizationTests"` — 6/6 passed |
| One focused build command | `dotnet build Tooba.Fulfillment.Infrastructure` — Build succeeded, 0 Warning(s), 0 Error(s) |

No complete Host suite, no AMSC, no broad repo tests, no retries, no multi-run loops.

## Files touched (one production + one test)

| File | Change |
| --- | --- |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Fulfillments/Ports/IFulfillmentInventoryGateway.cs` | default keyed member throws `NotSupportedException` (fail-closed) + corrected 111-char comment |
| `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/FulfillmentCharacterizationTests.cs` | added `Legacy_gateway_without_keyed_implementation_fails_closed` + `LegacyUnkeyedOnlyInventory` double |
| `docs/ai/tasks/TB-P0-KEYED-CONSUME-FAIL-CLOSED-004R.task.md` | task receipt |
| `docs/architecture/evidence/TB-P0-KEYED-CONSUME-FAIL-CLOSED-004R/summary.md` | this evidence file |

## Verification commands actually run

```text
$ dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj \
    --filter "FullyQualifiedName~FulfillmentCharacterizationTests"
  Passed Tooba.Fulfillment.Tests.Behavior.FulfillmentCharacterizationTests.Dispatch_consumes_reservation_via_inventory_gateway
  Passed Tooba.Fulfillment.Tests.Behavior.FulfillmentCharacterizationTests.Cancel_gate_reports_HasDispatchedQuantity
  Passed Tooba.Fulfillment.Tests.Behavior.FulfillmentCharacterizationTests.Legacy_gateway_without_keyed_implementation_fails_closed
  Passed Tooba.Fulfillment.Tests.Behavior.FulfillmentCharacterizationTests.Consolidated_package_dispatch_propagates_keyed_consume_failure
  Passed Tooba.Fulfillment.Tests.Behavior.FulfillmentCharacterizationTests.Consolidated_package_dispatch_forwards_deterministic_consumer_key
  Passed Tooba.Fulfillment.Tests.Behavior.FulfillmentCharacterizationTests.CreateFromPaid_is_idempotent_on_event_inbox

$ dotnet build src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Tooba.Fulfillment.Infrastructure.csproj
  Build succeeded.  0 Warning(s)  0 Error(s)
```

The new regression test proves both layers of fail-closed behaviour:

1. **Direct seam**: invoking the keyed member on a legacy double (no override) throws
   `NotSupportedException` and its unkeyed method records **zero** calls.
2. **End-to-end dispatch**: `DispatchConsolidatedPackageAsync` on a package whose gateway lacks the
   keyed capability fails with `NotSupportedException`, the unkeyed path is never used, and no
   member fulfillment is persisted as `Dispatched` (both stay `Packed`).

Production gateway keyed forwarding and deterministic key generation remain covered by the two
existing 004 tests, which stay green in the same run.

## Rollback

Revert this single commit (or restore `IFulfillmentInventoryGateway.cs` to the 004 default member
and drop the new test). The correction touches no schema, migration, journal, package routing, W6
behaviour, guard, baseline or config, so rollback is a pure source revert with no data migration.

## Out of scope (unchanged, by instruction)

Inventory schema/port/journal, Fulfillment package routing, W6 held/consumed reconciliation,
locked guards, baselines, config. No W6 change, no new workflow.

## Remaining P0 risks

* R1 — Cross-module atomicity is still not claimed; the Fulfillment commit and the Inventory
  consume remain two transactions. This task only makes the Inventory side replay-safe.
* R2 — The key is only as stable as the member shipment identity (cancel + re-create shipment
  yields a new key, which is the correct semantic).
* R3 — Journal-003's migration is still undeployed in the live Host, so this wiring stays dormant
  until the Architect authorizes that deployment.
