PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PARTY-AMSC-001-W3-R3
Parent-Task: TB-TMAR-PARTY-AMSC-001-W3-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: RECOVERY_AUTHORITATIVE_W3_TRUTH_RECONCILIATION_ONLY
Title: Reconcile stale W3 Master Recovery descriptor count to authoritative Party 11/11 truth

ARCHITECT VERDICT
Party production architecture remains accepted.
R2 correctly reconciled: W0 final SHA, W3-R1 final SHA, metadata handoff commits, W0 historical 10-vs-11 analysis-count defect.

One authoritative documentation inconsistency remains: the current Party W3 checkpoint inside docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md still states "11 declared codes ... -> 10 registered descriptors". This contradicts current repository truth (PartyErrorCodes.cs = 11 Party-owned stable constants; PartyErrorCatalogContributor.cs = 11 registered descriptors; manifest current Party certification = IsKnown over all 11 codes; R2 current authoritative recovery truth = 11/11). Because that stale text lives in the W3 certification checkpoint, not merely historical W0 evidence, Party Recovery is NOT yet closed.

STARTING HEAD
fc2005eb528c5506aec69f157530e218e0eb3561 (post-result evidence-only commit; parent = 3eb6e722179d50e308a986b20a4040b45a15555d; adds only docs/architecture/evidence/TB-TMAR-PARTY-AMSC-001-W3-R2/post-result.js; not a recovery implementation wave or certification authority)

CURRENT CERTIFICATION AUTHORITY
TB-TMAR-PARTY-AMSC-001-W3
d1cc2f480619ce1ec68cd730650018883684e6c7
COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 / STRUCTURE_CERTIFIED

CURRENT RECOVERY AUTHORITY
TB-TMAR-PARTY-AMSC-001-W3-R2
3eb6e722179d50e308a986b20a4040b45a15555d

GOAL
One tiny documentation/recovery truth reconciliation: correct the stale W3 Master Recovery descriptor count from 10 to 11; make the W3 checkpoint consistent with actual 11 declared / 11 registered Party-owned codes; preserve the historical W0 10/10 field as stale analysis metadata; preserve all W3 certification authority and R2 recovery lineage; optionally record this final reconciliation additively in SoT; zero production change; zero manifest change; zero schema change; zero guard weakening; automaticNextImplementationTask = NONE.

PRECHECK
- Verify HEAD == origin/main == fc2005eb528c5506aec69f157530e218e0eb3561.
- Verify fc2005eb parent == 3eb6e722179d50e308a986b20a4040b45a15555d.
- Verify fc2005eb changed only the R2 post-result evidence artifact.
- Read: docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md, docs/architecture/tmar-current-state.json, docs/architecture/tmar-module-structure-manifests.json, PartyErrorCodes.cs, PartyErrorCatalogContributor.cs, W3 evidence, R2 evidence.
- Count directly: PartyErrorCodes public const string = 11; PartyErrorCatalogContributor descriptors = 11.
- Verify the W3 Master Recovery contains the stale "10 registered descriptors" wording and that the R2 SoT says authoritativeStableErrorCount=11 and authoritativeStableErrorDescriptorCount=11.
- If any fact differs: RECOVERY_CONFLICT + STOP.

GLOBAL RECOVERY LOCK
Preserve exactly: lastAcceptedTask, lastAcceptedCommit, latestAcceptedImplementationWave, currentHostCheckpoint, nextHostFolder, repository-global workflowStop, repository-global automaticNextImplementationTask.

