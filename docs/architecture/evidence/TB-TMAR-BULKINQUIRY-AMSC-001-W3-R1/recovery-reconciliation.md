# TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1 — Recovery Reconciliation

## Verdict

`BULKINQUIRY_AMSC_001_RECOVERY_RECONCILED`

Mode: `RECOVERY_SOT_EVIDENCE_RECONCILIATION_ONLY` — zero production change; W3 certification preserved.

## Starting HEAD

`67b5b55a2fecec33f3109b90252152875680edc0` (`HEAD == origin/main`).

Accepted lineage: W0 `7b89d81e` → W1 `9e9e37df` → W2 `82c2fa8e` → W3 `67b5b55a`.

## 1. The contradiction (before → after)

### Before (stale / false)

| Artifact | Claim |
|---|---|
| `docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W1/migrate.md` §2 | `Domain/…csproj` → Contracts reference **removed**; Domain → BuildingBlocks only |
| `docs/architecture/tmar-current-state.json` `bulkInquiryModuleAmsc001W1` | `"domainContractsReference": "REMOVED"` |

### Disk / commit reality

```
src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Domain/Tooba.BulkInquiry.Domain.csproj
  <ProjectReference Include="..\..\..\BuildingBlocks\Tooba.BuildingBlocks\Tooba.BuildingBlocks.csproj" />
  <ProjectReference Include="..\Tooba.BulkInquiry.Contracts\Tooba.BulkInquiry.Contracts.csproj" />
```

`Domain/Aggregates/BulkPurchaseInquiry.cs` throws `ContractOperationException(BulkInquiryErrorCodes.Rejected)`
via a private `Rejected()` helper — i.e. it genuinely consumes its **own** Contracts constant.

### Proof that W1 did not remove the reference

```
git show --name-only 9e9e37df | Select-String "Domain.csproj"
→ (no match)
git log --oneline -3 -- .../Tooba.BulkInquiry.Domain.csproj
→ 8fa9e736 fix(tmar): evacuate Host ProductQnA ...
→ 237bb503 feat complete Shopeiva PDP tabs ...
```

Commit `9e9e37df` touched 12 files (Domain aggregate, Directory, operation seam, validation codes,
validator, catalog contributor, error codes, 3 guards, 1 behavior test, W1 evidence) — **not** the Domain
csproj. The `REMOVED` claim was historically false.

### After (corrected)

- W1 evidence §2 now states the reference was **retained** (self-module layering, not cross-module
  coupling) and adds an explicit W3-R1 truth-correction note.
- SoT `bulkInquiryModuleAmsc001W1.domainContractsReference` =
  `PRESERVED_OWN_MODULE_CONTRACTS_REFERENCE_FOR_BulkInquiryErrorCodes_Rejected` plus a
  `domainContractsReferenceNote` explaining the reconciliation.
- W2/W3 accepted rule preserved verbatim: `BulkInquiry_domain_references_only_buildingblocks_and_own_contracts`.
- W3 `nonBlockingWatch` R2 remains the authoritative accepted layering explanation.

## 2. Current Domain reference allowlist

`Tooba.BulkInquiry.Domain.csproj` project references = **exactly two**:

1. `Tooba.BuildingBlocks`
2. `Tooba.BulkInquiry.Contracts` (own module)

Foreign Application / Infrastructure / Domain = **ZERO** (guarded).

## 3. Cross-module coupling proof

- Cross-module boundary remains `CONTRACTS_ONLY`: `Tooba.Catalog.Contracts.Ports.ICatalogReviewProductLookup`.
- Foreign App/Infra/Domain coupling = `ZERO`.
- Cross-module join = `ZERO`; cross-module persistence = `ZERO`.
- The Domain → own-Contracts reference is self-module layering, matching the established precedent
  (Cart / AddressBook / ProductQnA / PageComposition / UserPreference / OperatorProfile / Party /
  Localization / Wishlist / Identity).

## 4. Master Recovery W3 SHA checkpoint

`docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` BulkInquiry checkpoint lineage now records the W3 final
commit explicitly:

```
… -> `TB-TMAR-BULKINQUIRY-AMSC-001-W3` Certify `67b5b55a`.
```

A new module-local block `BulkInquiry AMSC W3-R1 recovery reconciliation` records the reconciliation,
certified commit, focused validation count and stop gate. The AMC-001 historical/superseded lineage is
preserved unchanged.

## 5. Focused validation count reconciliation

One deterministic focused run of the established test project/context:

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~BulkInquiry
→ Passed! - Failed: 0, Passed: 23, Skipped: 2, Total: 25
```

Metadata reconciled:

| Record | Before | After |
|---|---|---|
| `bulkInquiryModuleAmsc001W1.focusedValidation` | `14 passed` | `23 passed / 0 failed / 2 skipped` |
| `bulkInquiryModuleAmsc001W2.focusedValidation` | `19 passed` | `23 passed / 0 failed / 2 skipped` |
| `bulkInquiryModuleAmsc001W3.focusedValidation` | `19 passed` | `23 passed / 0 failed / 2 skipped` |
| W3 `certification.md` §22 | `24 passed` | `23 passed / 0 failed / 2 skipped` |

The 2 skips are Postgres Testcontainers-gated `ProductQnAAndBulkInquiryPostgresTests`. No BulkInquiry
guard failed.

## 6. Production change proof

`productionCodeChanged = false`. Zero changes to any BulkInquiry production file, csproj, route, DTO,
handler, validator, error-code value, descriptor, `.resx`, DI, solution/project structure, schema or
migration. Zero frontend changes. `automaticNextImplementationTask = NONE`.

## 7. Exact changed-file list

```
docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W1/migrate.md
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W3/certification.md
src/backend/Host/Tooba.Host.Tests/Architecture/BulkInquiryModuleAmsc001W3CertGuardTests.cs
docs/ai/tasks/TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1.task.md
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1/recovery-reconciliation.md
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1/validation.md
```

## 8. Global recovery lock

Repository-global Host recovery authority preserved: `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`,
`currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`, `nextHostFolder = null`, repository-global
`workflowStop` and `automaticNextImplementationTask` untouched. This task is module-local BulkInquiry
recovery reconciliation only.

## 9. Stop gate

`USER_REVIEW_BULKINQUIRY_AMSC_001_W3_R1`.
