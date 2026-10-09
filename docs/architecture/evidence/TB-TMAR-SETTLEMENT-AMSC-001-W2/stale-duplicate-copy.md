# Stale / duplicate physical copy (skill §20)

`Physical-Copy-State = CLEAN`

## Retired paths — verified absent on disk

### Application (all moved by W1; none re-created)

```text
Application/Errors/SettlementExceptionMapper.cs                       ABSENT
Application/Errors/SettlementErrorCodes.cs                            ABSENT
Application/GlobalUsings.Domain.cs                                    ABSENT
Application/GlobalUsings.Layout.cs                                    ABSENT
Application/Ports/SettlementContracts.cs                              ABSENT
Application/Ports/IAdminPayoutGridQuery.cs                            ABSENT (moved to Payouts/Ports)
Application/Models/SettlementAdminModels.cs                           ABSENT
Application/Validators/SettlementValidationCodes.cs                   ABSENT
Application/Validators/Seller/RequestSellerPayoutCommandValidator.cs  ABSENT
Application/Validators/Admin/ProcessAdminPayoutCommandValidator.cs    ABSENT
Application/Validators/Admin/RetryAdminPayoutCommandValidator.cs      ABSENT
Application/Validators/Admin/QueryAdminPayoutGridQueryValidator.cs    ABSENT
Application/Commands/RequestSellerPayout/RequestSellerPayoutCommand.cs  ABSENT
Application/Commands/ProcessAdminPayout/ProcessAdminPayoutCommand.cs    ABSENT
Application/Commands/RetryAdminPayout/RetryAdminPayoutCommand.cs        ABSENT
Application/Queries/GetSellerSettlementBalance/GetSellerSettlementBalanceQuery.cs   ABSENT
Application/Queries/ListSellerSettlementEntries/ListSellerSettlementEntriesQuery.cs ABSENT
Application/Queries/ListSellerSettlementStatements/ListSellerSettlementStatementsQuery.cs ABSENT
Application/Queries/ListSellerPayoutRequests/ListSellerPayoutRequestsQuery.cs       ABSENT
Application/Queries/ListAdminSettlementBalances/ListAdminSettlementBalancesQuery.cs ABSENT
Application/Queries/ListAdminPayoutQueue/ListAdminPayoutQueueQuery.cs               ABSENT
Application/Queries/QueryAdminPayoutGrid/QueryAdminPayoutGridQuery.cs               ABSENT
Application/Queries/QueryAdminPayoutGrid/AdminPayoutGridQueryPolicy.cs              ABSENT
```

The retired folder shells `Application/Commands`, `Application/Queries`, `Application/Models`,
`Application/Ports`, `Application/Validators`, `Application/Errors` are all **absent** (git does not
track empty directories; the durable guard asserts `Directory.Exists(...) == false`).

### Infrastructure

```text
Infrastructure/GlobalUsings.Domain.cs   ABSENT
Infrastructure/GlobalUsings.Layout.cs   ABSENT
```

## Duplicate live homes — verified single

| Responsibility | Expected | Actual |
|---|---|---|
| `SettlementErrorCodes.cs` | exactly one | `Contracts/Errors/SettlementErrorCodes.cs` (1) |
| `SettlementErrorResourceSet.cs` | exactly one | `Contracts/Errors/SettlementErrorResourceSet.cs` (1) |
| `SettlementOperation.cs` | exactly one | `Application/Composition/SettlementOperation.cs` (1) |
| `SettlementDirectory.cs` | exactly one | `Infrastructure/Directories/SettlementDirectory.cs` (1) |
| `SettlementDbContext.cs` | exactly one | `Infrastructure/Persistence/SettlementDbContext.cs` (1) |
| `AdminPayoutGridQueryEngine.cs` | exactly one | `Infrastructure/Queries/AdminPayoutGridQueryEngine.cs` (1) |
| `OpenSettlementUseCaseGuard.cs` | exactly one | `Infrastructure/Directories/OpenSettlementUseCaseGuard.cs` (1) |
| `SettlementRequestValidators.cs` | exactly one | `Application/Validation/SettlementRequestValidators.cs` (1) |
| `SettlementValidationCodes.cs` | exactly one | `Application/Validation/SettlementValidationCodes.cs` (1) |
| Each of the 10 CQRS request types | exactly one `.cs` | 10/10 single (`Payouts/{Commands,Queries}`) |

Enforced by
`SettlementModuleAmsc001W2StructureGuardTests.No_stale_or_duplicate_physical_copy_of_moved_files_remains`
and `…Duplicate_ownership_of_the_same_responsibility_does_not_exist`.

## Solution entries

Every `Project Path` in the `/Modules/Settlement/` solution folder resolves to a real `.csproj`; no
`.slnx` entry points at a deleted path.

## Migrations — byte-identical to the W0 baseline

```text
git diff bac4dbe3 4ca4aafc -- src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Persistence
(empty)
git diff 54b1c8ff 4ca4aafc -- .../Persistence/Migrations
(empty)
```

| File | State |
|---|---|
| `20260827030000_InitialSettlement.cs` | unchanged |
| `20260827030000_InitialSettlement.Designer.cs` | unchanged |
| `SettlementDbContextModelSnapshot.cs` | unchanged |

No migration was regenerated for the structural cleanup; the `settlement` schema, DbContext, inbox
records and outbox registration are untouched. (The `(empty)` diff output above is the authoritative
proof — an earlier `Get-FileHash` comparison was invalidated by PowerShell text-mode redirection and
was discarded rather than reported.)
