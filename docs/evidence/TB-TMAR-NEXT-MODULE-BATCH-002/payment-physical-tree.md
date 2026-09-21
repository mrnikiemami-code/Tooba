# Payment physical tree (TB-TMAR-NEXT-MODULE-BATCH-002)

Visual Studio Solution Explorer equivalent. Every handwritten production `.cs`:

| Relative path | Namespace | Responsibility |
|---|---|---|
| `src/backend/Modules/Payment/Tooba.Payment.Application/Models/PaymentGatewayActorContext.cs` | `Tooba.Payment.Application.Models` | application model/DTO |
| `src/backend/Modules/Payment/Tooba.Payment.Application/Models/PaymentGatewayOutcomes.cs` | `Tooba.Payment.Application.Models` | application model/DTO |
| `src/backend/Modules/Payment/Tooba.Payment.Application/Ports/CommerceHoldPolicyPorts.cs` | `Tooba.Payment.Application.Ports` | application port |
| `src/backend/Modules/Payment/Tooba.Payment.Application/Ports/PaymentDirectoryPorts.cs` | `Tooba.Payment.Application.Ports` | application port |
| `src/backend/Modules/Payment/Tooba.Payment.Application/Ports/PaymentQueryPorts.cs` | `Tooba.Payment.Application.Ports` | application port |
| `src/backend/Modules/Payment/Tooba.Payment.Application/Ports/PaymentSettlementPorts.cs` | `Tooba.Payment.Application.Ports` | application port |
| `src/backend/Modules/Payment/Tooba.Payment.Application/Ports/PaymentWebhookPorts.cs` | `Tooba.Payment.Application.Ports` | application port |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Aggregates/CustomerPayment.cs` | `Tooba.Payment.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Aggregates/PaymentAllocation.cs` | `Tooba.Payment.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Aggregates/PaymentAttempt.cs` | `Tooba.Payment.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Aggregates/PaymentMethodHoldOverride.cs` | `Tooba.Payment.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Aggregates/PaymentProofAsset.cs` | `Tooba.Payment.Domain.Aggregates` | domain aggregate |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Events/PaymentCreatedDomainEvent.cs` | `Tooba.Payment.Domain.Events` | domain event |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Events/PaymentFailedDomainEvent.cs` | `Tooba.Payment.Domain.Events` | domain event |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Events/PaymentInitiatedDomainEvent.cs` | `Tooba.Payment.Domain.Events` | domain event |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Events/PaymentManualDepositRestoredDomainEvent.cs` | `Tooba.Payment.Domain.Events` | domain event |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Events/PaymentManualDepositUnconfirmedDomainEvent.cs` | `Tooba.Payment.Domain.Events` | domain event |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Events/PaymentRefundedDomainEvent.cs` | `Tooba.Payment.Domain.Events` | domain event |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Events/PaymentRefundFailedDomainEvent.cs` | `Tooba.Payment.Domain.Events` | domain event |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Events/PaymentRefundPendingDomainEvent.cs` | `Tooba.Payment.Domain.Events` | domain event |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/Events/PaymentSucceededDomainEvent.cs` | `Tooba.Payment.Domain.Events` | domain event |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/ValueObjects/PaymentAllocationTargetKind.cs` | `Tooba.Payment.Domain.ValueObjects` | domain value object |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/ValueObjects/PaymentAttemptStatus.cs` | `Tooba.Payment.Domain.ValueObjects` | domain value object |
| `src/backend/Modules/Payment/Tooba.Payment.Domain/ValueObjects/PaymentStatus.cs` | `Tooba.Payment.Domain.ValueObjects` | domain value object |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Adapters/PaymentHoldSettingsDirectory.cs` | `Tooba.Payment.Infrastructure.Adapters` | infrastructure adapter |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Adapters/PaymentQueryDirectory.cs` | `Tooba.Payment.Infrastructure.Adapters` | infrastructure adapter |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Adapters/PaymentSettlementBridge.cs` | `Tooba.Payment.Infrastructure.Adapters` | infrastructure adapter |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Adapters/PaymentWebhookHandler.cs` | `Tooba.Payment.Infrastructure.Adapters` | infrastructure adapter |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Adapters/PaymentWebhookSignatureValidator.cs` | `Tooba.Payment.Infrastructure.Adapters` | infrastructure adapter |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/DependencyInjection/PaymentModule.cs` | `Tooba.Payment.Infrastructure.DependencyInjection` | module DI composition |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs` | `Tooba.Payment.Infrastructure.Directories` | infrastructure directory/use-case orchestration |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Events/PaymentEvents.cs` | `Tooba.Payment.Infrastructure.Events` | integration/outbox events |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Messaging/PaymentOutboxRegistration.cs` | `Tooba.Payment.Infrastructure.Messaging` | outbox/messaging registration |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Persistence/PaymentDbContext.cs` | `Tooba.Payment.Infrastructure.Persistence` | EF persistence |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Persistence/PaymentWebhookInboxRecord.cs` | `Tooba.Payment.Infrastructure.Persistence` | EF persistence |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/FailClosedPaymentGateway.cs` | `Tooba.Payment.Infrastructure.Providers` | payment gateway provider |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/FakePaymentGateway.cs` | `Tooba.Payment.Infrastructure.Providers` | payment gateway provider |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/FakePaymentRefundGateway.cs` | `Tooba.Payment.Infrastructure.Providers` | payment gateway provider |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/ManualPaymentGateway.cs` | `Tooba.Payment.Infrastructure.Providers` | payment gateway provider |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/PaymentGatewayInstrumentation.cs` | `Tooba.Payment.Infrastructure.Providers` | payment gateway provider |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/PaymentGatewayOptions.cs` | `?` | payment gateway provider |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/WalletPaymentGateway.cs` | `Tooba.Payment.Infrastructure.Providers` | payment gateway provider |
| `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/WebhookPaymentGateway.cs` | `Tooba.Payment.Infrastructure.Providers` | payment gateway provider |

Root production `.cs` dump: **0**. Empty ceremonial Endpoints/Contracts projects: **none** (Payment Contracts/Endpoints N/A).
