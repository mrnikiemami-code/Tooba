# Physical tree — BEFORE (wave-start baseline `bac4dbe3`, i.e. the state W1 started from)

`git ls-tree -r --name-only bac4dbe3 -- src/backend/Modules/Settlement` (production `.cs` only; `bin/obj`
excluded). The wave-start baseline of **W2** is `4ca4aafc`; the tree below is the pre-W1 tree and is kept
so the W1 → W2 delta is auditable end to end (the W1 commit message and
`TB-TMAR-SETTLEMENT-AMSC-001-W1/migrate.md` §6/§7 record the moves).

## Tooba.Settlement.Application — `TECHNICAL_AXIS_FIRST` + `OVER_FOLDERED`

```text
Application/
  Commands/
    ProcessAdminPayout/ProcessAdminPayoutCommand.cs          <-- single-file use-case leaf
    RequestSellerPayout/RequestSellerPayoutCommand.cs        <-- single-file use-case leaf
    RetryAdminPayout/RetryAdminPayoutCommand.cs              <-- single-file use-case leaf
  Queries/
    GetSellerSettlementBalance/GetSellerSettlementBalanceQuery.cs
    ListAdminPayoutQueue/ListAdminPayoutQueueQuery.cs
    ListAdminSettlementBalances/ListAdminSettlementBalancesQuery.cs
    ListSellerPayoutRequests/ListSellerPayoutRequestsQuery.cs
    ListSellerSettlementEntries/ListSellerSettlementEntriesQuery.cs
    ListSellerSettlementStatements/ListSellerSettlementStatementsQuery.cs
    QueryAdminPayoutGrid/QueryAdminPayoutGridQuery.cs
    QueryAdminPayoutGrid/AdminPayoutGridQueryPolicy.cs
  Models/SettlementAdminModels.cs
  Ports/IAdminPayoutGridQuery.cs
  Ports/SettlementContracts.cs                               <-- mixed Application bundle + boundary events
  Validators/
    Admin/ProcessAdminPayoutCommandValidator.cs
    Admin/QueryAdminPayoutGridQueryValidator.cs
    Admin/RetryAdminPayoutCommandValidator.cs
    Seller/RequestSellerPayoutCommandValidator.cs
    SettlementValidationCodes.cs
  Errors/
    SettlementErrorCodes.cs                                  <-- stable codes in the wrong layer
    SettlementExceptionMapper.cs                             <-- message-text fault classification
  GlobalUsings.Domain.cs
  GlobalUsings.Layout.cs
```

## Tooba.Settlement.Contracts — boundary vocabulary only (no codes, no events)

```text
Contracts/
  History/SettlementHistoryContracts.cs
  Operations/SettlementAdminOrderDetailContracts.cs
  Operations/SettlementOrderAccrualContracts.cs
```

## Tooba.Settlement.Domain

```text
Domain/
  Aggregates/{PayoutAttempt,PayoutRequest,SellerPayoutProfile,SettlementAccount,SettlementEntry,SettlementStatement}.cs
  Entities/CommissionPolicy.cs
  Events/{PayoutFailedDomainEvent,PayoutSucceededDomainEvent,SettlementEntryPostedDomainEvent}.cs
  ValueObjects/{CommissionPolicySnapshot,EntryType,PayoutStatus,StatementStatus}.cs
```

## Tooba.Settlement.Infrastructure

```text
Infrastructure/
  Adapters/{SettlementAdminOrderDetailReader,SettlementHistoryReader,SettlementOrderAccrualAdapter}.cs
  Bridges/{SettlementOrderBridge,SettlementPaymentBridge,SettlementReturnsBridge}.cs
  DependencyInjection/SettlementModule.cs
  Directories/SettlementDirectory.cs            <-- 701 LOC with the use-case guard co-located
  Errors/SettlementErrorCatalogContributor.cs
  Gateways/{FailClosedPayoutGateway,FakePayoutGateway}.cs
  Handlers/SettlementEventHandlers.cs
  Messaging/SettlementOutboxRegistration.cs
  Observability/SettlementInstrumentation.cs
  Persistence/SettlementDbContext.cs
  Persistence/SettlementInboxRecords.cs
  Persistence/Migrations/20260827030000_InitialSettlement.cs
  Persistence/Migrations/20260827030000_InitialSettlement.Designer.cs
  Persistence/Migrations/SettlementDbContextModelSnapshot.cs
  Queries/AdminPayoutGridQueryEngine.cs
  GlobalUsings.Domain.cs
  GlobalUsings.Layout.cs
```

## Tooba.Settlement.Endpoints / Tests

```text
Endpoints/{SettlementEndpointModule.cs,Admin/{ISettlementAdminAuthorizer,SettlementAdminEndpoints}.cs,Seller/{ISettlementSellerAuthorizer,SettlementSellerEndpoints}.cs}
Tests/{Architecture/{SettlementArchitectureGuardTests,SettlementValidatorCoverageGuardTests}.cs,Behavior/SettlementErrorAndGridTests.cs,Endpoints/SettlementEndpointOwnershipTests.cs,Validation/SettlementValidatorTests.cs}
```

## Structural defects recorded at the W0/W1 baseline

| # | Defect | Path(s) | Classification |
|---|---|---|---|
| 1 | Technical-axis-first Application roots | `Application/Commands`, `Application/Queries`, `Application/Validators`, `Application/Models`, `Application/Ports`, `Application/Errors` | `TECHNICAL_AXIS_FIRST` |
| 2 | 10 single-file use-case leaf folders | `Application/{Commands,Queries}/<UseCase>/` | `OVER_FOLDERED` |
| 3 | Audience-first validator tree | `Application/Validators/{Admin,Seller}` | `TECHNICAL_AXIS_FIRST` |
| 4 | Mixed Application bundle + boundary events | `Application/Ports/SettlementContracts.cs` (398 LOC) | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` |
| 5 | Stable codes in Application, message-text mapper | `Application/Errors/*` | non-canonical mechanism (W1-owned) |
| 6 | Namespace-alias global usings | `Application/GlobalUsings.*.cs`, `Infrastructure/GlobalUsings.*.cs` | alias workaround (W1-owned) |
| 7 | Guard co-located with a 701 LOC directory | `Infrastructure/Directories/SettlementDirectory.cs` | `OVERSIZED_ONLY` |

Defects 1–7 were repaired by W1 (behavior-preserving). W2 owns the verification and the durable
locking of the resulting organization; see `physical-tree-after.md`.
