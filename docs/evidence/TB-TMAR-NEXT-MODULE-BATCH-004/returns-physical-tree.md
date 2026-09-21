# Returns physical tree

| path | namespace | responsibility |
|---|---|---|
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/ApproveReturnCommand.cs` | `Tooba.Returns.Application.Models` | ApproveReturnCommand |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/CreateReturnCommand.cs` | `Tooba.Returns.Application.Models` | CreateReturnCommand |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/RefundAttemptSnapshot.cs` | `Tooba.Returns.Application.Models` | RefundAttemptSnapshot |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/RejectReturnCommand.cs` | `Tooba.Returns.Application.Models` | RejectReturnCommand |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/RetryRefundCommand.cs` | `Tooba.Returns.Application.Models` | RetryRefundCommand |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/ReturnEligibilityReasonCodes.cs` | `Tooba.Returns.Application.Models` | ReturnEligibilityReasonCodes |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/ReturnEligibilityResult.cs` | `Tooba.Returns.Application.Models` | ReturnEligibilityResult |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/ReturnItemSnapshot.cs` | `Tooba.Returns.Application.Models` | ReturnItemSnapshot |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/ReturnLineCommand.cs` | `Tooba.Returns.Application.Models` | ReturnLineCommand |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/ReturnLineEligibility.cs` | `Tooba.Returns.Application.Models` | ReturnLineEligibility |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Models/ReturnSnapshot.cs` | `Tooba.Returns.Application.Models` | ReturnSnapshot |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Ports/IReturnDirectory.cs` | `Tooba.Returns.Application.Ports` | IReturnDirectory |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Ports/IReturnEligibilityEvaluator.cs` | `Tooba.Returns.Application.Ports` | IReturnEligibilityEvaluator |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Ports/IReturnInventoryGateway.cs` | `Tooba.Returns.Application.Ports` | IReturnInventoryGateway |
| `src/backend/Modules/Returns/Tooba.Returns.Application/Ports/IReturnUseCaseGuard.cs` | `Tooba.Returns.Application.Ports` | IReturnUseCaseGuard |
| `src/backend/Modules/Returns/Tooba.Returns.Contracts/Events/RefundSucceededIntegrationEvent.cs` | `Tooba.Returns.Contracts.Events` | RefundSucceededIntegrationEvent |
| `src/backend/Modules/Returns/Tooba.Returns.Contracts/Events/ReturnApprovedIntegrationEvent.cs` | `Tooba.Returns.Contracts.Events` | ReturnApprovedIntegrationEvent |
| `src/backend/Modules/Returns/Tooba.Returns.Contracts/Events/ReturnRequestedIntegrationEvent.cs` | `Tooba.Returns.Contracts.Events` | ReturnRequestedIntegrationEvent |
| `src/backend/Modules/Returns/Tooba.Returns.Contracts/Settlement/ReturnSettlementContracts.cs` | `Tooba.Returns.Contracts.Settlement` | ReturnSettlementSnapshot, IReturnSettlementReader |
| `src/backend/Modules/Returns/Tooba.Returns.Domain/Aggregates/RefundAttempt.cs` | `Tooba.Returns.Domain.Aggregates` | RefundAttempt |
| `src/backend/Modules/Returns/Tooba.Returns.Domain/Aggregates/ReturnItem.cs` | `Tooba.Returns.Domain.Aggregates` | ReturnItem |
| `src/backend/Modules/Returns/Tooba.Returns.Domain/Aggregates/ReturnRequest.cs` | `Tooba.Returns.Domain.Aggregates` | ReturnRequest |
| `src/backend/Modules/Returns/Tooba.Returns.Domain/Events/RefundSucceededDomainEvent.cs` | `Tooba.Returns.Domain.Events` | RefundSucceededDomainEvent |
| `src/backend/Modules/Returns/Tooba.Returns.Domain/Events/ReturnApprovedDomainEvent.cs` | `Tooba.Returns.Domain.Events` | ReturnApprovedDomainEvent |
| `src/backend/Modules/Returns/Tooba.Returns.Domain/Events/ReturnRequestedDomainEvent.cs` | `Tooba.Returns.Domain.Events` | ReturnRequestedDomainEvent |
| `src/backend/Modules/Returns/Tooba.Returns.Domain/ValueObjects/RefundAttemptStatus.cs` | `Tooba.Returns.Domain.ValueObjects` | RefundAttemptStatus |
| `src/backend/Modules/Returns/Tooba.Returns.Domain/ValueObjects/RefundDestination.cs` | `Tooba.Returns.Domain.ValueObjects` | RefundDestination |
| `src/backend/Modules/Returns/Tooba.Returns.Domain/ValueObjects/ReturnRequestStatus.cs` | `Tooba.Returns.Domain.ValueObjects` | ReturnRequestStatus |
| `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Bridges/ReturnSettlementBridge.cs` | `Tooba.Returns.Infrastructure.Bridges` | ReturnSettlementBridge |
| `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/DependencyInjection/ReturnsModule.cs` | `Tooba.Returns.Infrastructure.DependencyInjection` | ReturnsModule |
| `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.cs` | `Tooba.Returns.Infrastructure.Directories` | OpenReturnUseCaseGuard, ReturnDirectory |
| `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Evaluators/ReturnEligibilityEvaluator.cs` | `Tooba.Returns.Infrastructure.Evaluators` | ReturnEligibilityEvaluator |
| `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Gateways/ReturnInventoryGateway.cs` | `Tooba.Returns.Infrastructure.Gateways` | ReturnInventoryGateway |
| `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Messaging/ReturnsOutboxRegistration.cs` | `Tooba.Returns.Infrastructure.Messaging` | ReturnsOutboxRegistration |
| `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Observability/ReturnsInstrumentation.cs` | `Tooba.Returns.Infrastructure.Observability` | ReturnsInstrumentation |
| `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Persistence/ReturnsDbContext.cs` | `Tooba.Returns.Infrastructure.Persistence` | ReturnsDbContext, ReturnsDbContextFactory |
| `src/backend/Modules/Returns/Tooba.Returns.Tests/Architecture/ReturnsArchitectureGuardTests.cs` | `Tooba.Returns.Tests.Architecture` | ReturnsArchitectureGuardTests |
| `src/backend/Modules/Returns/Tooba.Returns.Tests/Behavior/ReturnsCharacterizationTests.cs` | `Tooba.Returns.Tests.Behavior` | ReturnsCharacterizationTests |

Handwritten production count: 38
