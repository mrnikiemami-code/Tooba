# TB-TMAR-CATALOG-AMC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only (no production code change in W0).

## Ownership

| Surface | Owner |
|---|---|
| Admin / Seller / Storefront Catalog HTTP | `Tooba.Catalog.Endpoints` |
| Capability CQRS (Categories, Attributes, Variants, Products, Settings, Storefront, …) | `Tooba.Catalog.Application` |
| Catalog aggregates / rules / settings entities | `Tooba.Catalog.Domain` |
| Directories / DbContext / Development seeds / Storefront composer | `Tooba.Catalog.Infrastructure` |
| Cross-module read/lookup ports + `CatalogErrorCodes` | `Tooba.Catalog.Contracts` |
| Host | Composition / thin seller authorizer only |

## Solution Explorer

- `/Modules/Catalog/` already groups all 5 projects in `Tooba.slnx` → `CANONICAL` (no W1 slnx wave required).

## Foreign coupling / microservice blockers

- ProjectReference / `using` to foreign `Application|Infrastructure|Domain`: **ZERO**
- Legal Contracts-only: Cart, Payment, Order, Party, Offer, Pricing, Inventory, Tax, Promotion, Reviews, Content, Media, Localization, OperatorProfile, ModuleContracts
- Cross-module persistence joins: none found in Catalog production sources
- Microservice extractability: **blocked by structure + god-files + residual non-canonical API/validator gaps**, not by illegal coupling

## Structured state (W0)

| Field | State |
|---|---|
| Foundation-State | `FOUNDATION_PARTIAL` (projects exist; Domain/App/Infra/Contracts root dumps + god-files) |
| Ownership-State | correct (module-owned HTTP + CQRS) |
| File-Cohesion-State | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` |
| Oversized/God-File-State | `CatalogDomain.cs` ~2361 LOC; `CatalogContracts.cs` ~1136 LOC; `CatalogDirectory.cs` ~1697 LOC |
| Localization-State | `HARDCODED_TEXT` (validators `WithMessage` Persian; Domain/seed content mixed) |
| API-Result-Pattern-State | `PARTIAL` — 29/42 endpoint files use `ApiResponseFactory`; 5 files still `Results.Json`/`Results.Problem` |
| Stable-Error-Code-State | `CATALOGUED` (`Contracts/Errors/CatalogErrorCodes`) — contributor/resx still in Endpoints |
| Logging-State | `CANONICAL` (no parallel pipeline found) |
| CQRS-State | `PARTIAL` — 145/167 `IRequest<Result*>`; 22 root write contracts still raw entity/`Unit` |
| Validator-Coverage-State | `GAPS` — 88 endpoint-reachable; 37 validators present; 67 missing (many classify as `NO_VALIDATOR_REQUIRED`) |
| Contracts-Boundary-State | `CLEAN` (project refs) / cohesion smell: Application `CatalogContracts.cs` dump |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Host-Residue-State | composition/seed host only |
| Schema-Migration-State | `UNCHANGED` (no schema work in AMSC) |
| Behavior-Preservation-Risk | `MEDIUM` (API mapping + Result conversion must preserve shapes/status) |
| Structure-Handoff-State | `REQUIRED` |
| Folder-Granularity-State | `MIXED` — Application capabilities mostly shallow; Domain/Infra/Contracts `ROOT_DUMP` |
| Final-Disposition | `READY_TO_MIGRATE` |

## Root dump inventory

- **Domain** root: 33 files + god `CatalogDomain.cs`
- **Application** root: 10 files (`CatalogContracts.cs`, Store*Write*, actor/scope helpers)
- **Infrastructure** root: 35 Directory/Gateway files + `CatalogModule.cs` (DI allowlist)
- **Contracts** root: 10 port/contract files (plus `Errors/`, `Cart/`, `Checkout/`, `Reservation/`)

## Residual non-canonical HTTP

| File | Issue |
|---|---|
| `Storefront/Browse/CatalogStorefrontBrowseEndpoints.cs` | `Results.Json` success + ad-hoc 404 JSON |
| `Storefront/Settings/CatalogStorefrontSettingsEndpoints.cs` | `Results.Json` + ad-hoc 500 JSON |
| `Storefront/TemplateCatalog/CatalogTemplateCatalogStorefrontEndpoints.cs` | `Results.Json` |
| `Admin/ProductMedia/CatalogProductMediaAdminEndpoints.cs` | one `Results.Json` 201 path |
| `Admin/CatalogDemo/CatalogDemoDevEndpoints.cs` | `Results.Problem` (dev-only) |

## Wave plan

| Wave | Focus | Commit |
|---|---|---|
| W0 | Analyze + SoT start | docs only |
| W1 | Split Domain god-file + Domain/Application/Contracts root dumps → capability folders; path↔namespace EXACT; GlobalUsings | code |
| W2 | Infrastructure root → `Directories/` / capability folders; keep `CatalogModule` root allowlist | code |
| W3 | Result conversion for residual write contracts; `ApiResponseFactory` on residual endpoints; validator exhaustive matrix; strip validator hard-coded Persian messages | code |
| W4 | Structure gate + Certify `COMPLETE_REFERENCE_PATTERN` + durable guards + manifest/SoT | code+docs |

## Canonical reference

- Offer / BuildingBlocks for Result, `ApiResponseFactory`, CQRS foundation, error catalog, path↔namespace
- Party / UserPreference AMSC waves for `/Modules/<Name>/` + certify evidence shape
