# Notification physical tree (TB-TMAR-NEXT-MODULE-BATCH-003)

Every handwritten production `.cs`:

| Relative path | Namespace | Responsibility |
|---|---|---|
| `src/backend/Modules/Notification/Tooba.Notification.Application/Models/NotificationModels.cs` | `Tooba.Notification.Application.Models` | application model/DTO |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Models/NotificationRecipientKindMapping.cs` | `Tooba.Notification.Application.Models` | application model/DTO |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Ports/INotificationDirectory.cs` | `Tooba.Notification.Application.Ports` | application/contracts port |
| `src/backend/Modules/Notification/Tooba.Notification.Application/Rendering/NotificationCopy.cs` | `Tooba.Notification.Application.Rendering` | localized copy/rendering |
| `src/backend/Modules/Notification/Tooba.Notification.Contracts/Commands/CreateNotificationCommand.cs` | `Tooba.Notification.Contracts.Commands` | command/contracts command |
| `src/backend/Modules/Notification/Tooba.Notification.Contracts/Copy/NotificationSemanticTypes.cs` | `Tooba.Notification.Contracts.Copy` | contracts copy/types |
| `src/backend/Modules/Notification/Tooba.Notification.Contracts/Dtos/NotificationRecipientKind.cs` | `Tooba.Notification.Contracts.Dtos` | contracts DTO |
| `src/backend/Modules/Notification/Tooba.Notification.Contracts/Ports/INotificationCreationPort.cs` | `Tooba.Notification.Contracts.Ports` | application/contracts port |
| `src/backend/Modules/Notification/Tooba.Notification.Contracts/Routes/NotificationTargetRoutes.cs` | `Tooba.Notification.Contracts.Routes` | target route allowlist |
| `src/backend/Modules/Notification/Tooba.Notification.Domain/Aggregates/UserNotification.cs` | `Tooba.Notification.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Notification/Tooba.Notification.Domain/ValueObjects/NotificationRecipientKind.cs` | `Tooba.Notification.Domain.ValueObjects` | domain value object |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/DependencyInjection/NotificationModule.cs` | `Tooba.Notification.Infrastructure.DependencyInjection` | module DI composition |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Directories/NotificationDirectory.cs` | `Tooba.Notification.Infrastructure.Directories` | directory orchestration |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Handlers/NotificationEventHandlers.cs` | `Tooba.Notification.Infrastructure.Handlers` | integration event handler |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Messaging/NotificationOutboxRegistration.cs` | `Tooba.Notification.Infrastructure.Messaging` | outbox registration |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Observability/NotificationInstrumentation.cs` | `Tooba.Notification.Infrastructure.Observability` | metrics/observability |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Persistence/NotificationDbContext.cs` | `Tooba.Notification.Infrastructure.Persistence` | EF persistence |
| `src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Projectors/NotificationProjector.cs` | `Tooba.Notification.Infrastructure.Projectors` | integration projector |
| `src/backend/Modules/Notification/Tooba.Notification.Tests/Architecture/NotificationArchitectureGuardTests.cs` | `Tooba.Notification.Tests.Architecture` | module source |
| `src/backend/Modules/Notification/Tooba.Notification.Tests/Behavior/NotificationBehaviorCharacterizationTests.cs` | `Tooba.Notification.Tests.Behavior` | module source |

Root production `.cs` dump: **0**.
