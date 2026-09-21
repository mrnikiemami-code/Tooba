# Order boundary audit — TB-TMAR-NEXT-MODULE-BATCH-003-R1

## Before

`Tooba.Notification.Infrastructure.csproj` ProjectReferences included:

- `..\..\Order\Tooba.Order.Application\Tooba.Order.Application.csproj`
- Payment/Fulfillment/Returns.Contracts (already Contracts-only for those modules)

`NotificationProjector.cs`:

```csharp
using Tooba.Order.Application;
```

Consumed:

- `IOrderNotificationReader`
- `OrderNotificationRecipientSnapshot`

Those types lived in `Tooba.Order.Application` (`OrderContracts.cs`).

Architecture guard rejected Payment/Fulfillment/Returns Application/Domain but **did not** reject Order.Application / Order.Domain / Order.Infrastructure.

## After

`Tooba.Notification.Infrastructure.csproj` ProjectReferences include:

- `..\..\Order\Tooba.Order.Contracts\Tooba.Order.Contracts.csproj`
- Payment/Fulfillment/Returns.Contracts (unchanged)

**Removed:** Order.Application project reference.

`NotificationProjector.cs`:

```csharp
using Tooba.Order.Contracts.Notifications;
```

New assembly:

- `Tooba.Order.Contracts/Notifications/OrderNotificationContracts.cs`
- namespace `Tooba.Order.Contracts.Notifications`
- types: `OrderNotificationRecipientSnapshot`, `OrderNotificationSellerSnapshot`, `IOrderNotificationReader`

`Tooba.Order.Infrastructure`:

- ProjectReference added to `Tooba.Order.Contracts`
- `OrderNotificationBridge` + DI registration use Contracts namespace
- Lookup semantics unchanged

Forbidden after repair (guard enforced):

- Order.Application / Domain / Infrastructure
- Payment/Fulfillment/Returns Application / Domain / Infrastructure
- foreign DbContext usings in Notification production sources
- `using Tooba.Order.Application` in Notification production sources
