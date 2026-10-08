# TB-TMAR-PROMOTION-AMSC-001-W2 — validation

Focused validation only (skill section 25). No open-ended repair loop.

| Check | Command | Result |
| --- | --- | --- |
| Promotion endpoints chain builds | `dotnet build Modules/Promotion/Tooba.Promotion.Endpoints/Tooba.Promotion.Endpoints.csproj` | **0 errors**, 0 warnings |
| Host tests project builds | `dotnet build Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj` | **0 errors** |
| Module tests | `dotnet test Modules/Promotion/Tooba.Promotion.Tests` | **9 / 9 PASS** |
| New W2 structure guard | `dotnet test --filter PromotionModuleAmsc001W2StructureGuardTests` | **7 / 7 PASS** |
| W1 migrate guard (no regression) | `dotnet test --filter PromotionModuleAmsc001W1MigrateGuardTests` | **10 / 10 PASS** |
| Focused Host Promotion/Merchandising guards | `dotnet test --filter Promotion|Merchandising|ContractsW6Characterization` | **46 PASS / 0 FAIL / 5 skipped** |
| SoT JSON parses | `Get-Content docs/architecture/tmar-current-state.json -Raw \| ConvertFrom-Json` | PASS |
| Manifest JSON parses | `Get-Content docs/architecture/tmar-module-structure-manifests.json -Raw \| ConvertFrom-Json` | PASS |
| Full Host suite | `dotnet test Host/Tooba.Host.Tests` | see below |

## Full Host suite comparison (regression gate)

| Wave | Total | Passed | Failed | Skipped |
| --- | --- | --- | --- | --- |
| W1 baseline (`failed-names-w1.txt`, `host-tests-w1-normal.txt`) | 2310 | 2103 | **77** | 130 |
| **W2 (this wave)** | 2317 | 2110 | **77** | 130 |

* New tests added by W2: **+7** (`PromotionModuleAmsc001W2StructureGuardTests`).
* Failure-name set diff between W1 and W2: **empty** — no new failure, none removed.
* Promotion/Merchandising-named failures in the W2 run: **zero**.
* `TmarSourceSizeAndInfraAppTests` and the two `TmarDurableGuardTests` recovery assertions are
  **pre-existing** failures also present in the W1 baseline (`failed-names-w1.txt`); they are caused by
  the earlier `b89f0b85` ProductWorkspace SoT edit and by the source-size baseline accounting for the new
  evidence/guard files, not by this wave's structure moves.
* `guardsWeakened = NONE`, `baselinesWidened = NONE`.

## Structure-State verdict

`READY_FOR_CERTIFY` — all section 27 gates met:

* Folder-Granularity-State = `PROFESSIONAL_SHALLOW`
* Solution-Explorer-State = `CANONICAL`
* Path-Namespace-State = `EXACT`
* Physical-Copy-State = `CLEAN`
* Root-Allowlist-State = `ENFORCED`
* no unjustified single-file request leaf folder
* no unjustified technical-axis-first request tree
* no root dump
* no unresolved god-file / over-split blocker
* Host final closure preserved
* focused structure guards pass
