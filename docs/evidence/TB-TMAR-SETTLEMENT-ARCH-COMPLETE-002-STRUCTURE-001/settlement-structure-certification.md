# TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001 — Settlement structure certification

## Accepted parent

- Parent task: `TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001` — ARCHITECT-ACCEPTED.
- Accepted commits:
  - validator implementation: `0cc4b52d59f0fa124c9844efbabd0d079df8c16e`
  - focused architecture-folder allowance follow-up: `a1d5f5bd52ec12fdbf79b268dc9205ab38eb65b1`
- Parent state preserved: Settlement = `COMPLETE_REFERENCE_PATTERN` / `HTTP_OWNING` / `MODULE_ENDPOINTS` / `MEDIATR_12_5`;
  10 endpoint-reachable requests; 4 `VALIDATOR_REQUIRED` with 4 present; 6 `NO_VALIDATOR_REQUIRED`; central
  `AddToobaCqrsFoundation`/`AddValidatorsFromAssembly` discovery proven; no direct validator invocation;
  `Settlement -> Host = ZERO`; two approved thin Host security adapters; no path/namespace mismatch; no alias debt.

## A. Exact live folder set

`Tooba.Settlement.Domain` (already capability-grouped; untouched):
`Aggregates`, `Entities`, `Events`, `ValueObjects`.

`Tooba.Settlement.Contracts` (untouched): `History`, `Operations`.

`Tooba.Settlement.Application`:
`Commands`, `Queries`, `Errors`, `Models`, `Ports`, `Validators`
Root `.cs`: `GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs`.

`Tooba.Settlement.Endpoints`:
`Admin`, `Seller`
Root `.cs`: `SettlementEndpointModule.cs`.

`Tooba.Settlement.Infrastructure`:
`Adapters`, `Bridges`, `DependencyInjection`, `Directories`, `Errors`, `Gateways`, `Handlers`, `Messaging`, `Observability`, `Persistence`, `Queries`
Root `.cs`: `GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs`.

No ceremonial folder was created and no already-conforming file was moved. Zero production `.cs` was added,
deleted or relocated by this task.

## B. Exact path <-> namespace guard

`SettlementArchitectureGuardTests.AssertNamespacesAlign` was replaced: the old loose
`StartsWith(nsPrefix)` acceptance is gone. The guard now derives the expected namespace from the physical path
(`nsPrefix` + dot-joined relative directory) and asserts **exact equality** for every Settlement production `.cs`
file across `Domain`, `Contracts`, `Application`, `Infrastructure`, `Endpoints`.

Exemptions (unchanged, legitimate):
- `GlobalUsings*.cs` — pure import aggregation, covered by the dedicated global-using guard.
- `Infrastructure/Persistence/Migrations/*` and `*ModelSnapshot.cs` — EF-generated migrations namespace.

Result: `PATH_NAMESPACE = EXACT`. No real mismatch was found, so no repair was performed.

## C. Explicit root allowlists / forbidden flattened files

New guard: `Settlement_root_allowlists_and_forbidden_flattened_files_are_enforced`.

| Project | rootAllowlist (asserted exactly) | forbiddenRootFiles |
| --- | --- | --- |
| `Tooba.Settlement.Application` | `GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs` | `SettlementContracts.cs`, `SettlementCommands.cs`, `SettlementQueries.cs`, `SettlementHandlers.cs`, `SettlementErrorCodes.cs`, `SettlementAdminModels.cs`, `RequestSellerPayoutCommand.cs`, `QueryAdminPayoutGridQuery.cs` |
| `Tooba.Settlement.Endpoints` | `SettlementEndpointModule.cs` | `SettlementSellerEndpoints.cs`, `SettlementAdminEndpoints.cs`, `ISettlementSellerAuthorizer.cs`, `ISettlementAdminAuthorizer.cs` |
| `Tooba.Settlement.Infrastructure` | `GlobalUsings.Domain.cs`, `GlobalUsings.Layout.cs` | `SettlementModule.cs`, `SettlementDbContext.cs`, `SettlementDirectory.cs`, `SettlementOutboxRegistration.cs`, `SettlementEventHandlers.cs`, `AdminPayoutGridQueryEngine.cs` |

Both the allowlist equality and the forbidden-file absence are asserted, so future capability files cannot be
flattened into root silently.

## D. GlobalUsings / alias-workaround guard

Approved GlobalUsings (pinned exactly by `Settlement_global_usings_are_approved_project_wide_imports_only`):

- `Application/GlobalUsings.Domain.cs`: `Tooba.Settlement.Domain.{Aggregates,Entities,Events,ValueObjects}`
- `Application/GlobalUsings.Layout.cs`: `Tooba.Settlement.Application.Ports`
- `Infrastructure/GlobalUsings.Domain.cs`: `Tooba.Settlement.Domain.{Aggregates,Entities,Events,ValueObjects}`
- `Infrastructure/GlobalUsings.Layout.cs`: `Tooba.Settlement.Application.Ports`, `Tooba.Settlement.Infrastructure.{Directories,DependencyInjection,Messaging,Handlers,Observability,Bridges,Gateways}`

Justification: these are genuine project-wide Settlement imports only; no foreign-module
Application/Infrastructure/Domain coupling and no alias workaround. No new GlobalUsings was added.

