# TB-TMAR-PRICING-AMSC-001-W3-R4 — validation

- Module: `Pricing`
- Starting head: `127c596aba7abefe6bdd0a1edb7c69c064267762` (branch `main`)
- Observed `HEAD == origin/main` at wake: `549f1ac535e37e4e8c9e3c44b88768c575dfeb5e` (the task's expected
  head plus one evidence-only hop; see `lineage-reconciliation.md` §4)

## 1. Change surface

```text
M  docs/architecture/tmar-current-state.json
M  docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
M  src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W3R3CertGuardTests.cs
A  docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R4/*
A  docs/ai/tasks/TB-TMAR-PRICING-AMSC-001-W3-R4.task.md
```

`git diff --numstat docs/architecture/tmar-current-state.json` → `70 11` (additive: 6 `commit`/`commitFull`
insertions, 2 placeholder replacements, 1 `certificationCommitState` replacement, the W3-R3 evidence
classification fields, the `postCertRecoveryState` closure wording and the new `pricingAmsc001W3R4` block).

```text
productionCodeChanged = false
manifestStructuralState = NOT_TOUCHED
schemaMigrationState = UNCHANGED
```

No file under `src/backend/Modules/Pricing/`, no `.csproj`, no `.slnx`, no manifest, no `.resx` and no
migration was touched.

## 2. JSON parse + exact assertions

`node docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R4/apply-recovery.cjs` → `SOT_OK`

| Assertion | Result |
| --- | --- |
| JSON parses after the additive edit | PASS |
| `w0.commitFull == 08d47b6a4af3e4e2102712b93f27185ac15a23ac` | PASS |
| `w1.commitFull == 069f77d2fa5b2c2cec3bb078147c3559a571bd64` | PASS |
| `w2.commitFull == f7f6abfec455b771952852e8627df4c57b698caf` | PASS |
| `w3R1.commitFull == 2e664bb336f45b8304818b7e5754f9b0fc364f20` | PASS |
| `w3R2.commitFull == 7159c8f773c1faa9b4b6d425b19067f50ca27572` | PASS |
| `w3R3.commitFull == a1ca9b5afab181da74fe6db0efbb38f43a3a9721` | PASS |
| no `commit`/`commitFull` placeholder survives on any Pricing block | PASS |
| `w3R1.masterRecoveryW3ShaBefore` still `PENDING_THIS_COMMIT` (historical marker preserved) | PASS |
| `w3R3.certificationCommitState == RECORDED_A1CA9B5A` | PASS |
| both evidence-only hops classified `EVIDENCE_ONLY_NOT_CERTIFICATION_AUTHORITY` | PASS |
| `w3R4` carries no self-referential `commit` field | PASS |
| preserved-fact spot checks (state/verdict/startingHead/parentCommit/certifiedCommit/architecture facts) | PASS |
| global recovery lock fields + `structureLock.certifiedModules` byte-equal to pre-edit | PASS |

## 3. Exact parent chain

`git rev-list --parents -n 1 <sha>` for all nine lineage commits → table in
`lineage-reconciliation.md` §1; `actualParentChainState = RECONCILED`.

## 4. Focused tests

| Suite | Result |
| --- | --- |
| `PricingModuleAmsc001W3R3CertGuardTests` | 8 / 8 passed |
| `PricingModuleAmsc001W3R2RepairGuardTests` | 11 / 11 passed |
| `PricingModuleAmsc001W2StructureGuardTests` | 8 / 8 passed |
| `PricingModuleAmsc001W1MigrateGuardTests` | 9 / 9 passed |
| **Pricing guard family total** | **36 / 36 passed** |
| `TmarCompleteReferenceStructureGateTests` | 3 passed / 1 failed — pre-existing, unrelated |
| `Tooba.Host.Tests` build | succeeded, **0 errors** |

The single `TmarCompleteReferenceStructureGateTests` failure
(`Certified_modules_satisfy_root_allowlists_and_namespace_alignment`) is the repository-global
`Tooba.Catalog.Contracts/Cart` path↔namespace deviation. It was probed on the stashed starting-head tree
during W3-R3 and fails byte-identically there; it is out of scope for a module-local Pricing wave and was
not weakened, widened or repaired. No broad suite was run, per the task's bounded-validation instruction.

## 5. No-regression proof

```text
Pricing Endpoints project directory  : ABSENT
Tooba.slnx /Modules/Pricing/ entries : 5 (Application, Contracts, Domain, Infrastructure, Tests)
Host Program.cs Pricing ceremony     : 0 hits
httpApplicability                    : NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER
manifestStructuralState              : NOT_TOUCHED_THIS_WAVE
schemaMigrationState                 : UNCHANGED
globalHostCheckpointState            : PRESERVED
```

## 6. Verdict

```text
Recovery-SoT-State               = FINAL_CLOSED
Production-Code-Changed-State    = ZERO
Actual-Parent-Chain-State        = RECONCILED
Certification-Authority-State    = W3_R3_A1CA9B5A
Structure-Authority-State        = W3_R2_7159C8F7
Evidence-Only-Commit-State       = RECORDED_127C596A_NOT_AUTHORITY
Guards-Weakened-State            = NONE
Baselines-Widened-State          = NONE
Evidence-State                   = COMPLETE
Automatic-Next-Implementation-Task-State = NONE
Workflow-Stop-State              = USER_REVIEW_PRICING_AMSC_001_W3_R4
```
