# SHIPPING ONE-LEVEL UI — TB-SHIPPING-ONE-LEVEL-TABS-141

> **STATUS: PARTIAL — Mission A PROVEN (schema provenance + explicit activation + migration applicability and
> idempotency on a disposable scratch tenant DB); Mission B NOT EXECUTED because the isolated runtime the
> accepted baseline sanctions (`Edition=SingleStore`) cannot register the Settlement per-group recorder at
> all. No fabricated ledger row, no invented commercial allocation, no source change.**

Base SHA: `509010d792ee9fc9ee0e7729f92011d46205d423` (`HEAD == origin/main` at claim).
Predecessor: TB-140 (PASS) — real sandbox `payment.succeeded.v1` created 1 `FulfillmentUnit`, 2 stable
`ShippingGroupId`s, exactly 2 `FulfillmentItem`s, no automatic shipment.

## 1. Mission A — why the Settlement schema was absent (concrete evidence, no speculation)

The absence is **not** a "module not composed" defect and **not** a scratch-fixture omission. It is the
combination of two independent, deliberate properties of the Settlement module, both visible in source:

| # | Fact | Evidence |
| --- | --- | --- |
| A1 | `SettlementModule` is explicitly composed into the Host module list. | `src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs:87` (`new SettlementModule(),`) |
| A2 | `SettlementModule` registers its `SettlementDbContext` but registers **zero** `IModuleSchemaMigrator`. | `.../Settlement/DependencyInjection/SettlementModule.cs:72-83` — compare the owner-correct peer `FulfillmentModule.cs:129` (`services.AddModuleSchemaMigrator<FulfillmentDbContext>("Fulfillment", ModuleSchemaMigrationOrder.Fulfillment);`) |
| A3 | The Development Host startup auto-migration path enumerates **only** registered `IModuleSchemaMigrator`s, ordered by its `Order`. A module that registers none is therefore never migrated. | `src/backend/Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs:120-124` (`provider.GetServices<IModuleSchemaMigrator>()`), `:146-149` (`await migrator.MigrateAsync(...)`) |
| A4 | There is no `ModuleSchemaMigrationOrder.Settlement` constant at all (the canonical order table jumps `Promotion=12` → `Reviews=14`), i.e. Settlement was never part of the startup chain. | `src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs:10-72` |
| A5 | Settlement **is** migration-capable through the *explicit* authority: the production CLI registry carries a Settlement descriptor, and that CLI is unreachable from Host startup. | `src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs:64` (`Descriptor<SettlementDbContext>("Settlement", SettlementDbContext.Schema)`), and the same file's own comments (`:85-92`) show this is metadata for the explicit CLI only |
| A6 | The startup auto-migration then reads `__ef_migrations_history` inside the module's own schema (`ToobaNpgsql.ConfigureModuleContext` → `MigrationsHistoryTable("__ef_migrations_history", schema)`), so "applied" is only meaningful when the module's own seam is used. | `src/backend/BuildingBlocks/Tooba.Persistence/ToobaNpgsql.cs:33-47` |

**Conclusion (Mission A):** the missing `settlement` schema is the *expected* consequence of a documented
design boundary — Settlement (like Returns, Support, Wallet, Geography, Shipping) is migrated **only** by the
explicit authority, never by Host startup. TB-138's "no Settlement schema in isolated tenant DB" is
reproduced here exactly (`returns` is absent for the same reason). It is **not** an accidental
registration bug in `SettlementModule`: adding an `IModuleSchemaMigrator` there would make *every*
Development Host startup emit Settlement DDL against the shared `:5088` / `tooba_alpha` development store,
which the task explicitly forbids ("Do NOT globally enable a module"), and would require a new canonical
order constant (an architecture-source decision for the Architect, not a bounded repair).

### 1.1 Runtime proof — reproducible, on a disposable scratch tenant

