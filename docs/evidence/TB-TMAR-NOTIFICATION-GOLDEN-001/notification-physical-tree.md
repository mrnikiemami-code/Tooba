# Notification Physical Tree

Handwritten Notification production `.cs` files (excluding Migrations/bin/obj):

| path | namespace | responsibility |
|---|---|---|
| `src/backend/Modules/Notification/Tooba.Notification.Domain/Aggregates/UserNotification.cs` | `Tooba.Notification.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Notification/Tooba.Notification.Domain/ValueObjects/NotificationRecipientKind.cs` | `Tooba.Notification.Domain.ValueObjects` | domain value object |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Commands/DismissCustomerNotification/DismissCustomerNotificationCommand.cs` | `Tooba.Notification.Application.Commands.DismissCustomerNotification` | MediatR command/handler |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Commands/DismissSellerNotification/DismissSellerNotificationCommand.cs` | `Tooba.Notification.Application.Commands.DismissSellerNotification` | MediatR command/handler |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Commands/MarkAllCustomerNotificationsRead/MarkAllCustomerNotificationsReadCommand.cs` | `Tooba.Notification.Application.Commands.MarkAllCustomerNotificationsRead` | MediatR command/handler |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Commands/MarkAllSellerNotificationsRead/MarkAllSellerNotificationsReadCommand.cs` | `Tooba.Notification.Application.Commands.MarkAllSellerNotificationsRead` | MediatR command/handler |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Commands/MarkCustomerNotificationRead/MarkCustomerNotificationReadCommand.cs` | `Tooba.Notification.Application.Commands.MarkCustomerNotificationRead` | MediatR command/handler |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Commands/MarkSellerNotificationRead/MarkSellerNotificationReadCommand.cs` | `Tooba.Notification.Application.Commands.MarkSellerNotificationRead` | MediatR command/handler |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Errors/NotificationErrorCodes.cs` | `Tooba.Notification.Application.Errors` | error codes |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Models/NotificationHttpModels.cs` | `Tooba.Notification.Application.Models` | HTTP response projection models |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Models/NotificationModels.cs` | `Tooba.Notification.Application.Models` | directory list models |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Models/NotificationRecipientKindMapping.cs` | `Tooba.Notification.Application.Models` | recipient kind mapping |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Ports/INotificationDirectory.cs` | `Tooba.Notification.Application.Ports` | application port |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Queries/GetCustomerUnreadNotificationCount/GetCustomerUnreadNotificationCountQuery.cs` | `Tooba.Notification.Application.Queries.GetCustomerUnreadNotificationCount` | MediatR query/handler |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Queries/GetSellerUnreadNotificationCount/GetSellerUnreadNotificationCountQuery.cs` | `Tooba.Notification.Application.Queries.GetSellerUnreadNotificationCount` | MediatR query/handler |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Queries/ListCustomerNotifications/ListCustomerNotificationsQuery.cs` | `Tooba.Notification.Application.Queries.ListCustomerNotifications` | MediatR query/handler |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Queries/ListSellerNotifications/ListSellerNotificationsQuery.cs` | `Tooba.Notification.Application.Queries.ListSellerNotifications` | MediatR query/handler |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Rendering/NotificationCopy.cs` | `Tooba.Notification.Application.Rendering` | copy/rendering |
| `src/backend/Modules/Notification/Tooba.Notification.Contracts/Commands/CreateNotificationCommand.cs` | `Tooba.Notification.Contracts.Commands` | contracts create command |
| `src/backend/Modules/Notification/Tooba.Notification.Contracts/Copy/NotificationSemanticTypes.cs` | `Tooba.Notification.Contracts.Copy` | semantic type constants |
| `src/backend/Modules/Notification/Tooba.Notification.Contracts/Dtos/NotificationRecipientKind.cs` | `Tooba.Notification.Contracts.Dtos` | contracts DTO |
| `src/backend/Modules/Notification/Tooba.Notification.Contracts/Ports/INotificationCreationPort.cs` | `Tooba.Notification.Contracts.Ports` | cross-module creation port |
| `src/backend/Modules/Notification/Tooba.Notification.Contracts/Routes/NotificationTargetRoutes.cs` | `Tooba.Notification.Contracts.Routes` | target route allowlist |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/DependencyInjection/NotificationModule.cs` | `Tooba.Notification.Infrastructure.DependencyInjection` | module DI |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Directories/NotificationDirectory.cs` | `Tooba.Notification.Infrastructure.Directories` | directory implementation |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Handlers/NotificationEventHandlers.cs` | `Tooba.Notification.Infrastructure.Handlers` | integration handlers |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Messaging/NotificationOutboxRegistration.cs` | `Tooba.Notification.Infrastructure.Messaging` | outbox registration |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Observability/NotificationInstrumentation.cs` | `Tooba.Notification.Infrastructure.Observability` | instrumentation |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Persistence/NotificationDbContext.cs` | `Tooba.Notification.Infrastructure.Persistence` | EF DbContext |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Projectors/NotificationProjector.cs` | `Tooba.Notification.Infrastructure.Projectors` | event projector |
| `src/backend/Modules/Notification/Tooba.Notification.Endpoints/NotificationEndpointModule.cs` | `Tooba.Notification.Endpoints` | endpoint composition |
| `src/backend/Modules/Notification/Tooba.Notification.Endpoints/Customer/INotificationCustomerAuthorizer.cs` | `Tooba.Notification.Endpoints.Customer` | customer auth seam |
| `src/backend/Modules/Notification/Tooba.Notification.Endpoints/Customer/NotificationCustomerEndpoints.cs` | `Tooba.Notification.Endpoints.Customer` | customer HTTP routes |
| `src/backend/Modules/Notification/Tooba.Notification.Endpoints/Seller/INotificationSellerAuthorizer.cs` | `Tooba.Notification.Endpoints.Seller` | seller auth seam |
| `src/backend/Modules/Notification/Tooba.Notification.Endpoints/Seller/NotificationSellerEndpoints.cs` | `Tooba.Notification.Endpoints.Seller` | seller HTTP routes |
| `src/backend/Modules/Notification/Tooba.Notification.Endpoints/Errors/NotificationErrorCatalogContributor.cs` | `Tooba.Notification.Endpoints.Errors` | error catalog |

## Host adapters (composition/security only)

| path | responsibility |
|---|---|
| `src/backend/Host/Tooba.Host/Customer/HostNotificationCustomerAuthorizer.cs` | session/dev-actor/guest actor resolution |
| `src/backend/Host/Tooba.Host/Seller/HostNotificationSellerAuthorizer.cs` | SellerPanelAccess adapter |

## Verdict
Notification-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
