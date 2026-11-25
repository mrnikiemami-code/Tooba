# R10-03 Inventory provisioning — backend slice report

Task: TB-296-R10-OFFER-PRODUCTION-READINESS / R10-03 (requested backend-only slice)
Branch: `opencode/iso-agents-002`
Repository HEAD (base, unchanged): `571d187cbe9850b5c59576d3b3e3f91fade76807`
Working tree: NOT committed (per instruction).
Date: 2026-11-26 (Tehran)

## Scope implemented

- Inventory-owned eligible active-location read.
- Idempotent StockPosition provisioning for an already persisted Offer.
- No fake StockItemId, no implicit warehouse creation, no total-stock mutation.
- Provisioned position opens with `OnHand=0` / `Reserved=0`; setting physical stock remains the
  existing `InventoryDirectory.AdjustAsync` path (untouched).
- Frontend, channel, history, migrations, Host business logic and other modules were not authored here.

## Files added/changed by this slice

- `src/backend/Modules/Inventory/Tooba.Inventory.Contracts/Seller/SellerOfferInventoryContracts.cs`
  - `InventoryEligibleLocation(Guid LocationId, string Code, string Name)`
  - `ProvisionedStockPosition(Guid StockItemId, Guid LocationId, Guid OfferId, decimal OnHand, decimal Reserved)`
  - `IInventoryProvisioningGateway` with `ListEligibleLocationsAsync` and `EnsurePositionAsync`
- `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryProvisioningDirectory.cs`
  - Active-only, `OrderBy(Code)` eligible read via `InventoryDbContext`.
  - `EnsurePositionAsync`: rejects empty ids; requires location exists+Active; returns existing
    position for `(OfferId, LocationId)`; otherwise `StockPosition.Open(ids.Next(), ...)` + `SaveChanges`.
  - Unique-index race (`ix_stock_positions_offer_id_location_id`, PostgreSQL `23505`) is caught,
    the losing entity is detached and the winning row is re-read and returned.
- `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/DependencyInjection/InventoryModule.cs`
  - `services.AddScoped<IInventoryProvisioningGateway, InventoryProvisioningDirectory>();`
    next to the existing `ISellerOfferInventoryGateway` registration.
- `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Queries/ListEligibleInventoryLocations/ListEligibleInventoryLocationsQuery.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/EnsureOfferInventoryPosition/EnsureOfferInventoryPositionCommand.cs`
  - Injects `IInventoryProvisioningGateway` + `IOfferStore`; seller scope requires offer ownership,
    admin scope (`SellerPartyId = null`) may target any persisted offer; known Inventory contract
    faults are mapped to a typed `Result` failure.
- `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/EnsureOfferInventoryPosition/EnsureOfferInventoryPositionCommandValidator.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Validation/OfferValidationCodes.cs`
  - added `offer.validation.location_id_required`.
- `src/backend/Modules/Offer/Tooba.Offer.Contracts/Dtos/SellerOfferPanelDtos.cs`
  - added `OfferInventoryPositionProvisionRequest(Guid LocationId)`.
- `src/backend/Modules/Offer/Tooba.Offer.Endpoints/Admin/OfferInventoryProvisioningAdminEndpoints.cs`
  - admin GET `/v1/admin/offers/inventory-locations` (view)
  - admin POST `/v1/admin/offers/{offerId:guid}/inventory/positions` (edit; body `{ locationId }`)
  - seller GET `/v1/seller/offers/inventory-locations` (edit)
  - seller POST `/v1/seller/offers/{offerId:guid}/inventory/positions` (edit; seller scope from auth)
- `src/backend/Modules/Offer/Tooba.Offer.Endpoints/OfferEndpointModule.cs`
  - one registration line `Admin.OfferInventoryProvisioningAdminEndpoints.Map(app);`
- `src/backend/Modules/Offer/Tooba.Offer.Tests/Application/OfferInventoryProvisioningHandlerTests.cs`
  - idempotent same offer/location via fake gateway, seller ownership rejection, admin scope,
    typed Inventory failure mapping, eligible-location projection, and source-verified active filter/order.
- `src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferEndpointValidatorCoverageGuardTests.cs`
  - manifest includes the two new endpoint-reachable requests; `EnsureOfferInventoryPositionCommand`
    is `VALIDATOR_REQUIRED`, `ListEligibleInventoryLocationsQuery` is auth-scoped with no input.

## Commands and results

1. `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore -v minimal`
   - Result: `Build succeeded. 0 Warning(s) 0 Error(s)` (exit 0)
   - Log: `evidence/r10-03-build.log`

2. `dotnet test src/backend/Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj --no-restore --filter "FullyQualifiedName~OfferInventoryProvisioningHandlerTests|FullyQualifiedName~OfferEndpointValidatorCoverageGuardTests|FullyQualifiedName~OfferArchitectureGuardTests|FullyQualifiedName~OfferPhysicalStructureGuardTests|FullyQualifiedName~OfferManifestDiskReconciliationGuardTests|FullyQualifiedName~OfferEndpointModuleTests"`
   - Result: `Passed! - Failed: 0, Passed: 57, Skipped: 0, Total: 57` (exit 0)
   - Log: `evidence/r10-03-offer-tests.log`

3. `dotnet restore src/backend/Modules/Inventory/Tooba.Inventory.Tests/Tooba.Inventory.Tests.csproj`
   - Needed because the test project had no `project.assets.json` in this worktree.
4. `dotnet test src/backend/Modules/Inventory/Tooba.Inventory.Tests/Tooba.Inventory.Tests.csproj --no-restore --filter "FullyQualifiedName~Inventory.Tests.Architecture"`
   - Result: `Failed: 2, Passed: 20, Skipped: 0, Total: 22`.
   - Both failures are pre-existing baseline drift at HEAD and are unrelated to this slice (verified with
     `git show HEAD:...`):
     - `InventoryAmcW1MigrateGuardTests.Every_declared_inventory_code_is_catalogued_and_localized`
       expects `InventoryErrorCodes.HeldReleaseIntentConflict` in the catalog contributor/resources;
       HEAD's contributor/resources do not contain it.
     - `InventoryArchitectureGuardTests.Inventory_golden_boundaries_and_physical_layout_remain_clean`
       hits `AllowedContractsFolders` missing `Replacement`; the `Replacement` contracts folder exists at HEAD.
   - Log: `evidence/r10-03-inventory-tests.log`

## Environment/identity notes

- Existing worktree at this time also contains unrelated in-progress **R10-01 channel** edits
  (`OfferChannelPolicy`, `GetAllowedOfferChannels`, `OfferChannelRequestResolver`, channel DTO/catalog
  additions). Those files were authored by a concurrent worker, not by this R10-03 slice, and were left
  untouched apart from the shared `OfferEndpointValidatorCoverageGuardTests` manifest, which already
  reconciled both requests (14 entries) when re-verified.
- No migration was created or applied. No live database was written. No Host process was started for
  this slice. No commit was made.
- Connected HTTP/PostgreSQL persistence proof is `NOT_RUN` for this slice by explicit instruction
  ("Do not apply migrations or write live DB"); the idempotency uniqueness proof is source/contract
  verified here, and DB-level proof remains for the R10 connected matrix.
