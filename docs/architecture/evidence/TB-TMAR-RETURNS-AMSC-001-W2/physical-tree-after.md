# Physical tree — AFTER (W2 verified, `0a573864` + this wave)

Verified on disk at the W2 wave. `bin/`, `obj/`, `artifacts/` excluded.

```text
src/backend/Modules/Returns/
  Tooba.Returns.Application/                       [capability-first shallow]
    Composition/
      ReturnsOperation.cs                          (67 LOC — single typed-fault -> Result seam)
      ReturnRefundDestinationParser.cs             (25 LOC — transport destination parsing)
    ReturnRequests/                                [the return-request capability]
      Commands/
        ApproveReturnCommand.cs                    (29 LOC)
        CreateReturnCommand.cs                     (23 LOC)
        RejectReturnCommand.cs                     (28 LOC)
        RetryReturnRefundCommand.cs                (16 LOC)
      Queries/
        GetAdminReturnQuery.cs                     (20 LOC)
        GetCustomerReturnQuery.cs                  (22 LOC)
        GetSellerReturnQuery.cs                    (22 LOC)
        ListAdminReturnsQuery.cs                   (14 LOC)
        ListCustomerReturnsQuery.cs                (14 LOC)
        ListSellerReturnsQuery.cs                  (14 LOC)
        QueryAdminReturnsGridQuery.cs              (107 LOC)
      Models/
        AdminReturnQueueFilters.cs                 (134 LOC — queue filter/status/action policy)
        AdminReturnWorkQueueRow.cs                 (24 LOC — internal read-model DTO)
        RefundAttemptSnapshot.cs                   (16 LOC)
        ReturnEligibilityReasonCodes.cs            (31 LOC — delegates to boundary contract)
        ReturnEligibilityResult.cs                 (13 LOC)
        ReturnItemSnapshot.cs                      (12 LOC)
        ReturnLineCommand.cs                       (6 LOC)
        ReturnLineEligibility.cs                   (10 LOC)
        ReturnSnapshot.cs                          (21 LOC)
      Ports/
        IReturnDirectory.cs                        (48 LOC)
        IReturnEligibilityEvaluator.cs             (11 LOC)
        IReturnInventoryGateway.cs                 (17 LOC)
        IReturnUseCaseGuard.cs                     (11 LOC)
    Validation/
      ReturnsRequestValidators.cs                  (79 LOC — 4 transport validators)
      ReturnsValidationCodes.cs                    (34 LOC)

  Tooba.Returns.Contracts/
    Errors/       ReturnsErrorCodes.cs, ReturnsErrorResourceSet.cs
    Events/       RefundSucceededIntegrationEvent.cs, ReturnApprovedIntegrationEvent.cs,
                  ReturnRequestedIntegrationEvent.cs
    History/      ReturnHistoryContracts.cs
    Operations/   ReturnAdminOperationsContracts.cs
    Resources/    ReturnsErrors.resx, ReturnsErrors.fa.resx
    Settlement/   ReturnSettlementContracts.cs

  Tooba.Returns.Domain/
    Aggregates/     RefundAttempt.cs, ReturnItem.cs, ReturnRequest.cs
    Events/         RefundSucceededDomainEvent.cs, ReturnApprovedDomainEvent.cs,
                    ReturnRequestedDomainEvent.cs
    ValueObjects/   RefundAttemptStatus.cs, RefundDestination.cs, ReturnRequestStatus.cs

  Tooba.Returns.Infrastructure/
    Adapters/            ReturnAdminOperationsAdapter.cs, ReturnHistoryReader.cs
    Bridges/             ReturnSettlementBridge.cs
    DependencyInjection/ ReturnsModule.cs
    Directories/         OpenReturnUseCaseGuard.cs, ReturnDirectory.cs
    Errors/              ReturnsErrorCatalogContributor.cs
    Evaluators/          ReturnEligibilityEvaluator.cs
    Gateways/            ReturnInventoryGateway.cs
    Messaging/           ReturnsOutboxRegistration.cs
    Observability/       ReturnsInstrumentation.cs
    Persistence/         ReturnsDbContext.cs
    Persistence/Migrations/
      20260827020000_InitialReturns.cs
      20260827020000_InitialReturns.Designer.cs
      20260827200000_AddRefundDestination.cs
      20260909130600_DecimalReturnQuantity.cs
      ReturnsDbContextModelSnapshot.cs
    Queries/             AdminReturnGridQueryEngine.cs

  Tooba.Returns.Endpoints/
    ReturnEndpointModule.cs                            [root allowlist = exactly this]
    Admin/     IReturnAdminAuthorizer.cs, ReturnAdminEndpoints.cs
    Customer/  IReturnCustomerAuthorizer.cs, ReturnCustomerAuthorizer.cs,
               ReturnCustomerEndpoints.cs
    Seller/    IReturnSellerAuthorizer.cs, ReturnSellerEndpoints.cs

  Tooba.Returns.Tests/
    Architecture/  ReturnsArchitectureGuardTests.cs
    Behavior/      ReturnsCharacterizationTests.cs, ReturnsErrorAndGridTests.cs,
                   ReturnsSemanticPresentationTests.cs
    Endpoints/     ReturnsEndpointOwnershipTests.cs
```

## Deltas vs BEFORE

| Removed path | Replaced by |
|---|---|
| `Application/Commands/{ApproveReturn,CreateReturn,RejectReturn,RetryReturnRefund}/…` | `Application/ReturnRequests/Commands/*.cs` (flattened) |
| `Application/Queries/{7 use-case}/…` | `Application/ReturnRequests/Queries/*.cs` (flattened) |
| `Application/Models/{CreateReturnCommand,ApproveReturnCommand,RejectReturnCommand,RetryRefundCommand}.cs` | deleted (duplicates of the MediatR requests) |
| `Application/Models/AdminReturnWorkQueueModels.cs` | `ReturnRequests/Models/{AdminReturnWorkQueueRow,AdminReturnQueueFilters}.cs` |
| `Application/Errors/ReturnsExceptionMapper.cs` (folder removed) | `Application/Composition/{ReturnsOperation,ReturnRefundDestinationParser}.cs` |
| `Application/Ports/ReturnSemanticMapper.cs` | deleted (dead) |
| `Application/{Commands,Queries,Models,Ports}` roots | `Application/ReturnRequests/{Commands,Queries,Models,Ports}` |

| Added path | Reason |
|---|---|
| `Contracts/Errors/ReturnsErrorResourceSet.cs`, `Contracts/Resources/ReturnsErrors{,.fa}.resx` | W1 localization ownership (21 bilingual keys, registered once) |
| `Infrastructure/Directories/OpenReturnUseCaseGuard.cs` | cohesion split out of `ReturnDirectory.cs` |
| (none) | `Infrastructure/Persistence/Migrations/20260909130600_DecimalReturnQuantity.cs` already existed at `f5c5a6db`; `git diff f5c5a6db 0a573864` on `Persistence/` is empty — the three migrations and the model snapshot are byte-identical through W1/W2 |

Zero net production source-file count change is not claimed: 4 duplicate/dead files were deleted, 3
files were split out of 2 god-files, and the resource set was added. Behavior is unchanged (W1
characterization + guard suites green).
