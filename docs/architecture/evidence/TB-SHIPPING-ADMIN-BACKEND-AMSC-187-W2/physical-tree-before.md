# Physical tree — BEFORE (base `a41e4274789848fc89ce7895dcce597d8bc997a7`)

Produced from `git ls-tree -r HEAD --name-only src/backend/Modules/Shipping` (109 tracked
`.cs` / `.csproj` / `.resx` paths; generated `bin/`/`obj/` excluded).

```text
Tooba.Shipping.Contracts/
  Admin/ShippingAdminContracts.cs
  Admin/ShippingMethodFinancialStatusContracts.cs
  Errors/ShippingErrorCodes.cs
  Operations/ShippingOperationalQuoteContracts.cs
  Packaging/ShippingPackagingContracts.cs
  Rates/ShippingRatePolicyContracts.cs
  Scheduling/ShippingScheduleContracts.cs
Tooba.Shipping.Domain/
  Aggregates/ShippingCoverageGrant.cs
  Aggregates/ShippingMethodCoverage.cs
  Aggregates/ShippingPackagingPolicy.cs
  Aggregates/ShippingRatePolicy.cs
  Operations/ShippingOperationalQuoteEvaluator.cs
  Packaging/ShippingPackagingFeeResolver.cs
  Rates/ShippingRatePolicyEngine.cs
  Scheduling/ShippingOperatingSchedule.cs
  Scheduling/ShippingOperatingWindow.cs
  Scheduling/ShippingScheduleAvailabilityEvaluator.cs
Tooba.Shipping.Application/
  Operations/Queries/ShippingOperationalQuoteQueries.cs
  Packaging/Commands/ShippingPackagingPolicyCommands.cs
  Packaging/Ports/IShippingPackagingPolicyDirectory.cs
  Packaging/Validation/ShippingPackagingValidators.cs
  Rates/Commands/ShippingRateCoverageCommands.cs
  Rates/Ports/IShippingDestinationScopeValidator.cs
  Rates/Ports/IShippingMethodCoverageDirectory.cs
  Rates/Ports/IShippingMethodOptionValidator.cs
  Rates/Ports/IShippingRateCoverageDirectory.cs
  Rates/Queries/GetShippingMethodFinancialStatusQuery.cs
  Rates/ReadModels/ShippingMethodFinancialStatusEvaluator.cs
  Scheduling/Commands/ShippingScheduleCommands.cs
  Scheduling/Ports/IShippingOperatingScheduleDirectory.cs
  Validation/ShippingAdminValidators.cs
  Validation/ShippingOperationalValidators.cs
  Validation/ShippingScheduleValidators.cs
Tooba.Shipping.Infrastructure/
  DependencyInjection/ShippingModule.cs
  Operations/ShippingOperationalQuoteLookup.cs
  Packaging/ShippingPackagingFeeLookup.cs
  Packaging/ShippingPackagingPolicyDirectory.cs
  Packaging/ShippingShipmentCostLookup.cs
  Persistence/Migrations/20261014144742_InitialShipping.cs (+ .Designer.cs)
  Persistence/Migrations/20261015030159_OptionLevelRatePolicy.cs (+ .Designer.cs)
  Persistence/Migrations/20261015045130_OperatingSchedule.cs (+ .Designer.cs)
  Persistence/Migrations/20261015063838_WeightAndCountRateTiers.cs (+ .Designer.cs)
  Persistence/Migrations/20261015071836_ShippingPackagingPolicy.cs (+ .Designer.cs)
  Persistence/Migrations/20261016211322_OptionScopedCoverageRestriction.cs (+ .Designer.cs)
  Persistence/Migrations/ShippingDbContextModelSnapshot.cs
  Persistence/ShippingDbContext.cs
  Rates/FulfillmentShippingMethodOptionValidator.cs
  Rates/GeographyDestinationScopeValidator.cs
  Rates/ShippingRateCoverageDirectory.cs
  Rates/ShippingRateQuoteLookup.cs
  Scheduling/ShippingOperatingScheduleDirectory.cs
Tooba.Shipping.Endpoints/
  Admin/ShippingAdminAuthorizer.cs
  Admin/ShippingMethodFinancialStatusEndpoints.cs
  Admin/ShippingOperatingScheduleEndpoints.cs
  Admin/ShippingOperationalQuoteEndpoints.cs
  Admin/ShippingPackagingPolicyEndpoints.cs
  Admin/ShippingRateCoverageEndpoints.cs        <-- ALSO held `ShippingEndpointModule`
  Errors/ShippingErrorCatalogContributor.cs
  Resources/ShippingErrorResources.cs
  Resources/ShippingErrors.resx
  Resources/ShippingErrors.fa.resx
Tooba.Shipping.Tests/  (Admin/, Compatibility/, Composition/, Operations/, Packaging/, Persistence/, Rates/, Scheduling/)
```

## Defect proven in the BEFORE state

`src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingRateCoverageEndpoints.cs`
declared two public types with different responsibilities:

1. `ShippingRateCoverageEndpoints` — the 7 admin rate/coverage route handlers (capability code), and
2. `ShippingEndpointModule` — the module **composition root** (`MapShippingEndpoints`,
   `AddShippingEndpointPresentation`), which belongs at the Endpoints project root per
   `ENDPOINTS_CAPABILITY_FOLDERS`.

Consequence: the Endpoints project root contained **no** `.cs` file, so the canonical
`*EndpointModule.cs` composition entry did not exist at its canonical path. Everything else
(Application capability-first tree, Infrastructure `DependencyInjection/ShippingModule.cs`,
Contracts/Domain capability folders, `/Modules/Shipping/` solution group) was already canonical.
