# TB-TMAR-STORY-AMSC-001-W3-R1 — Recovery / SoT Reconciliation

- **Task:** `TB-TMAR-STORY-AMSC-001-W3-R1`
- **Parent:** `TB-TMAR-STORY-AMSC-001-W3` (commit `39ab324e`)
- **Mode:** `RECOVERY_SOT_RECONCILIATION_ONLY`
- **Skill:** recovery / SoT / evidence reconciliation only
- **Target:** `docs/architecture` (documentation / SoT / evidence truth only)
- **Starting HEAD:** `39ab324e9c57e342516202bdd8df95953dda6439` (`HEAD == origin/main` verified)
- **Production change:** **ZERO**

---

## 1. Why this wave exists

The W3 certification commit could not contain its own SHA. The Story AMSC module recovery checkpoint in
`docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` therefore recorded the lineage line as
`TB-TMAR-STORY-AMSC-001-W3` Certify *(this commit, reported in the Bridge Result only; the Architect
reconciles the final SHA separately)*, and `storyAmsc001W3.commit` was `PENDING_W3_COMMIT`.

W3-R1 records the actual final SHA and reconciles the recovery checkpoint — the same bounded
reconciliation wave already applied to StoreContext (`TB-TMAR-STORECONTEXT-AMSC-001-W3`), Settlement
(`e7131dc0`), PageComposition (`9770b884`), Media, Localization, Inventory, Identity, Content,
CustomerProfile, Fulfillment, BulkInquiry, Cart and Returns.

## 2. Historical-truth correction

| Location | Before | After |
| --- | --- | --- |
| Master Recovery Story lineage line | `TB-TMAR-STORY-AMSC-001-W3` Certify *(this commit …)* | `TB-TMAR-STORY-AMSC-001-W3` Certify `39ab324e` (`39ab324e9c57e342516202bdd8df95953dda6439`) |
| SoT `storyAmsc001W3.commit` | `PENDING_W3_COMMIT` | `39ab324e9c57e342516202bdd8df95953dda6439` |
| SoT `storyAmsc001W3.guardsAdded` | `StoryModuleAmsc001W3CertGuardTests (9 facts)` | `StoryModuleAmsc001W3CertGuardTests (8 facts)` |
| SoT `storyAmsc001W3.focusedValidation` | `… StoryModuleAmsc001W3CertGuardTests 9/9 …` | `… StoryModuleAmsc001W3CertGuardTests 8/8 …` |
| SoT R1 block `storyAmsc001W3R1` | absent | appended (recovery-reconciliation record) |
| Master Recovery W3-R1 checkpoint | absent | appended (authoritative, module-local) |

The two `guardsAdded` / `focusedValidation` corrections are a **truth repair only**: the W3 record was
written before the guard class was finalised at 8 `[Fact]`s (the earlier “9 facts” figure counted a
helper-backed phantom fact). No assertion was removed, no guard was weakened, no baseline was widened.

No other Story line was rewritten. The accepted lineage is preserved exactly:

```text
W0 Analyze     0c73390a3211e0ee9057e9234d62d3e4f14b5e4e
W1 Migrate     2a09e7bb7f1ab687435007951160b4bcfefb4c18
W2 Structure   4cd9a6cc543ccd307d775dfe703459de7b12c95d
W3 Certify     39ab324e9c57e342516202bdd8df95953dda6439
W3-R1 Recovery this commit  RECONCILED
```

## 3. Current Story authority preserved

`storyAmsc001W0..W3` remains the authoritative current lineage, unchanged:

```text
state                          = STORY_AMSC_001_CERTIFIED
verdict                        = COMPLETE_REFERENCE_PATTERN
lockVersion                    = ARCH-COMPLETE-002
structureCertified             = true
structureState                 = CERTIFIED
httpApplicability              = HTTP_OWNING
endpointOwnershipState         = MODULE_OWNED (25 routes, Host ZERO)
endpointReachableRequests      = 25
validatorCoverageState         = EXHAUSTIVE_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED
crossModuleBoundaryState       = CLEAN_CONTRACTS_ONLY
foreignModuleLayerCoupling     = ZERO
crossModuleJoinState           = NONE
microserviceExtractable        = true
blockingResidualDebt           = ZERO
```

## 4. Historical lineage preservation

