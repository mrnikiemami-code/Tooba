# Settlement physical tree

| Path | Namespace | Folder responsibility |
|------|-----------|------------------------|
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/GlobalUsings.Domain.cs` | `?` | Tooba.Settlement.Application |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/GlobalUsings.Layout.cs` | `?` | Tooba.Settlement.Application |
| `src/backend/Modules/Settlement/Tooba.Settlement.Application/Ports/SettlementContracts.cs` | `Tooba.Settlement.Application.Ports` | Ports |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/PayoutAttempt.cs` | `Tooba.Settlement.Domain.Aggregates` | Aggregates |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/PayoutRequest.cs` | `Tooba.Settlement.Domain.Aggregates` | Aggregates |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/SellerPayoutProfile.cs` | `Tooba.Settlement.Domain.Aggregates` | Aggregates |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/SettlementAccount.cs` | `Tooba.Settlement.Domain.Aggregates` | Aggregates |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/SettlementEntry.cs` | `Tooba.Settlement.Domain.Aggregates` | Aggregates |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/SettlementStatement.cs` | `Tooba.Settlement.Domain.Aggregates` | Aggregates |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Entities/CommissionPolicy.cs` | `Tooba.Settlement.Domain.Entities` | Entities |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Events/PayoutFailedDomainEvent.cs` | `Tooba.Settlement.Domain.Events` | Events |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Events/PayoutSucceededDomainEvent.cs` | `Tooba.Settlement.Domain.Events` | Events |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Events/SettlementEntryPostedDomainEvent.cs` | `Tooba.Settlement.Domain.Events` | Events |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/ValueObjects/CommissionPolicySnapshot.cs` | `Tooba.Settlement.Domain.ValueObjects` | ValueObjects |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/ValueObjects/EntryType.cs` | `Tooba.Settlement.Domain.ValueObjects` | ValueObjects |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/ValueObjects/PayoutStatus.cs` | `Tooba.Settlement.Domain.ValueObjects` | ValueObjects |
| `src/backend/Modules/Settlement/Tooba.Settlement.Domain/ValueObjects/StatementStatus.cs` | `Tooba.Settlement.Domain.ValueObjects` | ValueObjects |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Bridges/SettlementOrderBridge.cs` | `Tooba.Settlement.Infrastructure.Bridges` | Bridges |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Bridges/SettlementPaymentBridge.cs` | `Tooba.Settlement.Infrastructure.Bridges` | Bridges |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Bridges/SettlementReturnsBridge.cs` | `Tooba.Settlement.Infrastructure.Bridges` | Bridges |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/DependencyInjection/SettlementModule.cs` | `Tooba.Settlement.Infrastructure.DependencyInjection` | DependencyInjection |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Directories/SettlementDirectory.cs` | `Tooba.Settlement.Infrastructure.Directories` | Directories |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Gateways/FailClosedPayoutGateway.cs` | `Tooba.Settlement.Infrastructure.Gateways` | Gateways |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Gateways/FakePayoutGateway.cs` | `Tooba.Settlement.Infrastructure.Gateways` | Gateways |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/GlobalUsings.Domain.cs` | `?` | Tooba.Settlement.Infrastructure |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/GlobalUsings.Layout.cs` | `?` | Tooba.Settlement.Infrastructure |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Handlers/SettlementEventHandlers.cs` | `Tooba.Settlement.Infrastructure.Handlers` | Handlers |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Messaging/SettlementOutboxRegistration.cs` | `Tooba.Settlement.Infrastructure.Messaging` | Messaging |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Observability/SettlementInstrumentation.cs` | `Tooba.Settlement.Infrastructure.Observability` | Observability |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Persistence/SettlementDbContext.cs` | `Tooba.Settlement.Infrastructure.Persistence` | Persistence |
| `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Persistence/SettlementInboxRecords.cs` | `Tooba.Settlement.Infrastructure.Persistence` | Persistence |

Handwritten production .cs count: 31
