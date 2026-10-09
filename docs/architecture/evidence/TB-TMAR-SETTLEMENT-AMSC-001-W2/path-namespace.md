# Path ↔ namespace exactness (skill §18)

`Path-Namespace-State = EXACT`

## Method

Every production `.cs` under the five production projects was scanned for its declared namespace and
compared to the namespace derived from its physical path (`<Project>.<Relative.Folder>`), with the
locked EF exemptions applied (`Persistence/Migrations/*`, `*.Designer.cs`, `*ModelSnapshot.cs`) plus
`bin`/`obj`/`artifacts` exclusion.

```text
node .git/pn-check-settlement.js

production .cs checked: 66
locked exemptions (Migrations/Designer/Snapshot): 3
mismatches: 0
per project: {"Tooba.Settlement.Contracts":8,"Tooba.Settlement.Domain":14,
              "Tooba.Settlement.Application":21,"Tooba.Settlement.Infrastructure":18,
              "Tooba.Settlement.Endpoints":5}
```

## Spot-check of the moved W1 surface (highest-risk paths)

| File | Declared namespace | Expected |
|---|---|---|
| `Application/Composition/SettlementOperation.cs` | `Tooba.Settlement.Application.Composition` | same |
| `Application/Payouts/Commands/RequestSellerPayoutCommand.cs` | `Tooba.Settlement.Application.Payouts.Commands` | same |
| `Application/Payouts/Queries/GetSellerSettlementBalanceQuery.cs` | `Tooba.Settlement.Application.Payouts.Queries` | same |
| `Application/Payouts/Models/AdminPayoutModels.cs` | `Tooba.Settlement.Application.Payouts.Models` | same |
| `Application/Payouts/Ports/SettlementDirectoryPorts.cs` | `Tooba.Settlement.Application.Payouts.Ports` | same |
| `Application/Validation/SettlementRequestValidators.cs` | `Tooba.Settlement.Application.Validation` | same |
| `Contracts/Errors/SettlementErrorCodes.cs` | `Tooba.Settlement.Contracts.Errors` | same |
| `Contracts/Events/SettlementEntryPostedIntegrationEvent.cs` | `Tooba.Settlement.Contracts.Events` | same |
| `Infrastructure/Directories/OpenSettlementUseCaseGuard.cs` | `Tooba.Settlement.Infrastructure.Directories` | same |
| `Infrastructure/Queries/AdminPayoutGridQueryEngine.cs` | `Tooba.Settlement.Infrastructure.Queries` | same |
| `Endpoints/SettlementEndpointModule.cs` | `Tooba.Settlement.Endpoints` | same |
| `Endpoints/Admin/SettlementAdminEndpoints.cs` | `Tooba.Settlement.Endpoints.Admin` | same |

## Alias / shim proof

| Mechanism | Count |
|---|---|
| `GlobalUsings*.cs` files anywhere in the module | **0** (the four W0 files were retired by W1) |
| `TypeForwardedTo` | **0** |
| `using <Alias> = ...` namespace-alias workaround hiding folder debt | **0** |
| `global using` directives | **0** |
| Compatibility shim types | **0** |

The four retired files were `Application/GlobalUsings.Domain.cs`, `Application/GlobalUsings.Layout.cs`,
`Infrastructure/GlobalUsings.Domain.cs`, `Infrastructure/GlobalUsings.Layout.cs` — all added to the
manifest `forbiddenRootFiles` so they cannot return.

## Durable enforcement

- `SettlementModuleAmsc001W2StructureGuardTests.Path_namespace_alignment_is_exact_for_every_production_source`
- `SettlementModuleAmsc001W2StructureGuardTests.No_namespace_alias_workaround_or_global_using_file_remains`
