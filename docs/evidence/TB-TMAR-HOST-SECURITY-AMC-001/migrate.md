# Migrate — Host/Security AMC-001

## Disposition

**No folder evacuation.** Host/Security remains the intentional thin platform security boundary.

## Hygiene executed

| Item | Before | After |
| --- | --- | --- |
| `ReviewEndpoints` seller list auth | static `SellerPanelAccess.RequireAuthorizedAsync` | `ISellerPanelAccess` DI |
| `HostPaymentStorefrontAuthorizer` | `RequestServices.GetRequiredService` | constructor-injected `CurrentAuthenticatedSession` |
| Seller R1/R5 guard file lists | missing Catalog/Story adapters | includes `HostCatalogSellerAuthorizer`, `HostStorySellerAuthorizer` |

## Explicit non-goals

- Deleting `Host/Security`
- Moving authorizers into AccessControl / Identity modules
- Schema / frontend change
