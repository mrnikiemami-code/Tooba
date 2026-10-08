# Physical tree — BEFORE (W0 baseline, `f5c5a6db`)

Extracted from git: `git ls-tree -r --name-only f5c5a6db -- src/backend/Modules/Returns`.
Only production `.cs` shown (bin/obj/artifacts excluded; migrations listed separately).

```text
src/backend/Modules/Returns/
  Tooba.Returns.Application/            <- TECHNICAL_AXIS_FIRST + OVER_FOLDERED
    Commands/                           <- technical axis at project root
      ApproveReturn/ApproveReturnCommand.cs                 (1-file leaf)
      CreateReturn/CreateReturnCommand.cs                   (1-file leaf)
      RejectReturn/RejectReturnCommand.cs                   (1-file leaf)
      RetryReturnRefund/RetryReturnRefundCommand.cs         (1-file leaf)
    Queries/                            <- technical axis at project root
      GetAdminReturn/GetAdminReturnQuery.cs                 (1-file leaf)
      GetCustomerReturn/GetCustomerReturnQuery.cs           (1-file leaf)
      GetSellerReturn/GetSellerReturnQuery.cs               (1-file leaf)
      ListAdminReturns/ListAdminReturnsQuery.cs             (1-file leaf)
      ListCustomerReturns/ListCustomerReturnsQuery.cs       (1-file leaf)
      ListSellerReturns/ListSellerReturnsQuery.cs           (1-file leaf)
      QueryAdminReturnsGrid/QueryAdminReturnsGridQuery.cs   (1-file leaf)
    Errors/ReturnsExceptionMapper.cs    <- message-text classification seam (169 LOC)
    Models/                             <- 12 files, incl. DUPLICATE command records
      AdminReturnWorkQueueModels.cs     <- MULTI_RESPONSIBILITY (163 LOC: row + filters + policy)
      ApproveReturnCommand.cs           <- duplicate of Commands/ApproveReturn/ApproveReturnCommand
      CreateReturnCommand.cs            <- duplicate of Commands/CreateReturn/CreateReturnCommand
      RejectReturnCommand.cs            <- duplicate of Commands/RejectReturn/RejectReturnCommand
      RetryRefundCommand.cs             <- duplicate of Commands/RetryReturnRefund/RetryReturnRefundCommand
      RefundAttemptSnapshot.cs  ReturnEligibilityReasonCodes.cs  ReturnEligibilityResult.cs
      ReturnItemSnapshot.cs  ReturnLineCommand.cs  ReturnLineEligibility.cs  ReturnSnapshot.cs
    Ports/
      IReturnDirectory.cs  IReturnEligibilityEvaluator.cs  IReturnInventoryGateway.cs
      IReturnUseCaseGuard.cs
      ReturnSemanticMapper.cs           <- DEAD (zero callers)

  Tooba.Returns.Contracts/
    Errors/ReturnsErrorCodes.cs
    Events/History/Operations/Settlement/   (fine)
    (no Resources/ — bilingual resources lived elsewhere)

  Tooba.Returns.Domain/   Aggregates/ Events/ ValueObjects/   (fine)

  Tooba.Returns.Infrastructure/   Adapters/ Bridges/ DependencyInjection/ Errors/
    Evaluators/ Gateways/ Messaging/ Observability/ Persistence/ Queries/   (fine)
    (no OpenReturnUseCaseGuard in Directories/ — cohesion gap)

  Tooba.Returns.Endpoints/   Admin/ Customer/ Seller/ + root ReturnEndpointModule.cs   (fine)

  Tooba.Returns.Tests/   Architecture/ Behavior/ Endpoints/   (fine)
```

## Defects recorded by W0 for this surface

| Defect | Exact path | State |
|---|---|---|
| Technical-axis-first Application root | `Application/{Commands,Queries}` | `TECHNICAL_AXIS_FIRST` |
| 11 single-file use-case leaf folders | `Application/Commands/<UseCase>/`, `Application/Queries/<UseCase>/` | `OVER_FOLDERED` |
| Duplicate command records | `Application/Models/{CreateReturnCommand,ApproveReturnCommand,RejectReturnCommand,RetryRefundCommand}.cs` | `DUPLICATE_COPY` |
| Multi-responsibility god-file | `Application/Models/AdminReturnWorkQueueModels.cs` (163 LOC) | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` |
| Message-text fault classifier | `Application/Errors/ReturnsExceptionMapper.cs` (169 LOC, ~18 `ex.Message` literals) | non-canonical mechanism + cohesion violation |
| Dead mapper | `Application/Ports/ReturnSemanticMapper.cs` | dead code |
| Mixed-purpose directoriy | `Infrastructure/Directories/ReturnDirectory.cs` (guard co-located) | cohesion gap |
