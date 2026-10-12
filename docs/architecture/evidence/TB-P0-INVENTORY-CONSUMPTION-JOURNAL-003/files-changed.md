# TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003 — files changed

All changes are inside Inventory ownership (plus one Host test file). No Fulfillment, Order,
Host production, UI, auth or tenant file was modified.

## Production (Inventory module only)

| File | Change |
|---|---|
| `src/backend/Modules/Inventory/Tooba.Inventory.Contracts/Fulfillment/FulfillmentInventoryLifecyclePort.cs` | Additive member `ConsumeAsync(Guid reservationId, string consumerKey, CancellationToken)`; the existing unkeyed member and its strict contract are unchanged and documented as such. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Persistence/ReservationConsumptionRecord.cs` | **New.** Durable consumption journal entity (PK `ReservationId`, `StockItemId`, `Quantity numeric(18,6)`, nullable `ConsumerKey varchar(128)`, `ConsumedAt`). |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Persistence/InventoryDbContext.cs` | New `DbSet<ReservationConsumptionRecord> ReservationConsumptions` + mapping (table `reservation_consumptions`, PK `ReservationId`, `numeric(18,6)`, `ConsumerKey` ≤128). |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Persistence/Migrations/20261012110133_AddReservationConsumptions.cs` | **New.** Creates `inventory.reservation_consumptions` only; `Down` drops it. No backfill, no touch of historical rows. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Persistence/Migrations/InventoryDbContextModelSnapshot.cs` | Adds only the `ReservationConsumptionRecord` entity block (33 lines). Nothing else in the snapshot is edited. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.cs` | `ConsumeAsync(Guid)` becomes a thin legacy entry point; new keyed overload; new private `ConsumeReservationAsync` core with one local transaction, state-guarded CAS claim, journal insert, replay/conflict truth table, and constraint-conflict translation. |

## Tests

| File | Change |
|---|---|
| `src/backend/Host/Tooba.Host.Tests/InventoryConsumptionJournalTests.cs` | **New.** 11 focused PostgreSQL (Testcontainers) tests: replay, conflicting consumer, legacy `Consumed` row without journal, strict legacy double-consume, injected save failure → full rollback (incl. outbox), concurrent same/different consumers, released/unknown domain errors, insufficient reserved stock rollback, blank key, durable PK/unique constraint proof. |

## Documentation / evidence

| File | Change |
|---|---|
| `docs/ai/tasks/TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003.task.md` | Exact received task receipt. |
| `docs/architecture/evidence/TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003/*` | This evidence bundle (design, files, invariants, migration safety, tests, risks, summary). |

## Explicitly NOT changed

* `FulfillmentInventoryGateway` / Fulfillment dispatch orchestration / W6 skip logic.
* `InventoryErrorCodes` / `InventoryErrorCatalogContributor` / `InventoryErrors*.resx`
  (see `design.md` §2.4 — adding a code would require editing a certification lock).
* Any AMSC/ARCH-COMPLETE-002 guard, manifest or `tmar-current-state.json` record.
* Any other module, Host production code, schema or migration history of another module.