`storyModuleAmc001` .. `storyModuleAmc001W6Cert` (`TB-TMAR-STORY-AMC-001` W1→W6-CERT) are **not**
rewritten into AMSC truth. They stay at their truthful values as historical evidence and are explicitly
marked `HISTORICAL / SUPERSEDED FOR CURRENT STORY MODULE AUTHORITY`. The AMSC-001 W0→W3 lineage is
authoritative for the current Story module certification.

## 5. Additive SoT record

`storyAmsc001W3R1`:

```text
state                          = STORY_AMSC_001_RECOVERY_RECONCILED
productionCodeChanged          = false
productionScopeState           = RECOVERY_SOT_EVIDENCE_ONLY
certifiedCommit                = 39ab324e9c57e342516202bdd8df95953dda6439
masterRecoveryW3ShaBefore      = PLACEHOLDER_THIS_COMMIT
masterRecoveryW3ShaState       = RECORDED_39AB324E
historicalLineageState         = PRESERVED_HISTORICAL_NOT_REWRITTEN
globalHostCheckpointState      = PRESERVED
manifestStructuralState        = NOT_TOUCHED
schemaMigrationState           = UNCHANGED
frontendState                  = FROZEN_UNTOUCHED
guardsWeakened                 = NONE (0)
commitState                    = REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER
stopGate                       = USER_REVIEW_STORY_AMSC_001_W3_R1
automaticNextImplementationTask = NONE
```

The R1 record deliberately carries **no** self-referential `commit` placeholder (the Pricing W3-R3
lesson): the W3-R1 final SHA is reported through the Bridge Result contract only, and the SoT block
records an explicit `commitState` instead.

## 6. Repository-global Host root checkpoint

**Not superseded and not displaced.** `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`,
`currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`, the repository-global
`workflowStop = USER_REVIEW_HOST_ROOT_FINAL_CERT_001` and
`automaticNextImplementationTask = NONE` are untouched. The manifest structure, schema/migrations and
the frozen frontend are untouched. No guard was weakened.

## 7. Focused validation

| Run | Result |
| --- | --- |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj` | **0 errors** |
| `--filter StoryModuleAmsc001W3R1RecoveryGuardTests` | **5/5 PASS** (new recovery guard) |
| Story AMSC + AMC + `HostStoryAmc` + `StoryFoundation` focused filter | **45 passed / 0 failed** (2 docker-skipped) |
| `ErrorCatalogUniqueCodeGuardTests` | **3/3 passed** |
| Declared-red family (`HostGridAmcR3/R4/R5R1`, `TmarDurableGuardTests`, `TmarCompleteReferenceStructureGateTests`, `TmarSourceSizeAndInfraAppTests`) | **10 failed / 21 passed — identical to the clean W2 baseline** |

### 7a. Newly-caused Story regression found and repaired

The W3 certification promotion added `Story` to `structureLock.certifiedModules`, which made
`TmarCompleteReferenceStructureGateTests.Uncertified_modules_are_explicitly_not_claimed` **newly red**
(its frozen 28-entry `certifiedModules` literal did not contain `Story`). A clean `git worktree` at the
untouched W2 HEAD (`4cd9a6cc`) proved that fact was **green at W2** and that this regression is therefore
**Story-owned debt introduced by the AMSC promotion**, not pre-existing drift.

It was repaired by extending that frozen literal with `Story` — the same bounded literal-extension
precedent used by the Pricing W3-R3 certification wave. **No assertion was removed, no guard was
weakened and no baseline was widened.** After the repair the declared-red family failure set at HEAD is
byte-for-byte identical to the W2 baseline (10 failed / 21 passed), i.e. **zero net regression**.

The genuinely pre-existing, unrelated reds remain unchanged and are **not** repaired here (a module-local
reconciliation must not displace the repository-global Host root checkpoint): `HostGridAmcR3/R4/R5R1`
(Catalog/Party/Reviews folder drift), the two `TmarDurableGuardTests` recovery-checkpoint facts (frozen
`certifiedModules` literal + repository-global checkpoint pins), the
`TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy…` Catalog Contracts namespace debt and
`TmarSourceSizeAndInfraAppTests` (stale git-ignored `.tmp-baseline` worktree).

## 8. Verdict

```text
W3-R1-State       = RECONCILED
W3-Certification  = PRESERVED_UNCHANGED
Production-Change = ZERO
Stop gate         = USER_REVIEW_STORY_AMSC_001_W3_R1
automaticNextImplementationTask = NONE
```