Environment: task-owned disposable container `tooba-tb141-pg` (Postgres 16-alpine, `127.0.0.1:55462`,
`tooba_141_scratch` + `tooba_141_messaging`, role `scratch141`); isolated Host from the TB-140 publish tree
(no source rebuild), `ASPNETCORE_ENVIRONMENT=DOTNET_ENVIRONMENT=Development`, `http://127.0.0.1:5409`,
single active tenant `store-alpha` → `tenant-141-scratch`. `prepare-isolated-target-141.js` printed
`PREPARE=PASS` with `failClosed=true` (1 tenant, canonical gateway tenant present, no `tooba_alpha`, no
`tenant-alpha`, no `Port=5432`, no prior scratch port, no `ShippingRateAuthority`, no
`ShippingOperationalShadow`).

| Step | Observation | Evidence file |
| --- | --- | --- |
| Host startup auto-migration (single launch, `HEALTH_200_OBSERVED=True`) | 30 non-system schemas created: `access_control … wishlist`. **`settlement` present? 0. `returns` present? 0.** `settlement` tables = 0. | `host-launch-141.txt`, `scratch-readback-141.txt` (`PRE_ACTIVATION.*`) |
| Explicit activation (module-owned seam: `ToobaNpgsql.ConfigureModuleContext` + `Database.MigrateAsync()`) | `MIGRATIONS_APPLIED_BEFORE=[]`, `MIGRATIONS_PENDING_BEFORE=[20260827030000_InitialSettlement,20261016140000_AddSettlementShippingGroupFees]`, then both applied | `settlement-migration-activation-141.txt` |
| Idempotency (the task's explicit warning: raw DDL looking idempotent is not proof) | second `MigrateAsync()` → `SECOND_APPLY_WAS_NOOP=True`; `MIGRATIONS_APPLIED_AFTER_SECOND_APPLY` unchanged | `settlement-migration-activation-141.txt` |
| Migration version applied (canonical history table) | `settlement.__ef_migrations_history` = `20260827030000_InitialSettlement`, `20261016140000_AddSettlementShippingGroupFees` | `scratch-readback-141.txt` (`C.*`) |
| Resulting schema | `SETTLEMENT_TABLE_COUNT=12`; `settlement.shipping_group_fees` with the declared 26 columns and 6 indexes (`pk_…`, `ix_…_idempotency_key`, `ix_…_payment_group_charge` (partial, charge-only), `ix_…_payment_group`, `ix_…_shipping_group_id`, `ix_…_seller_party_id`) | `settlement-migration-activation-141.txt`, `scratch-readback-141.txt` (`D.*`, `E.*`) |
| Truthful pending-ledger shape (no invented commercial data) | identity + `customer_charge_amount` + `allocation_state` NOT NULL; **every** commercial/funding column NULLABLE and NULL, never a silent zero: `allocation_policy_version`, `allocation_policy_reference`, `carrier_payable_amount`, `seller_share_amount`, `marketplace_share_amount`, `shipping_discount_amount`, `shipping_tax_amount`, `funder_adjustment_amount`, `funder_reference` | `scratch-readback-141.txt` (`E.*`) |
| No fabricated rows | `shipping_group_fees` rows = 0, `settlement_entries` rows = 0, `payout_requests` rows = 0 | `scratch-readback-141.txt` (`F.*`, `G.*`) |

`readback-141.sql` section `B` is intentionally the post-activation count (12); the pre-activation count is
the `PRE_ACTIVATION.settlement_tables|0` line captured before the harness ran.

## 2. Mission B — `payment → settlement` customer-charge readback: NOT EXECUTED (concrete blocker)

The per-group pending charge can only be written by `SettlementDirectory.RecordShippingGroupFeesAsync`,
whose **only** production caller is `SettlementPaymentSucceededHandler` (`Handlers/SettlementEventHandlers.cs:23`).
That handler is registered **only** when `Tooba:Edition == "Marketplace"`:

```text
SettlementModule.cs:57-61
if (IsMarketplaceEdition(configuration))
{
    services.AddScoped<IIntegrationEventHandler<PaymentSucceededIntegrationEvent>, SettlementPaymentSucceededHandler>();
    services.AddScoped<IIntegrationEventHandler<RefundSucceededIntegrationEvent>, SettlementRefundSucceededHandler>();
}
```

The isolated runtime the accepted TB-138/TB-140 lineage sanctions is `Edition=SingleStore`
(`isolation-guard.json` → `"edition":"SingleStore"`), so in that environment the recorder cannot be
registered and no journey could ever produce a Settlement row — regardless of whether the schema exists.
This is proven at test level by the module's own composition guard:
`dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~Marketplace_registers_settlement_payment_and_refund_handlers_singlestore_does_not`
→ **Passed 1/1** (`focused-test-settlement-edition-gate.txt`).

Activating `Edition=Marketplace` in the scratch overlay was deliberately **not** invented: Marketplace is a
different deployment shape (`ControlPlaneRegistry` then takes its connection from
`Tooba:Marketplace:ConnectionReference` and parses **no** tenant records —
`Configuration/PlatformOptionsValidator.cs:272-279, 366-368`), and TB-130's own proof of this path
(`MarketplaceSettlementEventPathTests`) constructs `SettlementDirectory`/the handler directly in a test
fixture, i.e. it never proved the full Marketplace *runtime* journey. Inventing that environment inside a
45-minute box would be new environment policy, not a bounded repair.

**Explicitly UNPROVEN (not claimed as PASS):**
- no `settlement.shipping_group_fees` row was ever produced by a real paid journey;
- the "TWO positive rows 200000 + 180000 IRR, matched to stable `ShippingGroupId`s, `PendingAllocation`"
  invariant remains **UNPROVEN** at runtime;
- the MD5 `IX_…_payment_group_charge` partial-unique constraint is proven to exist, but its behaviour under a
  real duplicate paid delivery is unproven.

Preserved from TB-140: the Fulfillment durable success is untouched (no Fulfillment source, migration,
test or evidence change in this task; the scratch run's `fulfillment` schema was created by the same
unchanged startup path).

## 3. Non-interference and cleanup

- Shared `:5088` / `:3000`, `tooba_alpha`, shared Postgres `:5432` and shared RabbitMQ were never started,
  stopped, re-pointed or written; no credential was printed or committed.
- Only one isolated Host launch (`LAUNCH_COUNT=1`), one scratch container, two harness runs; no broad suites
  (single focused filter), no retry loop.
- Cleanup: isolated Host PID terminated (PID-verified), container `tooba-tb141-pg` removed, `.tmp-141-run`
  (holds the scratch-only overlay) deleted. Rollback for the scratch DB is `docker rm -f tooba-tb141-pg`.
- No source, contract, migration, migration-order, module-registration, appsettings, frontend, CSS, theme,
  token, typography, RTL, shipping-card or admin change. No commercial allocation, no `Settled`/`Revenue`/
  `Credit`/`Payable` transition, no seller payout row.

## 4. First remaining blocker (for the Architect)

Decide the **activation contract** for Settlement before any further paid-journey acceptance:

1. keep Settlement CLI-only (status quo) — then a runtime settlement readback requires a sanctioned
   explicit-activation step *and* `Edition=Marketplace` in the disposable environment; or
2. add `Settlement` to the canonical startup chain (`ModuleSchemaMigrationOrder` constant +
   `AddModuleSchemaMigrator<SettlementDbContext>` in `SettlementModule`) — an architecture-source decision
   that makes every Development Host startup emit Settlement DDL, including against shared `tooba_alpha`.

Until that decision, TB-130's `shipping_group_fees` ledger stays proven only by focused tests plus this
schema-level runtime proof, and the "two positive pending groups per paid marketplace checkout" invariant
cannot be demonstrated on the SingleStore isolated environment.
