# TB-TMAR-CONTENT-AMSC-001-W3-R1 — Validation

## 1. JSON parse

```
docs/architecture/tmar-current-state.json → PARSE PASS (no duplicate properties)
```

## 2. Focused Content run (deterministic)

Two deterministic runs of the same focused command bracket the R1 guard addition:

```
# (a) baseline on the certified HEAD c012345d, before the R1 guard fact was added
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~Content
→ Passed! - Failed: 0, Passed: 66, Skipped: 14, Total: 80 (net8.0)

# (b) final R1 tree, after the R1 reconciliation guard fact was added
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~Content
→ Passed! - Failed: 0, Passed: 67, Skipped: 14, Total: 81 (net8.0)
```

Skips = Postgres Testcontainers-gated Content integration tests. Required Content guards
(`ContentModuleAmsc001W3CertGuardTests`, `ContentModuleAmsc001W2StructureGuardTests`,
`HostContentAmcR1GuardTests`, `…R2`, `…R3`, `…R4`) all PASS in both runs.

The **66** is the count at the reconciled certified HEAD and matches the current certification
convention; the **67** is the same focused set after R1 itself added the durable
`Content_amsc_w3r1_recovery_reconciliation_truth_is_locked` fact. Historical W3 metadata is
reconciled to the certified-HEAD count (66); the R1 final count is 67.

## 3. W3 cert guard

`ContentModuleAmsc001W3CertGuardTests` → PASS (existing 4 facts + 1 new R1 reconciliation fact = 5).

## 4. Master Recovery W3 SHA proof

```
Select-String docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md -Pattern "Certify `c012345d`"
→ match (Content AMSC lineage)
```

## 5. Git diff scope proof

```
git diff --name-only
→ docs/architecture/tmar-current-state.json
→ docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
→ docs/architecture/evidence/TB-TMAR-CONTENT-AMSC-001-W3/certification.md
→ src/backend/Host/Tooba.Host.Tests/Architecture/ContentModuleAmsc001W3CertGuardTests.cs
→ docs/ai/tasks/TB-TMAR-CONTENT-AMSC-001-W3-R1.task.md
→ docs/architecture/evidence/TB-TMAR-CONTENT-AMSC-001-W3-R1/recovery-reconciliation.md
→ docs/architecture/evidence/TB-TMAR-CONTENT-AMSC-001-W3-R1/validation.md
```

Zero Content production files, zero csproj, zero schema/migration, zero frontend, zero other modules,
zero manifest structural change.

## 6. Boundary / coupling proof

```
Content Endpoints csproj → no Tooba.Content.Infrastructure / Tooba.Content.Domain
Content Contracts        → no Tooba.Content.Domain / Tooba.Content.Application
Content production       → zero ContractOperationException("<literal>") hard-coded fault text
ContentErrorCodes        → 76 declared = 76 registered descriptors = 76 en/fa resource keys
```

Cross-module boundary remains `CONTRACTS_ONLY` (`Localization.Contracts`, `Media.Contracts`); foreign
App/Infra/Domain coupling `ZERO`; cross-module join `ZERO`; cross-module persistence `ZERO`.

## 7. HEAD / working tree

`HEAD == origin/main`. Unrelated pre-existing untracked evidence artifacts preserved, not touched.
