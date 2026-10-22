# Focused validation (actually run)

All commands were bounded (each finished in ≤12 s of wall clock); no full solution build, no whole
Shipping suite rerun, no browser/server/DB/container start, no shared-cache or `.next` cleanup.

| # | Command | Result |
| --- | --- | --- |
| 1 | `dotnet build src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Tooba.Shipping.Endpoints.csproj -v q` | **succeeded**, 0 warnings, 0 errors, 2.72 s (after the extraction) |
| 2 | `dotnet build src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj -v q` | **succeeded**, 0 errors, 1 pre-existing warning (`xUnit1031` in `ShippingRateCoverageAdminTests.cs:144`, unrelated to W2) |
| 3 | `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -p:ArtifactsPath=$env:TEMP\tooba-w2-187-artifacts` | **succeeded**, 0 errors, 30 pre-existing warnings, 11.23 s — proves the Host composition compiles against the moved type in an **isolated, task-owned** artifacts directory (the locked running Host's `bin/` was never touched or stopped) |
| 4 | `dotnet test ... --no-build --filter "…ShippingAdminEndpointOwnershipTests\|…ShippingErrorCatalogAndResourcesTests\|…ShippingPackagingEndpointOwnershipTests\|…ShippingOperatingScheduleEndpointOwnershipTests\|…ShippingOperationalQuoteEndpointOwnershipTests\|…ShippingMethodFinancialAuthorityTests\|…ShippingAdminAuthorizerContextTests\|…ShippingModuleCompositionGuardTests\|…ShippingModuleStructureGuardTests"` | **Passed: 74, Failed: 0, Skipped: 0** |
| 5 | `dotnet test ... --filter "FullyQualifiedName~ShippingModuleStructureGuardTests"` | **Passed: 7, Failed: 0** (the new durable structure guard) |
| 6 | `node -e "require('…/tmar-module-structure-manifests.json')"` + manifest assertions | JSON parses; `modules` = 33; Shipping `structureCertified === false`; 5 declared projects |

## What these checks prove

* The moved composition type resolves from its canonical root path and namespace at compile time, and
  the production Host composition path (`builder.Services.AddShippingEndpointPresentation()`,
  `app.MapShippingEndpoints()`) still compiles with a single added `using`.
* Production DI/resource resolution is unchanged: `ShippingErrorCatalogAndResourcesTests` builds the
  real provider, resolves `IErrorDefinitionCatalog` and confirms all 44 `shipping.*` codes are
  registered exactly once by the single contributor/resource set now declared in
  `ShippingEndpointModule.cs`.
* Route ownership, permission usage, request/validator discovery and the authorizer context matrix are
  unchanged (the source-pin tests were pointed at the authoritative file, assertions kept equivalent).

## What was deliberately NOT run

* Full solution build / full Shipping suite: the known pre-existing
  `OperationalQuoteCompatibilityTests` failure (three Order consumers) is **not** a W2 regression and
  the task forbids repairing or re-baselining it here; no need to reproduce it.
* No DB, migration, container, package or browser operation. No schema/data/flag change.
* The pre-existing untracked `src/backend/Host/Tooba.Host.Tests/bin/Debug/**` artifacts in the
  workspace were left untouched and are not part of this change.

## Known environment constraint carried forward (honest)

W1 reported that a locked running Host prevented a normal in-place full link. W2 did not stop or
restart it; the isolated `ArtifactsPath` build above is the substitute and it succeeded, so no check is
reported as unavailable.
