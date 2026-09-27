# Foundation — TB-TMAR-HOST-ADMIN-AMC-001 W1

## Classification

Catalog destination before W1: **FOUNDATION_PARTIAL** (Contracts/Domain/Application/Infrastructure present; **Endpoints missing**).

After W1: Endpoints project exists and is Host-wired; Admin HTTP still Host-owned.

## Created

| Path | Role |
|------|------|
| `Modules/Catalog/Tooba.Catalog.Endpoints/Tooba.Catalog.Endpoints.csproj` | HTTP project |
| `CatalogEndpointModule.cs` | `MapCatalogModuleEndpoints` (empty map) + `AddCatalogEndpointPresentation` |
| `Admin/ICatalogAdminAuthorizer.cs` | Thin seam over `IAdminPanelAccess` (Fulfillment parity) |
| `GlobalUsings.cs` | ASP.NET Core composition usings |

## Wiring

| Seam | Change |
|------|--------|
| `Tooba.slnx` | New Folder `/Modules/Catalog/` with Domain/Contracts/Application/Infrastructure/**Endpoints** (removed from mixed `/Modules/`) |
| `Tooba.Host.csproj` | ProjectReference → Catalog.Endpoints |
| `Program.cs` | `AddCatalogEndpointPresentation()` + `MapCatalogModuleEndpoints()` |

## Deferred (intentional)

- Error catalog / `.resx` / `CatalogErrorCodes` — land with first canonical `Result` + `ApiResponseFactory` endpoint (W2+)
- Moving `QuantitySettingsEndpoints` / `StoreAppearanceSettings*` — optional W1; deferred to keep foundation green and behavior-stable (Host still uses RAW `Results.Json` + static `AdminPanelAccess`)
- Full Admin evacuation

## Pattern references

- Fulfillment: thin `I*AdminAuthorizer` → `IAdminPanelAccess`
- Content/Offer: EndpointModule composition + presentation registration
- Endpoints → Application + Contracts + BuildingBlocks; **no** Infrastructure/Host refs
