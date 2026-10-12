# TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003 — migration safety

Migration: `20261012110133_AddReservationConsumptions`
Project: `Tooba.Inventory.Infrastructure` · Context: `InventoryDbContext` · Schema: `inventory`

## 1. What the migration does

```sql
CREATE TABLE inventory.reservation_consumptions (
    reservation_id uuid          NOT NULL,
    stock_item_id  uuid          NOT NULL,
    quantity       numeric(18,6) NOT NULL,
    consumer_key   character varying(128) NULL,
    consumed_at    timestamp with time zone NOT NULL,
    CONSTRAINT pk_reservation_consumptions PRIMARY KEY (reservation_id)
);
```

`Down` = `DROP TABLE inventory.reservation_consumptions;` only.

* Purely **additive**: one new table, one new PK constraint. No `ALTER`, no `UPDATE`, no `DELETE`,
  no backfill, no data movement, no rewrite of any existing table or row.
* Historical `inventory.reservations` rows in state `Consumed` are **left exactly as they are**.
  They have no journal row, and the code treats a missing journal row as *unprovable* (typed
  conflict on the keyed path) — never as replay. No provenance is invented.
* `numeric(18,6)` matches the precision/scale already used for `stock_positions.on_hand/reserved`,
  `reservations.quantity` and `return_restock_inbox.quantity` (migration
  `20260909130400_DecimalInventoryQuantity`), so the journal quantity is exactly comparable with the
  reservation quantity (`journal.Quantity == reservation.Quantity` is part of replay detection).
* `consumer_key` is nullable on purpose: legacy/unkeyed callers must not be forced to invent an
  ownership key ("Existing legacy callers cannot be silently changed to fake ownership").

## 2. Generated-migration drift that was deliberately REMOVED

`dotnet ef migrations add` also scaffolded three operations that are unrelated to this task and come
from a **pre-existing** model/snapshot drift in the module (`return_restock_inbox` was created by an
older hand-written migration, so its PK/index names and its column type do not match what the current
model would generate):

| Scaffolded operation | Why removed |
|---|---|
| `AlterColumn return_restock_inbox.quantity` from `numeric(18,6)` → `numeric` | Would **drop the precision/scale** of a historical column — data-shape rewrite, out of scope, and a real regression risk. |
| `DropPrimaryKey("PK_return_restock_inbox")` + `AddPrimaryKey("pk_return_restock_inbox")` | Renames a constraint on a historical table; unrelated to Inventory consumption. |
| `RenameIndex("IX_return_restock_inbox_reservation_id" → "ix_return_restock_inbox_reservation_id")` | Same. |

The migration and the `ModelSnapshot.cs` therefore contain **only** the additive journal change; the
snapshot diff is exactly the 33-line entity block for `ReservationConsumptionRecord`.

Note for reviewers: because that pre-existing drift is untouched, `dotnet ef migrations
has-pending-model-changes` reports pending changes for the module (it did before this task as well —
the drift items above). This is a pre-existing condition, deliberately not "fixed" here because
fixing it would mean rewriting a historical table inside an Inventory-consumption task.

## 3. Migration convention compliance

* Placed in `Persistence/Migrations/` with the module's timestamp-name convention.
* This repo has two accepted shapes for migration files: older ones pair `X.cs` with an
  `X.Designer.cs` carrying `[DbContext]`/`[Migration]`, the newest one
  (`20260909130400_DecimalInventoryQuantity`) inlines those attributes and ships **no** Designer.
  This migration follows the newest shape (attributes inlined, no Designer), so EF discovers it —
  verified by the fact that the integration tests failed with `42P01: relation
  "inventory.reservation_consumptions" does not exist` until the attributes were inlined, and pass
  afterwards (see `focused-test-results.txt`).
* No migration was executed against a live server, no live data was mutated, no deploy/restart.

## 4. Runtime application proof

Every new integration test runs `inventory.Database.MigrateAsync()` against a fresh
`postgres:16-alpine` Testcontainer before acting, then asserts against the created table and its PK
constraint (`pg_constraint.contype='p'` → `pk_reservation_consumptions`) and receives a real
`23505 unique_violation` (`PostgresErrorCodes.UniqueViolation`) when inserting a duplicate journal
row. The 11-test run is green: see `focused-test-results.txt` /
`raw-inventory-consumption-journal-tests.txt`.

## 5. Deployment note (not executed here)

The migration is repository-ready but **not applied** to any environment. When an authorised release
does apply it, the statement is a single `CREATE TABLE` on an empty table: no lock escalation on
existing inventory tables, no backfill window, and the application code keeps working with legacy
`Consumed` rows because a missing journal row is an explicit typed conflict rather than an implicit
success.
