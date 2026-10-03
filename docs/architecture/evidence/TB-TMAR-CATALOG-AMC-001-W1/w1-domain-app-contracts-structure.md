# TB-TMAR-CATALOG-AMC-001-W1 — Domain / Application / Contracts structure

## Scope

Migrate physical structure for Catalog Domain, Application root dumps, and Contracts root ports. Infrastructure Directory root dumps deferred to W2.

## Domain

- Split god-file `CatalogDomain.cs` (~2361 LOC, 43 types) into:
  - `Aggregates/`
  - `Enums/`
  - `Events/`
  - `Rules/`
  - `ValueObjects/`
- Moved remaining Domain root files into capability folders:
  - `Categories/`, `Products/`, `Reservation/`, `Settings/`, `StoreLandingPages/`, `StoreMenus/`, `TemplateCatalog/`
- Domain root allowlist: `GlobalUsings.cs` + `.csproj` only
- Path ↔ namespace EXACT; consumer GlobalUsings added (Application/Infrastructure/Endpoints/Host.Tests)

## Application

- Moved root dumps into capability folders:
  - `Shared/` (actor + store scope)
  - `ProductPublishing/ProductPublishPrep.cs`
  - `Settings/StoreAppearance/Commands/*Write*`
  - `StoreLandingPages/Commands/*Write*`
  - `StoreMenus/Commands/*Write*`
- Split god-file `CatalogContracts.cs` (~1136 LOC, 65 types) into:
  - `Ports/` (3 interfaces)
  - `Models/` (62 records/enums)
- Application root now: GlobalUsings only (+ Validators codes folder remains)

## Contracts

- Moved 10 root contract/port files into `Ports/` with namespace `Tooba.Catalog.Contracts.Ports`
- Added `using Tooba.Catalog.Contracts.Ports` to 61 consumers
- Existing `Errors/`, `Cart/`, `Checkout/`, `Reservation/` unchanged

## Solution Explorer

- Unchanged `/Modules/Catalog/` (already canonical)

## Build

- `Tooba.Catalog.*` + `Tooba.Host` build PASS after namespace consumer fixes

## Deferred to W2

- Infrastructure root `*Directory.cs` / gateway dumps → `Directories/`
- Residual `Results.Json` / non-Result write contracts / validator matrix → W3
