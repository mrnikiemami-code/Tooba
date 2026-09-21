# Guard delta — TB-TMAR-NEXT-MODULE-BATCH-003-R1

## NotificationArchitectureGuardTests

### `Infrastructure_uses_public_contracts_not_foreign_application`

Now requires Order.Contracts and rejects:

- Order.Application / Order.Domain / Order.Infrastructure
- Payment/Fulfillment/Returns Application / Domain / Infrastructure
- `using Tooba.Order.Application` in Notification production sources
- foreign DbContext names (`OrderDbContext`, `PaymentDbContext`, `FulfillmentDbContext`, `ReturnsDbContext`)

### New: `Order_Contracts_notification_surface_is_clean`

- physical `Notifications/` folder present; no root `.cs` dump
- namespaces start with `Tooba.Order.Contracts.Notifications`
- Contracts csproj has no Domain/Application/Infrastructure/Host refs
- no `TypeForwardedTo`

## Parent BATCH-003 guards preserved

Domain no Contracts; Contracts clean; Application no foreign App/Infra; IClock/IIdGenerator; physical trees; exception codes; SoftDelete; no clock/id bypass.
