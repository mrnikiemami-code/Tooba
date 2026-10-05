# TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1 — Validation

## 1. JSON parse

```
docs/architecture/tmar-current-state.json → PARSE PASS (no duplicate properties)
```

## 2. Focused BulkInquiry run (deterministic)

Two deterministic runs of the same focused command bracket the R1 guard addition:

```
# (a) baseline on the certified HEAD 67b5b55a, before the R1 guard fact was added
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~BulkInquiry
→ Passed! - Failed: 0, Passed: 23, Skipped: 2, Total: 25 (net8.0)

# (b) final R1 tree, after the R1 reconciliation guard fact was added
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~BulkInquiry
→ Passed! - Failed: 0, Passed: 24, Skipped: 2, Total: 26 (net8.0)
```

Skips = `ProductQnAAndBulkInquiryPostgresTests` (Postgres Testcontainers). Required BulkInquiry guards
(`BulkInquiryModuleAmsc001W3CertGuardTests`, `…W2StructureGuardTests`, `BulkInquiryModuleAmcW1SolutionGuardTests`,
`…W2StructureGuardTests`, `…W3CqrsGuardTests`, `…W4CertGuardTests`) all PASS in both runs.

The **23** is the count at the reconciled certified HEAD and matches the external worker summary; the **24**
is the same focused set after R1 itself added the durable
`BulkInquiry_amsc_w3r1_recovery_reconciliation_truth_is_locked` fact. Historical W1/W2/W3 metadata is
reconciled to the certified-HEAD count (23); the R1 final count is 24.

## 3. W3 cert guard

`BulkInquiryModuleAmsc001W3CertGuardTests` → PASS (existing 4 facts + 1 new R1 reconciliation fact).

## 4. Master Recovery W3 SHA proof

```
Select-String docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md -Pattern "Certify `67b5b55a`"
→ match (BulkInquiry AMSC lineage)
```

## 5. Git diff scope proof

```
git diff --name-only
→ docs/architecture/tmar-current-state.json
→ docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
→ docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W1/migrate.md
→ docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W3/certification.md
→ src/backend/Host/Tooba.Host.Tests/Architecture/BulkInquiryModuleAmsc001W3CertGuardTests.cs
```

Zero production files, zero csproj, zero schema/migration, zero frontend, zero other modules.

## 6. Boundary / coupling proof

```
grep Domain.csproj → BuildingBlocks + own BulkInquiry.Contracts only
grep foreign module names in Domain.csproj → ZERO
```

## 7. HEAD / working tree

`HEAD == origin/main`. Unrelated pre-existing untracked evidence artifacts preserved, not touched.
