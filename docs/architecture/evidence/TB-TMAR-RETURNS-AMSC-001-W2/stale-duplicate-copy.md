# Physical-Copy-State

**Verdict: `CLEAN`** (one authoritative home per responsibility; no stale/duplicate live path)

## 1. Stale-path check

Every path that W1 retired was re-tested on disk at W2. **All absent:**

| Retired path | On disk |
|---|---|
| `Application/Errors/` (folder) | absent |
| `Application/Errors/ReturnsExceptionMapper.cs` | absent |
| `Application/Ports/ReturnSemanticMapper.cs` | absent |
| `Application/Commands/` (technical-axis root) | absent |
| `Application/Queries/` (technical-axis root) | absent |
| `Application/Models/` (technical-axis root) | absent |
| `Application/Ports/` (technical-axis root) | absent |
| `Application/Commands/CreateReturn/CreateReturnCommand.cs` | absent |
| `Application/Commands/ApproveReturn/ApproveReturnCommand.cs` | absent |
| `Application/Commands/RejectReturn/RejectReturnCommand.cs` | absent |
| `Application/Commands/RetryReturnRefund/RetryReturnRefundCommand.cs` | absent |
| `Application/Queries/GetAdminReturn/GetAdminReturnQuery.cs` | absent |
| `Application/Queries/GetCustomerReturn/GetCustomerReturnQuery.cs` | absent |
| `Application/Queries/GetSellerReturn/GetSellerReturnQuery.cs` | absent |
| `Application/Queries/ListAdminReturns/ListAdminReturnsQuery.cs` | absent |
| `Application/Queries/ListCustomerReturns/ListCustomerReturnsQuery.cs` | absent |
| `Application/Queries/ListSellerReturns/ListSellerReturnsQuery.cs` | absent |
| `Application/Queries/QueryAdminReturnsGrid/QueryAdminReturnsGridQuery.cs` | absent |
| `Application/Models/AdminReturnWorkQueueModels.cs` | absent (split) |
| `Application/Models/CreateReturnCommand.cs` (duplicate) | absent |
| `Application/Models/ApproveReturnCommand.cs` (duplicate) | absent |
| `Application/Models/RejectReturnCommand.cs` (duplicate) | absent |
| `Application/Models/RetryRefundCommand.cs` (duplicate) | absent |

`git diff f5c5a6db 0a573864` records these as deletions (R-renames where content was preserved), so
no leftover path can silently keep the same types alive.

## 2. Duplicate-live-home check

A module-wide type-name scan reports the following name repeats. Each was inspected individually:

| Type name | Homes | Verdict |
|---|---|---|
| `ReturnSnapshot` | `Application/ReturnRequests/Models/ReturnSnapshot.cs` (Domain-typed read model: `ReturnRequestStatus`, `RefundDestination`) vs `Contracts/Operations/ReturnAdminOperationsContracts.cs` (wire projection: `ReturnRequestOperationStatus`, `RefundDestinationOption`) | **NOT a duplicate copy.** Different namespaces, different types, different roles. The boundary port `IReturnAdminOperations` must expose boundary-typed DTOs (Contracts-only rule), so the Contracts records are required. `ReturnAdminOperationsAdapter.Map(AppModels.ReturnSnapshot)` is the explicit translation seam. `ReturnDirectory` returns the Application-local read model. |
| `ReturnItemSnapshot` | same split | NOT a duplicate copy |
| `RefundAttemptSnapshot` | same split | NOT a duplicate copy |
| `ReturnLineEligibility` | same split | NOT a duplicate copy |
| `ReturnEligibilityResult` | same split | NOT a duplicate copy |
| `ReturnLineCommand` | same split | NOT a duplicate copy |
| `ReturnStatusOverlayRow` | `Contracts/Operations/ReturnAdminOperationsContracts.cs` (wire) vs `Application/ReturnRequests/Ports/IReturnDirectory.cs` (internal port row) | NOT a duplicate copy |

The pre-W1 duplicates that **were** true duplicate copies — `Application/Models/{CreateReturnCommand,
ApproveReturnCommand,RejectReturnCommand,RetryRefundCommand}.cs`, which mirrored the MediatR requests
1:1 in the same assembly and the same namespace family — are deleted and are listed as
`forbiddenRootFiles`/absent above.

## 3. Solution-entry staleness

| Check | Result |
|---|---|
| `.slnx` entries pointing at deleted paths | 0 |
| Project references to removed folders | 0 |
| `csproj` `<Compile Remove …>` patches hiding stale files | 0 |
| `bin`/`obj` leftovers tracked in git | 0 |

## 4. Reachability of moved files

Every file moved by W1 is reachable and compiled:

- the module builds clean (all six projects);
- `Application/ReturnRequests/Models` is imported by 40+ files across Infrastructure, Endpoints,
  Host tests and the module's own Tests project (verified by `rg`);
- `Tooba.Returns.Tests` characterization + endpoint-ownership suites pass against the new layout.

## 5. Enforcement

`ReturnsModuleAmsc001W2StructureGuardTests.No_stale_or_duplicate_physical_copy_of_moved_files_remains`
asserts the 13 highest-risk retired paths stay absent. Combined with the exact path↔namespace guard
(0 mismatches) and the build, a resurrected stale copy would fail CI immediately.
