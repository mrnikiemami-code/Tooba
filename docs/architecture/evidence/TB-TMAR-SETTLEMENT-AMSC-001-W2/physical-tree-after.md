# Physical tree — AFTER (W2 verified state, `4ca4aafc` + this wave)

Enumerated from disk at the W2 baseline; production `.cs` only, `bin/obj/artifacts` excluded.
Counts: Contracts 8 · Domain 14 · Application 21 · Infrastructure 18 · Endpoints 5 = **66 production
`.cs`** (+3 locked EF exemptions: 1 migration + 1 designer + 1 model snapshot).

## Tooba.Settlement.Contracts (8)

```text
Contracts/
  Errors/SettlementErrorCodes.cs                  <-- single canonical stable-code home (17 HTTP + 1 platform)
  Errors/SettlementErrorResourceSet.cs
  Events/PayoutFailedIntegrationEvent.cs          <-- published wire contract (payout.failed.v1)
  Events/PayoutSucceededIntegrationEvent.cs       <-- published wire contract (payout.succeeded.v1)
  Events/SettlementEntryPostedIntegrationEvent.cs <-- published wire contract (settlement.entry.posted.v1)
  History/SettlementHistoryContracts.cs
  Operations/SettlementAdminOrderDetailContracts.cs
  Operations/SettlementOrderAccrualContracts.cs
Resources/SettlementErrors.resx                   <-- embedded, non-.cs
Resources/SettlementErrors.fa.resx                <-- embedded, non-.cs
```

`Capability-foldered boundary vocabulary only`: no CQRS request, no Application dump, no root `.cs`.
Root allowlist `[]`.

## Tooba.Settlement.Domain (14)

```text
Domain/
  Aggregates/{PayoutAttempt,PayoutRequest,SellerPayoutProfile,SettlementAccount,SettlementEntry,SettlementStatement}.cs
  Entities/CommissionPolicy.cs
  Events/{PayoutFailedDomainEvent,PayoutSucceededDomainEvent,SettlementEntryPostedDomainEvent}.cs
  ValueObjects/{CommissionPolicySnapshot,EntryType,PayoutStatus,StatementStatus}.cs
```

No root god-file; invariants raise `SettlementErrorCodes` constants (single self-module Contracts edge).

## Tooba.Settlement.Application (21) — `PROFESSIONAL_SHALLOW` capability-first

```text
Application/
  Payouts/
    Commands/
      ProcessAdminPayoutCommand.cs
      RequestSellerPayoutCommand.cs
      RetryAdminPayoutCommand.cs
    Queries/
      AdminPayoutGridQueryPolicy.cs
      GetSellerSettlementBalanceQuery.cs
      ListAdminPayoutQueueQuery.cs
      ListAdminSettlementBalancesQuery.cs
      ListSellerPayoutRequestsQuery.cs
      ListSellerSettlementEntriesQuery.cs
      ListSellerSettlementStatementsQuery.cs
      QueryAdminPayoutGridQuery.cs
    Models/
      AdminPayoutModels.cs
      SettlementDisplayLabels.cs
    Ports/
      IAdminPayoutGridQuery.cs
      SettlementDirectoryPorts.cs
      SettlementGatewayPorts.cs
      SettlementReaderPorts.cs
      SettlementRestorePolicy.cs
  Composition/
    SettlementOperation.cs                        <-- single typed-fault seam
  Validation/
    SettlementRequestValidators.cs
    SettlementValidationCodes.cs
```

Top-level folders: `Composition`, `Payouts`, `Validation` — exactly three, all justified.
`Commands/` and `Queries/` contain **zero** subfolders (no single-file use-case leaves).

## Tooba.Settlement.Infrastructure (18) — capability/integration folders

```text
Infrastructure/
  Adapters/{SettlementAdminOrderDetailReader,SettlementHistoryReader,SettlementOrderAccrualAdapter}.cs
  Bridges/{SettlementOrderBridge,SettlementPaymentBridge,SettlementReturnsBridge}.cs
  DependencyInjection/SettlementModule.cs
  Directories/{OpenSettlementUseCaseGuard,SettlementDirectory}.cs   <-- guard extracted (W1)
  Errors/SettlementErrorCatalogContributor.cs
  Gateways/{FailClosedPayoutGateway,FakePayoutGateway}.cs
  Handlers/SettlementEventHandlers.cs
  Messaging/SettlementOutboxRegistration.cs
  Observability/SettlementInstrumentation.cs
  Persistence/{SettlementDbContext,SettlementInboxRecords}.cs
  Persistence/Migrations/20260827030000_InitialSettlement.cs
  Persistence/Migrations/20260827030000_InitialSettlement.Designer.cs
  Persistence/Migrations/SettlementDbContextModelSnapshot.cs
  Queries/AdminPayoutGridQueryEngine.cs
```

Persistence is foldered; migrations live only under `Persistence/Migrations`. Root allowlist `[]`.

## Tooba.Settlement.Endpoints (5) — audience capability folders

```text
Endpoints/
  SettlementEndpointModule.cs                       <-- composition entry at root (allowlisted)
  Admin/{ISettlementAdminAuthorizer,SettlementAdminEndpoints}.cs
  Seller/{ISettlementSellerAuthorizer,SettlementSellerEndpoints}.cs
```

## Tooba.Settlement.Tests

```text
Tests/
  Architecture/{SettlementArchitectureGuardTests,SettlementValidatorCoverageGuardTests}.cs
  Behavior/SettlementErrorAndGridTests.cs
  Endpoints/SettlementEndpointOwnershipTests.cs
  Validation/SettlementValidatorTests.cs
```

## Delta versus `physical-tree-before.md`

| Dimension | Before | After |
|---|---|---|
| Application top-level folders | `Commands, Queries, Models, Ports, Validators, Errors` + 2 `GlobalUsings` | `Composition, Payouts, Validation` |
| Use-case leaf folders | 10 | 0 |
| Boundary events home | `Application/Ports/SettlementContracts.cs` | `Contracts/Events/` |
| Stable codes home | `Application/Errors/` | `Contracts/Errors/` |
| Message-text mapper | `Application/Errors/SettlementExceptionMapper.cs` | retired → `Application/Composition/SettlementOperation.cs` |
| Alias global usings | 4 files | 0 files |
| Infrastructure root `.cs` | 2 `GlobalUsings` | 0 |
| Project count | 6 (Contracts, Domain, Application, Infrastructure, Endpoints, Tests) | 6 (unchanged) |
| Migrations | 1 + designer + snapshot | identical (byte-for-byte, see `stale-duplicate-copy.md`) |
