# SHIPPING-ONE-LEVEL-UI-082 — Fulfillment one-level catalog foundation

Task: `TB-SHIPPING-ONE-LEVEL-TABS-082` (parent `TB-SHIPPING-ONE-LEVEL-TABS-081`)
Base SHA: `0d83659238f9a1b668c6f61bb7d505192a2effc7`
Scope: Fulfillment only, additive. No frontend, no Host, no Order/Shipping/Geography/Payment/
Settlement/Checkout change. No live database mutation.

## 1. What was built

A new, additive, Fulfillment-owned **one-level shipping method catalog** that sits beside the existing
two-level `shipping_services` / `shipping_service_options` catalog. The legacy catalog is untouched and
remains the only thing the legacy Checkout path reads.

| Layer | Path |
| --- | --- |
| Domain aggregate | `Tooba.Fulfillment.Domain/Aggregates/ShippingMethod.cs` |
| Domain translation | `Tooba.Fulfillment.Domain/Aggregates/ShippingMethodTranslation.cs` |
| EF mapping | `Tooba.Fulfillment.Infrastructure/Persistence/FulfillmentDbContext.cs` (`shipping_methods`, `shipping_method_translations`) |
| Migration (additive, reversible) | `.../Persistence/Migrations/20261015120000_AddShippingOneLevelCatalog.cs` |
| Contracts (read + gate) | `Tooba.Fulfillment.Contracts/Shipping/ShippingOneLevelCatalogContracts.cs` |
| Reader (read-only) | `Tooba.Fulfillment.Infrastructure/Shipping/ShippingOneLevelCatalogReader.cs` |
| Backfill planner (pure) | `Tooba.Fulfillment.Application/Shipping/ShippingMethodBackfillPlanner.cs` |
| DI | `Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs` |
| Stable codes + localization + descriptors | `FulfillmentErrorCodes`, `FulfillmentErrorCatalogContributor`, both `FulfillmentErrors*.resx` |
| Focused tests | `Tooba.Fulfillment.Tests/Behavior/ShippingOneLevelMethodCatalogTests.cs` |

## 2. Decision B1 — identity model

Each merchant-visible method is a row with:

- `method_code` — **stable public code**, `varchar(32)`, unique, case-normalized to lower-case;
- `provider_kind` — **hidden internal metadata** (never a second merchant configuration level);
- `legacy_service_code` + nullable `legacy_option_code` — **immutable** compatibility binding to the
  existing two-level catalog;
- `sort_order`, `is_active`, `created_at`, `updated_at`;
- localized `name`/`description` rows.

Identity is **never** keyed on the option UUID: `ShippingServiceDirectory.UpdateAsync` deletes and
re-creates option rows, so their IDs are not stable. `ShippingMethod.Update` intentionally exposes only
`sortOrder` + `isActive` — the code and the legacy binding cannot be mutated.

The uniqueness rules encode the architectural intent:

- `ix_shipping_methods_method_code` — unique on `method_code` (case-normalized in the aggregate, so a
  case-insensitive collision is impossible);
- `ux_shipping_methods_legacy_binding` — unique on `(legacy_service_code, legacy_option_code)`
  **`WHERE legacy_option_code IS NOT NULL`**. The partial predicate is deliberate: a service-level
  fallback policy row (`legacy_option_code IS NULL`) is *not* a second sellable method and must not
  collide with the option-bound rows of the same service.

Codes are preserved **byte-identically**: `post:standard`, `post:express`, `tipax:standard`,
`tipax:express`, `store_courier`, `in_person`, `snapp_courier`. A service that owns option rows does
**not** get a bare-service method row; only its option-bound rows are methods.

## 3. Decision B2 — 32-character width and code shape

`ShippingMethod.MaxCodeLength = 32`, matching the narrowest known paid column
(`checkouts.shipping_method_code varchar(32)`). **No** order/checkout table was widened and **no**
historical cart/order/shipment code was rewritten.

Shape rules (`ShippingMethod.TryNormalizeCode`, mirrored by the DB `CHECK` constraints):

- length 1..32 after trimming and lower-casing;
- at most one `:` separator (the legacy composite form);
- every segment `[a-z0-9_]+` — so `-`, ` `, `/`, `.`, `::`, leading/trailing `:` and triple composites
  are all rejected rather than silently rewritten;
- `Create` throws a typed `SemanticException` on an invalid code instead of normalizing it into
  something else.

## 4. Backfill / mapping (preservation-safe, idempotent)

`ShippingMethodBackfillPlanner.Plan(services, existing)` is a **pure function** — no `DbContext`, no
writes — that returns an ordered plan of `Create` / `Skip` / `AlignActivation` items plus a list of
violations. It does **not** call the destructive `UpdateAsync` routine.

Rules implemented and test-proven:

- every option of every service is mapped, **including inactive options** (`post:retired` in the test
  fixture): a persisted code may exist historically and must keep resolving. Inactive stays inactive.
- an **option-less service** yields exactly one method with `legacy_option_code = NULL`, carrying the
  service's own activation state (`snapp_courier` stays inactive, `store_courier`/`in_person` active).
