# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W10-R1

## Parent

| Field | Value |
|---|---|
| Parent-Task | TB-TMAR-HOST-ADMIN-AMC-001-W10 |
| Parent-Commit | bb510b65be7e27096e061036e4e5c5335e8e0b51 |
| Claim-ID | 951c9b00-3207-4581-8a0f-f24309ccfb2a |

## Pre-repair audit

| Item | Finding |
|---|---|
| `MapAttributeInvalid` definition | Present in `Admin/CatalogAttributeEndpoints.cs` |
| `MapAttributeInvalid` runtime callers | **ZERO** (definition only; grep `MapAttributeInvalid\(` → definition site only) |
| Guard assertions requiring helper presence | W8 Host file; W9 Host file |
| `SetProductAttributeRequest` definition | Public record at bottom of Admin Attribute file |
| Seller consumer | `Seller/SellerPanelEndpoints.SetProductAttributeAsync` binds body |
| Also via Admin import | `SetProductVariantAxesRequest` (variant axes Seller route) — **must keep** `using Tooba.Host.Admin` |
| Other Admin-namespace-only DTO dependents | None for `SetProductAttributeRequest` |
| JSON shape to preserve | `RawValue` (string), `EnumOptionId` (Guid?) |
| W10 four Catalog Product Attribute routes | Must remain unchanged |

## Disposition

| Residual | Action |
|---|---|
| Dead `MapAttributeInvalid` | REMOVE (zero runtime callers) |
| Stale W8/W9/W10 guard expectations | REPAIR — assert helper/DTO **absent**; assert migrated surfaces do **not** message-parse |
| `SetProductAttributeRequest` in Admin file | RELOCATE to Seller endpoint file (caller-local transport) |
| Variant/category-change Host members | RETAIN unchanged |
| W11 | NOT STARTED |

## Out of scope

- Variant / category-change migration
- Seller panel business migration
- New shared business contract for the HTTP DTO
- Product Attribute Catalog route/behavior changes
- StoreAppearance
