# W23-CHECKOUT-IDENTITY analyze

## Source
- `Host/Admin/CheckoutIdentitySettingsEndpoints.cs` (HTTP_ENDPOINT — ILLEGAL_ENDPOINT_OWNERSHIP + DIRECT_DB_WRITE)

## Destination
- Catalog — FOUNDATION_READY (Settings CQRS pattern established by CheckoutAbuse)

## Disposition
| Responsibility | Class | Destination |
|---|---|---|
| Admin GET/PUT `/v1/admin/settings/checkout-identity` | HTTP_ENDPOINT | Catalog.Endpoints/Admin/Settings |
| Load/save singleton `StoreCheckoutIdentitySettings` | APPLICATION_USE_CASE + PERSISTENCE | Catalog.Application Settings/CheckoutIdentity + Infrastructure directory |
| View/write models | APPLICATION models | Catalog.Application Models; write request on Endpoints |
| Host `CheckoutIdentityGate` (storefront enforcement) | HOST platform seam | RETAIN Host.Storefront (not this wave) |
| StoreAppearance* / ReservationPolicy* / HoldPolicy* / Merchandising* / ProductWorkspace* | OUT OF SCOPE | deferred |

## Notes
- GET previously injected Host `CheckoutIdentityGate.GetEffectiveAsync`; Catalog directory reads the same singleton (AuthenticatedOnly default if missing).
- PUT policy coerce Host parity: only `GuestAllowed` (ignore-case) → GuestAllowed; else AuthenticatedOnly. No invalid-policy throw → no new CatalogErrorCode.
- Labels FA/EN preserved in directory ToView for Admin JSON contract parity.
