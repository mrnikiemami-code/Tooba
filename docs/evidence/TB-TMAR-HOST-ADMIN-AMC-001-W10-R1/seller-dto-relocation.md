# Seller DTO relocation — TB-TMAR-HOST-ADMIN-AMC-001-W10-R1

## Problem

W10 left `SetProductAttributeRequest` in `Admin/CatalogAttributeEndpoints.cs` solely for Host Seller consumption. That violates the W10 partial-host rule: retained Admin Attribute file must only hold variant/category-change members.

## Consumer inventory

| Consumer | Usage |
|---|---|
| `Seller/SellerPanelEndpoints.SetProductAttributeAsync` | HTTP body bind: `RawValue`, `EnumOptionId` |
| Admin Attribute routes | None (product-attribute Admin routes already Catalog-owned) |
| Catalog ProductValues endpoints | Own module DTOs; **not** this record |

## Relocation

| From | To |
|---|---|
| `Tooba.Host.Admin` public record in CatalogAttributeEndpoints.cs | `Tooba.Host.Seller` public record at end of SellerPanelEndpoints.cs |

Exact signature preserved:

```csharp
public sealed record SetProductAttributeRequest(string RawValue, Guid? EnumOptionId);
```

No new shared business contract. No Seller folder migration. No route/status/JSON behavior change.

## Admin namespace import

Seller still needs `using Tooba.Host.Admin` for **`SetProductVariantAxesRequest`** (variant-axes Seller route). Import is **not** removable; leak for `SetProductAttributeRequest` specifically is removed.

## Post-state

| Check | Result |
|---|---|
| Admin CatalogAttributeEndpoints.cs | No `SetProductAttributeRequest` |
| SellerPanelEndpoints.cs | Owns record + binds same shape |
| Host/Admin `*.cs` count | 53 (no new Admin file) |