ALLOWED FILES
- docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
- docs/architecture/tmar-current-state.json only if additive R3 closure record is useful
- docs/architecture/evidence/TB-TMAR-PARTY-AMSC-001-W3-R3/*
- docs/ai/tasks/TB-TMAR-PARTY-AMSC-001-W3-R3.task.md

FORBIDDEN
Any production file; any Party/Promotion csproj; Host production; frontend; manifest; W3 guards; global gates; validators/resources/resx; schema/migrations; rewriting the historical W0 10/10 field; changing W3 verdict/lock/authority; changing semantic wave SHAs; guard weakening; baseline widening; unrelated cleanup; next module.

IMPLEMENTATION
1. MASTER RECOVERY W3 CHECKPOINT: correct ONLY the stale descriptor-count statement. Replace the contradictory meaning "11 declared codes -> 10 registered descriptors" with the truthful current/certified meaning "11 declared Party-owned stable codes -> 11 registered Party-owned descriptors". Preserve the Foundation-owned note: seller.authorization.denied remains Foundation-owned and is not duplicated by Party. Do not alter unrelated W3 facts.
2. PRESERVE HISTORICAL W0 TRUTH: do not change partyAmsc001W0.stableErrorCodeState = CATALOGUED_10_OF_10_MISSING_ISKNOWN_DECLARED_CODE_GUARD; keep the R2 additive correction explaining it was stale W0 analysis metadata.
3. OPTIONAL FINAL R3 CLOSURE BLOCK: add partyAmsc001W3R3 (task, parentTask, mode RECOVERY_AUTHORITATIVE_W3_TRUTH_RECONCILIATION_ONLY, startingHead fc2005eb, state PARTY_AMSC_001_RECOVERY_FINAL_CLOSED, productionCodeChanged false, currentCertificationAuthority TB-TMAR-PARTY-AMSC-001-W3, currentCertifiedCommit d1cc2f480619ce1ec68cd730650018883684e6c7, recoveryAuthority TB-TMAR-PARTY-AMSC-001-W3-R2, recoveryCommit 3eb6e722179d50e308a986b20a4040b45a15555d, postResultEvidenceCommit fc2005eb528c5506aec69f157530e218e0eb3561, authoritativeStableErrorCount 11, authoritativeStableErrorDescriptorCount 11, w3MasterRecoveryDescriptorState RECONCILED_11_OF_11, historicalW0ErrorCountState PRESERVED_AS_STALE_HISTORICAL_ANALYSIS_METADATA, manifestStructuralState NOT_TOUCHED, schemaMigrationState UNCHANGED, globalHostCheckpointState PRESERVED, guardsWeakened NONE, baselinesWidened NONE, workflowStop USER_REVIEW_PARTY_AMSC_001_W3_R3, automaticNextImplementationTask NONE). Do NOT add a self-referential R3 commit placeholder.

EVIDENCE
Create docs/architecture/evidence/TB-TMAR-PARTY-AMSC-001-W3-R3/: authoritative-w3-truth-reconciliation.md, validation.md. Record: exact stale W3 Master Recovery text before; exact corrected 11/11 meaning after; direct counts from source (11 constants / 11 descriptors); proof the Foundation-owned seller.authorization.denied remains non-duplicated; proof the historical W0 stale 10/10 field is preserved; proof R2 lineage is preserved; proof production/manifest/schema unchanged; proof the Host checkpoint is unchanged.

BOUNDED VALIDATION
Run only: exact text assertion (W3 checkpoint no longer says 10 registered descriptors); exact text assertion (W3 checkpoint states 11 declared / 11 registered); source count 11 constants / 11 descriptors; JSON parse SoT if changed; R2 lineage assertions unchanged; git diff scope proof. No builds required. No full tests. No production edits.

PASS CRITERIA
W3 Master Recovery authoritative checkpoint says truthful 11/11; W0 historical stale field preserved; R2 lineage/recovery preserved; W3 certification authority remains d1cc2f48; production/manifest/schema untouched; Host checkpoint preserved; automatic next NONE.

COMMIT/PUSH
If PASS: exactly one R3 reconciliation commit, push main, verify HEAD == origin/main, STOP.

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PARTY-AMSC-001-W3-R3
Parent-Task: TB-TMAR-PARTY-AMSC-001-W3-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: FC2005EB | DIVERGED
Post-Result-Evidence-Commit-State: RECOGNIZED_EVIDENCE_ONLY | CONFLICT
Production-Code-Changed-State: ZERO | NONZERO
Current-Certification-Authority-State: W3_D1CC2F48 | CONFLICT
Recovery-Authority-State: R2_3EB6E722 | CONFLICT
W3-Master-Recovery-Descriptor-State: RECONCILED_11_OF_11 | STALE_10 | CONFLICT
Stable-Error-Catalog-State: AUTHORITATIVE_11_OF_11 | CONFLICT
Historical-W0-Error-Count-State: PRESERVED_STALE_HISTORICAL_METADATA | REWRITTEN | CONFLICT
R2-Lineage-State: PRESERVED | REGRESSED
Manifest-Structural-State: NOT_TOUCHED | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Global-Host-Checkpoint-State: PRESERVED | REGRESSED
Guards-Weakened-State: NONE | NONZERO
Baselines-Widened-State: NONE | NONZERO
Evidence-State: COMPLETE | INCOMPLETE
Recovery-SoT-State: FINAL_CLOSED | STALE | CONFLICT
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PARTY_AMSC_001_W3_R3
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP completely after result. No R4. No W4. No next module. Wait for Architect review.

END_TOOBA_TASK
