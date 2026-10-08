# Path-Namespace-State

**Verdict: `EXACT`**

## 1. Method

Script: `.git/pn-check-returns.js` (evidence script, kept out of the repository tree).

For every production `.cs` under `src/backend/Modules/Returns/<project>/…` the path-derived namespace
is computed as `Tooba.Returns.<Layer>` + the directory chain relative to the project root, and compared
for **exact** string equality with the `namespace` declaration in the file.

Locked exemptions (section 18): `Persistence/Migrations/`, `*.Designer.cs`, `*ModelSnapshot.cs`.

## 2. Result

```text
production .cs checked: 71
locked exemptions (Migrations/Designer/Snapshot): 5
mismatches: 0
```

| Project | Production `.cs` | Mismatches |
|---|---|---|
| `Tooba.Returns.Application` | 28 | 0 |
| `Tooba.Returns.Contracts` | 6 | 0 |
| `Tooba.Returns.Domain` | 9 | 0 |
| `Tooba.Returns.Endpoints` | 7 | 0 |
| `Tooba.Returns.Infrastructure` | 14 | 0 |
| `Tooba.Returns.Tests` | 7 | 0 |
| **Total** | **71** | **0** |

Exempted files (5): the three migration `.cs`, the `InitialReturns.Designer.cs` and the
`ReturnsDbContextModelSnapshot.cs`.

## 3. Alias / workaround check

| Check | Result |
|---|---|
| `namespace X = …;` alias directives used to hide folder debt | 0 |
| `global using` namespace re-mapping of module namespaces | 0 |
| `#pragma` namespace suppression | 0 |
| Files whose declared namespace disagrees with the folder but still compile | 0 |

## 4. Spot-check of the moved capability tree

| Path | Declared namespace |
|---|---|
| `Application/Composition/ReturnsOperation.cs` | `Tooba.Returns.Application.Composition` |
| `Application/ReturnRequests/Commands/CreateReturnCommand.cs` | `Tooba.Returns.Application.ReturnRequests.Commands` |
| `Application/ReturnRequests/Models/AdminReturnWorkQueueRow.cs` | `Tooba.Returns.Application.ReturnRequests.Models` |
| `Application/ReturnRequests/Ports/IReturnDirectory.cs` | `Tooba.Returns.Application.ReturnRequests.Ports` |
| `Application/ReturnRequests/Queries/QueryAdminReturnsGridQuery.cs` | `Tooba.Returns.Application.ReturnRequests.Queries` |
| `Application/Validation/ReturnsRequestValidators.cs` | `Tooba.Returns.Application.Validation` |

The W0 `Application.{Commands,Queries,Models,Ports}` namespaces no longer exist anywhere in the
module; the W1 rename propagated to every consumer and the module builds clean.

## 5. Enforcement

`ReturnsModuleAmsc001W2StructureGuardTests.Path_namespace_alignment_is_exact_for_every_production_source`
re-derives and re-asserts this matrix inside the test suite, so any future file dropped into the wrong
folder (or any alias workaround) fails CI.
