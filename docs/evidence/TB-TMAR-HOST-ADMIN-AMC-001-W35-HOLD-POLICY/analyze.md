# W35-HOLD-POLICY analyze

## Source (Host Admin)
- HoldPolicySettingsEndpoints.cs (GET/PUT `/v1/admin/settings/hold-policy`)
- Multi-owner aggregate: Catalog store hours + Payment method overrides + Cart persistence hours + Order reservation preview/write

## Destination
| Responsibility | Destination |
|---|---|
| Admin HTTP same routes | Catalog.Endpoints Admin.Settings.HoldPolicySettingsEndpoints |
| MediatR CQRS | Catalog.Application Settings/HoldPolicy |
| Store cart/payment hours | Catalog.Contracts `IStoreHoldPolicySettingsPort` + Infrastructure |
| Reservation write | existing `IStoreReservationPolicySettingsPort` |
| Reservation store preview | Order.Contracts `IReservationCyclePolicyPreviewPort` + Order.Infrastructure |
| Payment method overrides + platform defaults | Payment.Contracts `IPaymentHoldSettingsGateway` |
| Cart platform persistence hours | Cart.Contracts `ICartPersistenceHoursSource` |
| Auth / Result | ICatalogAdminAuthorizer + ApiResponseFactory |

## Out of scope
StoreAppearance*
