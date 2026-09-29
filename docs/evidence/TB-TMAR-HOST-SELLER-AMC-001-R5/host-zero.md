# TB-TMAR-HOST-SELLER-AMC-001-R5 — Host zero

## Final Host/Seller state

```text
src/backend/Host/Tooba.Host/Seller/  -> ABSENT (directory does not exist)
```

| Metric | Before R5 | After R5 |
| --- | --- | --- |
| Host/Seller production files | 2 | 0 |
| Host-owned seller routes | 1 | 0 |
| Duplicate route ownership | 0 | 0 |
| Host/Development sink regression | none | none |

Evacuated/deleted files:

```text
src/backend/Host/Tooba.Host/Seller/SellerPanelEndpoints.cs
src/backend/Host/Tooba.Host/Seller/SellerDevActorBootstrap.cs
```

Removed Host wiring:

```text
Program.cs: using Tooba.Host.Seller;  -> removed
Program.cs: app.MapSellerPanelEndpoints(); -> removed
```

## No sink-folder regression

`src/backend/Host/Tooba.Host/Development/` keeps its accepted allowlist unchanged:

```text
DevelopmentSchemaMigrator.cs
DevelopmentTenantCommerceContext.cs
MarketplaceAdminDevBootstrap.cs
MarketplaceDevelopmentBootstrap.cs
MarketplaceSellerDevBootstrap.cs
```

`MarketplaceSellerDevBootstrap.cs` remains an accepted Host/Development composition file; it now
resolves the AccessControl-owned `ISellerDevContextStore` port instead of a Host seller type.

No Seller business class was moved anywhere under `Host`. The only non-Security Host files whose
name contains `Seller` are the pre-existing development bootstrap and the Filesystem-backed admin
grid engine (`Grid/AdminSellersGridQueryEngine.cs`), both unrelated to seller route ownership.

## Host/Security/Seller (R1A) — retained and protected

The R1A boundary is untouched and remains the canonical global Host security adapter boundary:
`SellerPanelAccess.cs`, `SellerSecurityErrorCodes.cs`, `HostSellerPanelAccess.cs` and the thin
per-module `Host*SellerAuthorizer.cs` adapters. It has ZERO foreign
Application/Domain/Infrastructure/Persistence.

## Route ownership after R5 (unchanged R1A/R2/R3/R4 + evacuated R5)

| Route | Owner |
| --- | --- |
| seller Catalog routes | `Tooba.Catalog.Endpoints/Seller/CatalogSellerEndpoints.cs` |
| seller settings pair | `Tooba.Party.Endpoints/Seller/PartySellerSettingsEndpoints.cs` |
| seller dashboard | `Tooba.Order.Endpoints/Seller/SellerDashboardEndpoints.cs` |
| `GET /v1/seller/dev-contexts` | `Tooba.AccessControl.Endpoints/Seller/Development/SellerDevContextEndpoints.cs` |
| Host-owned seller routes | NONE |

## Certification

```text
Host/Seller production file count = ZERO
Host/Seller route count           = ZERO
Host/Seller directory             = ABSENT
Seller business route ownership in Host = NONE
Seller development bootstrap business authority in Host = NONE
fullSellerFolderCertification     = PASS (CLOSED_HOST_ZERO)
```

No R6 was created: the final closure criteria are all proven here.