- activation is `service.IsActive && option.IsActive` — never invented, never widened.
- re-running the planner against an already-mapped catalog yields **all `Skip`** (idempotent) and the
  same decisions on a third run (stable).
- violations reported instead of silently applied: duplicate existing code (case-insensitive),
  duplicate binding, duplicate method code, invalid/over-length code, and `binding_mismatch` when an
  existing row's legacy binding disagrees with the desired one.
- a pre-existing bare-service row (e.g. `post`) is **not** consumed or repurposed — the planner simply
  has no desired method for it and leaves it alone.

**Live Development is UNMIGRATED.** The task forbade live `tooba_alpha` mutation, so no backfill was
executed against any database; the plan was proven against in-memory fixtures only. The live rollout
procedure is deferred and recorded in §7.

## 5. Default-OFF gate

`ShippingOneLevelCatalogOptions.SectionName = "Tooba:ShippingOneLevelCatalog"` with
`Enabled = false` by default. There is **no** appsettings section, so the option binds to its safe
default and the gate evaluates `Disabled` by absence.

`ShippingOneLevelCatalogPolicy.Evaluate` is fail-closed: `null` ⇒ `Disabled`, and only an explicit
`Enabled = true` yields `Enabled`. The gate is wired in `FulfillmentModule` (Fulfillment is the correct
configuration owner) and is scoped solely to the future catalog-read/UI path. It does not read, imply or
flip `Tooba:ShippingRateAuthority` or `Tooba:ShippingOperationalShadow`; a focused test asserts the three
section names are distinct.

**No storefront behaviour changed.** No consumer was wired to the new reader; the legacy Checkout path,
paid snapshots, rate authority and operational shadow are exactly as before.

## 6. Verification actually performed

| Check | Result |
| --- | --- |
| `dotnet build` `Tooba.Fulfillment.Tests` | succeeded, 0 warnings, 0 errors |
| `dotnet test` `Tooba.Fulfillment.Tests` | **94 passed / 0 failed** (12 new focused tests) |
| Error-code counts | 91 declared constants, 89 locally registered descriptors — matches the updated durable guard |
| Host AMSC guard execution | **NOT RUN** — blocked by the running Development Host (`Tooba.Host`, pid 33548) holding its own `bin/` DLLs. Task forbids an unexpected Host restart, so the guard was not executed; its two affected expectations (migration list, code counts) were verified statically against the real files. |

The migration was **not** applied to any database, so no isolated-DB up/down evidence exists. The `Up`
is idempotent (`CREATE TABLE IF NOT EXISTS` / `CREATE … IF NOT EXISTS`) and the `Down` drops exactly the
two new tables.

### Known gap (documented, not hidden)

`FulfillmentDbContextModelSnapshot.cs` was **not** extended. That matches the pre-existing repository
state, where the snapshot already omits `shipping_services` / `shipping_service_options` (the
`20260910170000_AddShippingServiceCatalog` migration is also raw SQL with no snapshot delta). Consequence:
the next EF-generated migration for this DbContext must be authored/verified by hand rather than purely
scaffolded. Fixing the stale snapshot is out of 082 scope.

## 7. Rollout / rollback

**Rollout (later authorization required).** The gate stays OFF, so nothing is read yet. A later task may
(a) apply `20261015120000_AddShippingOneLevelCatalog` to a non-production database, (b) execute the
backfill plan from `ShippingMethodBackfillPlanner` as an explicit, reviewed write step, and only then
(c) wire a reader consumer behind the gate.

**Rollback.** Gate OFF restores the pre-task read path with zero data change. The schema rollback is the
single migration `Down` (drops `shipping_method_translations` then `shipping_methods`); no other module
references these tables, so dropping them cannot affect Order, Shipping, Geography, Payment or
Settlement.

## 8. Guardrails honoured

- Legacy `shipping_services` / `shipping_service_options` rows: not removed, renamed, or rewritten.
- Legacy `(serviceCode, optionCode?)` key retained internally; the nullable option fallback is preserved.
- No frontend, Host, Order, Shipping, Geography, Payment, Settlement, Checkout, pricing or paid-snapshot
  change.
- `Tooba:ShippingRateAuthority` and `Tooba:ShippingOperationalShadow` remain OFF.
- Capacity and COD are untouched (neither is a reservation or a settlement in this task).
- No blanket test suite run; only the focused Fulfillment project.

## 9. Dependency for 083

Task 083 (next in the `081` roadmap) may now build on: the persisted one-level table, the stable
≤32-character code contract, the read-only `IShippingOneLevelCatalogReader`, and the default-OFF
`Tooba:ShippingOneLevelCatalog` gate. It must **not** enable the gate, must **not** touch
`Tooba:ShippingRateAuthority` / `Tooba:ShippingOperationalShadow`, and must **not** widen
`checkouts.shipping_method_code`.

Two open items still require an explicit Architect ruling before 083 goes wide:

1. whether the legacy catalog remains dual-written once the one-level catalog becomes authoritative;
2. the live backfill authorization and its exact execution window.