`Settlement_rejects_namespace_alias_workarounds_and_foreign_global_aliases` rejects:
- alias assignments inside GlobalUsings files (`=` rejected),
- foreign-module global aliases (`Tooba.<Other>.*Application|Infrastructure|Domain`),
- compatibility shims that re-declare a flattened Settlement namespace over a capability-folder path,
- `TypeForwardedTo`.

Result: `Alias-Workaround = NONE`.

## E. Validator coverage (certification prerequisite)

Reused and preserved `SettlementValidatorCoverageGuardTests` (its final assertion was flipped from
"still not certified" to "structure certified").

- Endpoint-reachable requests = 10; all real `IRequest`; real handlers; all dispatched via `ISender`;
  no `IValidator`/`ValidateAsync` in endpoints; MediatR = 12.5.0.
- `VALIDATOR_REQUIRED = 4`: `RequestSellerPayoutCommand`, `ProcessAdminPayoutCommand`,
  `RetryAdminPayoutCommand`, `QueryAdminPayoutGridQuery` — all 4 present and resolvable to their exact concrete
  validators via foundation DI.
- `NO_VALIDATOR_REQUIRED = 6`: 4 `AUTH_SCOPED_QUERY` + 2 `NO_INPUT` — none registered.
- No ceremonial validator was created by this task.

## F. Host ownership lock

New guard: `Settlement_host_residue_is_exactly_two_thin_security_adapters`.

- Only Host Settlement security artifacts: `Admin/HostSettlementAdminAuthorizer.cs`, `Seller/HostSettlementSellerAuthorizer.cs`.
- No Host `Settlement/` folder, no Host Settlement grid engine, no Host Settlement panel composer,
  no Settlement business service/runtime owner in Host.
- `SettlementDbContext` in Host remains limited to the already accepted migration/dev allowlist
  (`Program.cs`, `ToobaModuleComposition.cs`, `ModuleMigrationRegistry.cs`, `MarketplaceDevelopmentBootstrap.cs`).
- Settlement project references contain no `Tooba.Host`; Settlement production sources contain no `Tooba.Host`.
  `Settlement -> Host = ZERO`.

The two approved security adapters were not moved or deleted.

## G. Cross-module boundary state

Unchanged and preserved (no boundary redesign): `Domain` has no foreign-module dependency; `Application` holds
`Domain` + approved Contracts-only boundaries and no foreign Application/Infrastructure/Domain or Host;
`Infrastructure` consumes approved Contracts-only dependencies (Order/Payment/Returns/Party Contracts) with no
foreign Application/Domain/Infrastructure or DbContext; `Endpoints` hold `Application` + BuildingBlocks only,
no `Infrastructure`, no Host, no DbContext. No schema or migration change.

## H. Manifest change

`docs/architecture/tmar-module-structure-manifests.json`:

- Added Settlement module: `structureCertified = true`, `lockVersion = ARCH-COMPLETE-002`, with the three
  projects above, their `rootAllowlist`, `rootAllowlistJustification` (Application + Infrastructure only) and
  `forbiddenRootFiles`, `forbiddenTopLevelFolders = []`.
- Removed `Settlement` from `uncertifiedHttpOwningModules`.
- No other module entry was altered.

## I. SoT certification closure

Updated: `tmar-current-state.json`, `TOOBA-TMAR-MASTER-RECOVERY.md`, `TOOBA-ARCHITECT-BOOTSTRAP.md`,
`TmarDurableGuardTests.cs`, `TmarCompleteReferenceStructureGateTests.cs`.

Recorded Settlement state = `COMPLETE_REFERENCE_PATTERN`, `httpApplicability = HTTP_OWNING`,
`endpointOwnership = MODULE_ENDPOINTS`, `cqrs = MEDIATR_12_5`,
`structureCertifiedUnderArchComplete002 = true`,
`validatorCoverage = COMPLETE_4_OF_4_REQUIRED_PRESENT_6_NO_VALIDATOR_REQUIRED`, `endpointReachableRequests = 10`,
`workerInternalRequests = 0`, `pathNamespace = EXACT`, `rootAllowlist = ENFORCED`, `aliasWorkaround = NONE`,
`hostResidue = TWO_THIN_HOST_SECURITY_ADAPTERS_ONLY`, `settlementToHostDependency = ZERO`, `manifestCertified = true`.

Certified set (`structureLock.certifiedModules`): `Order, Cart, StoreContext, Offer, Payment, Settlement`.

`nextTask = USER_REVIEW_SETTLEMENT_ARCH_COMPLETE_002_STRUCTURE_001`,
`nextTaskGate = USER_REVIEW_REQUIRED_AFTER_SETTLEMENT_STRUCTURE_CERTIFICATION`. No further module was auto-selected.

Preserved: `Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT`, `frontendFrozen = true`.

## J. Focused validation results

| Check | Result |
| --- | --- |
| `SettlementArchitectureGuardTests` | PASS (7) |
| `SettlementValidatorCoverageGuardTests` | PASS |
| `TmarCompleteReferenceStructureGateTests` | PASS |
| `TmarDurableGuardTests` | PASS |
| `dotnet build Tooba.Settlement.Tests.csproj --no-restore` | PASS |
| `dotnet build Tooba.Host.Tests.csproj --no-restore` | PASS |

No full Settlement/Host suite, broad TMAR suite, solution test, solution build, Testcontainers or DB integration
test was run.

## K. Checkout / frontend preservation

Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`; frontend production changes = none; `frontendFrozen = true`.
