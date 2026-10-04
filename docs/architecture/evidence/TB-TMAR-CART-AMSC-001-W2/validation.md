# TB-TMAR-CART-AMSC-001-W2 — Validation

## 1. Environment

| Item | Value |
| --- | --- |
| Branch | `main` |
| HEAD at start | `a5ddc052` (Cart W1) |
| `origin/main` at start | `a5ddc052` |
| HEAD == origin/main | ✅ |
| Solution | `src/backend/Tooba.slnx` |
| SDK | .NET 8 |

## 2. Build

```text
dotnet build src/backend/Tooba.slnx -v q --nologo
→ Build succeeded. 0 Error(s)
```

## 3. Cart test suite (authoritative gate)

```text
dotnet test src/backend/Modules/Cart/Tooba.Cart.Tests/Tooba.Cart.Tests.csproj -v q --nologo
→ Passed!  - Failed: 0, Passed: 27, Skipped: 0, Total: 27
```

| Wave | Result |
| --- | --- |
| W0 baseline | Failed 1, Passed 22, Total 23 |
| W1 after | Failed 0, Passed 24, Total 24 |
| **W2 after** | Failed **0**, Passed **27**, Total 27 |

Delta: `+3` new durable structure/size guards
(`Cart_application_is_capability_first_with_no_technical_axis_root`,
`Cart_has_no_single_file_use_case_leaf_folders`,
`Cart_directory_stays_under_the_arch_size_ceiling`).

## 4. Host suite (regression proof)

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -v q --nologo
→ Failed: 91, Passed: 1761, Skipped: 130, Total: 1982
```

`Compare-Object` against the pre-change baseline failing-test name set returned **no differences**.

The one Host guard that the move turned red (`HostCartResidualGuardTests.CreateGuestCart_uses_commerce_context_without_hardcoded_values`,
whose path literal pointed at `Commands/CreateGuestCart/`) was retargeted to the new physical path; its
assertions were not touched.

## 5. Structural scans (post-change)

| Scan | Result |
| --- | --- |
| `Application/Commands/` at the Application root | **absent** |
| `Application/Queries/` at the Application root | **absent** |
| `Application/Models/` | **absent** |
| `Application/Carts/{Commands,Queries,Validators}` | **present**, 5 / 2 / 4 files |
| Directories under `Carts/` other than the 3 axes | **0** |
| Leaf folders under any `Carts/` axis | **0** |
| Single-file use-case leaf folders in Application | **0** |
| Stale `Tooba.Cart.Application.Commands.*` / `.Queries.*` namespace references | **0** |
| Stale `Tooba.Cart.Application.Models` references | **0** |
| `Directory.Build` / csproj explicit file lists requiring update | **0** (SDK-style globbing) |
| `Tooba.slnx` `/Modules/Cart/` grouping | already canonical, unchanged |

## 6. Path ↔ namespace exactness

Every moved file's namespace mirrors its folder path:

```text
Application/Carts/Commands/AddCartLineCommand.cs                    → Tooba.Cart.Application.Carts.Commands
Application/Carts/Commands/ChangeCartLineQuantityCommand.cs         → Tooba.Cart.Application.Carts.Commands
Application/Carts/Commands/CreateGuestCartCommand.cs                → Tooba.Cart.Application.Carts.Commands
Application/Carts/Commands/MergeCartAfterLoginCommand.cs            → Tooba.Cart.Application.Carts.Commands
Application/Carts/Commands/RemoveCartLineCommand.cs                 → Tooba.Cart.Application.Carts.Commands
Application/Carts/Queries/GetCartQuery.cs                           → Tooba.Cart.Application.Carts.Queries
Application/Carts/Queries/GetCurrentAuthenticatedCartQuery.cs       → Tooba.Cart.Application.Carts.Queries
Application/Carts/Validators/AddCartLineCommandValidator.cs         → Tooba.Cart.Application.Carts.Validators
Application/Carts/Validators/ChangeCartLineQuantityCommandValidator.cs → Tooba.Cart.Application.Carts.Validators
Application/Carts/Validators/GetCartQueryValidator.cs               → Tooba.Cart.Application.Carts.Validators
Application/Carts/Validators/RemoveCartLineCommandValidator.cs      → Tooba.Cart.Application.Carts.Validators
```

`AssertNamespacesAlign("Tooba.Cart.Application", "Tooba.Cart.Application")` is GREEN with the
strengthened full-path comparison.

## 7. Manifest reconciliation

`docs/architecture/tmar-module-structure-manifests.json` parses (`node JSON.parse` OK) and now lists
all 5 Cart production projects. `Tooba.Cart.Application.forbiddenTopLevelFolders = ["Commands",
"Queries", "Models"]`. No module list membership changed.

`docs/architecture/tmar-current-state.json` parses and gains `cartModuleAmsc001W2`.

## 8. Wave gate

| Wave | Gate | State |
| --- | --- | --- |
| W0 | evidence, SoT checkpoint, commit + push | ✅ `f1d98ec2` |
| W1 | typed faults, contract-sourced codes, size decomposition, commit + push | ✅ `a5ddc052` |
| **W2 (this)** | capability-first Application, no tombstone, manifest locks all 5 projects, guards green, commit + push | ✅ |
| W3 | `Cart` re-certified `COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002`; durable cert guard green; SoT closure; commit + push | ⏳ |

## 9. Recovery status

`HEAD == origin/main` at wave start. The three pre-existing untracked foreign artifacts (W0 F15) remain
untracked and are **not** committed by this run.

No `RECOVERY_CONFLICT`.
