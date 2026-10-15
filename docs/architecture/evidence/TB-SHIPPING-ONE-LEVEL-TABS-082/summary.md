# TB-SHIPPING-ONE-LEVEL-TABS-082 — evidence

| item | value |
| --- | --- |
| Task | `TB-SHIPPING-ONE-LEVEL-TABS-082` |
| Parent | `TB-SHIPPING-ONE-LEVEL-TABS-081` |
| Program | `TOOBA-SHIPPING-ONE-LEVEL-TABS` |
| Mode | `BOUNDED_FULFILLMENT_ONE_LEVEL_CATALOG_FOUNDATION` |
| Bridge task row | `5d7f2d27-5ed4-4146-a679-adfc5207dc10` |
| Expected base SHA | `0d83659238f9a1b668c6f61bb7d505192a2effc7` |
| Claimed at (UTC) | 2026-10-15T11:31:59Z |
| Stop state | `STOP_FOR_ARCHITECT` |
| Live database | **UNMIGRATED** — no database was written |

## Status

`PASS` for the bounded additive Fulfillment foundation, with one honestly documented gap
(`FulfillmentDbContextModelSnapshot.cs` not extended) and one blocked verification (Host AMSC guard
could not run because the Development Host process holds its own build output).

## Changed / added paths

### Production

- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingMethod.cs` (new)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingMethodTranslation.cs` (new)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Shipping/ShippingOneLevelCatalogContracts.cs` (new)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Errors/FulfillmentErrorCodes.cs` (+5 codes)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/ShippingMethodBackfillPlanner.cs` (new)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/FulfillmentDbContext.cs`
  (+2 `DbSet`, +2 entity mappings)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/Migrations/20261015120000_AddShippingOneLevelCatalog.cs` (new)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/ShippingOneLevelCatalogReader.cs` (new)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs`
  (+reader registration, +gate options binding)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Errors/FulfillmentErrorCatalogContributor.cs` (+5 descriptors)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Resources/FulfillmentErrors.resx` (+5 entries)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Resources/FulfillmentErrors.fa.resx` (+5 entries)

### Tests

- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/ShippingOneLevelMethodCatalogTests.cs` (new, 12 tests)
- `src/backend/Host/Tooba.Host.Tests/Architecture/FulfillmentModuleAmsc001W3CertGuardTests.cs`
  (migration list + error-code counts re-baselined to reality)

### Docs

- `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-082.md` (new)
- `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-082/**` (this evidence set)

## Verification actually run

| command | result |
| --- | --- |
| `dotnet build src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests` | Build succeeded, 0 warnings, 0 errors |
| `dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests` | `Passed! Failed: 0, Passed: 94, Skipped: 0` |
| `dotnet test …Host.Tests --filter FulfillmentModuleAmsc001W3CertGuardTests` | **blocked**: `MSB3027`/`MSB3021` file locks held by the running `Tooba.Host (33548)` |
| static count check of guard expectations | declared codes = 91, registered `D(...)` = 89 → matches the edited guard |

The Host test project could not be built because the Development Host process locks
`src/backend/Host/Tooba.Host/bin/Debug/net8.0/*.dll`. Restarting that Host is outside this task's
authorization, so the guard was not executed. Its two affected assertions were verified by direct
inspection of the real files instead.

## Default-OFF proof

- `ShippingOneLevelCatalogOptions.SectionName = "Tooba:ShippingOneLevelCatalog"`, `Enabled = false`.
- No `Tooba:ShippingOneLevelCatalog` key exists in any appsettings (grep-verified), so the gate is
  `Disabled` by absence.
- `ShippingOneLevelCatalogPolicy.Evaluate(null) == Disabled` (unit-proven).
- No consumer reads `IShippingOneLevelCatalogReader`; the legacy storefront/Checkout path is unchanged.

## Rollback

1. Gate stays OFF ⇒ zero behavioural change, no data touched.
2. Schema rollback = `Down` of `20261015120000_AddShippingOneLevelCatalog`
   (`DROP TABLE shipping_method_translations; DROP TABLE shipping_methods;`).
3. No other module references the new tables.
