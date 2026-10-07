// TB-TMAR-PARTY-AMSC-001-W3-R3 — post the complete Task Result to Bridge and close the lifecycle.
const TASK_ROW_ID = "8a7f22e7-3957-4743-b17b-b156a60cae60";
const TASK_ID = "TB-TMAR-PARTY-AMSC-001-W3-R3";

const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PARTY-AMSC-001-W3-R3
Parent-Task: TB-TMAR-PARTY-AMSC-001-W3-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS
Summary: Bounded W3 authoritative-truth reconciliation only (zero production, zero manifest, zero schema/migration, zero guard weakening, zero behavior change; global recovery lock preserved). PRECHECK PASS: branch main, HEAD == origin/main == fc2005eb528c5506aec69f157530e218e0eb3561 (required starting HEAD); fc2005eb parent verified == 3eb6e722179d50e308a986b20a4040b45a15555d; fc2005eb diff confirmed evidence-only (adds only docs/architecture/evidence/TB-TMAR-PARTY-AMSC-001-W3-R2/post-result.js) and is recognized as post-result evidence, not a recovery implementation wave or certification authority (recorded as postResultEvidenceCommit in the SoT R3 block). Stale fact confirmed before edit: the current Party W3 certification checkpoint in TOOBA-TMAR-MASTER-RECOVERY.md said "11 declared codes ... -> 10 registered descriptors", contradicting repository truth. Direct source counts: PartyErrorCodes.cs public const string = 11 Party-owned stable constants; PartyErrorCatalogContributor.cs D(PartyErrorCodes.) descriptor registrations = 11; authoritative = 11 declared / 11 registered. IMPLEMENTATION: corrected ONLY the stale descriptor-count statement in the existing Party W3 Master Recovery certification checkpoint bullet - now reads "11 declared Party-owned stable codes with the certified IsKnown(string?) declared-code guard -> 11 registered Party-owned descriptors; descriptor-count wording reconciled from a stale single-digit descriptor analysis note by TB-TMAR-PARTY-AMSC-001-W3-R3; seller.authorization.denied stays Foundation-owned and is not duplicated by Party" (Foundation-owned note preserved verbatim in meaning). No unrelated W3 fact altered: lineage 2477bbb3 -> 29012df0 -> ffff7100 -> d1cc2f48, verdict COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 STRUCTURE_CERTIFIED, structure, cross-module boundary bullets all untouched. HISTORICAL W0 TRUTH PRESERVED: partyAmsc001W0.stableErrorCodeState = CATALOGUED_10_OF_10_MISSING_ISKNOWN_DECLARED_CODE_GUARD unchanged verbatim, with the R2 additive field stableErrorCodeStateReconciledByR2 (HISTORICAL_ANALYSIS_METADATA_STALE) intact; not rewritten. R2 LINEAGE PRESERVED: partyAmsc001W3R2 block unchanged (state PARTY_AMSC_001_RECOVERY_CLOSED_RECONCILED, recoveryCommit 3eb6e722179d50e308a986b20a4040b45a15555d) and its Master Recovery sub-checkpoint unchanged; W3 certification authority unchanged at d1cc2f480619ce1ec68cd730650018883684e6c7. SoT: additive partyAmsc001W3R3 closure block recorded (task/parentTask per contract, mode RECOVERY_AUTHORITATIVE_W3_TRUTH_RECONCILIATION_ONLY, startingHead fc2005eb, state PARTY_AMSC_001_RECOVERY_FINAL_CLOSED, productionCodeChanged false, currentCertificationAuthority TB-TMAR-PARTY-AMSC-001-W3, currentCertifiedCommit d1cc2f480619ce1ec68cd730650018883684e6c7, recoveryAuthority TB-TMAR-PARTY-AMSC-001-W3-R2, recoveryCommit 3eb6e722179d50e308a986b20a4040b45a15555d, postResultEvidenceCommit fc2005eb528c5506aec69f157530e218e0eb3561, authoritativeStableErrorCount 11, authoritativeStableErrorDescriptorCount 11, w3MasterRecoveryDescriptorState RECONCILED_11_OF_11, historicalW0ErrorCountState PRESERVED_AS_STALE_HISTORICAL_ANALYSIS_METADATA, manifestStructuralState NOT_TOUCHED, schemaMigrationState UNCHANGED, globalHostCheckpointState PRESERVED, guardsWeakened NONE, baselinesWidened NONE, workflowStop USER_REVIEW_PARTY_AMSC_001_W3_R3, automaticNextImplementationTask NONE; no self-referential R3 commit placeholder added). BOUNDED VALIDATION PASS: rg "10 registered descriptors" Master Recovery = 0 matches; corrected 11/11 wording = exactly 1 match in the W3 checkpoint bullet; no residual stale "arrow 10" count variants; source counts 11 constants / 11 descriptors; JSON parse SoT OK after additive block; R2 lineage assertions unchanged; git diff scope proof shows this R3 commit touches only allowed files (Master Recovery single bullet 1 line changed, SoT json +25 additive R3 block, R3 evidence 2 files, persisted task artifact); zero changes under src/, zero csproj, zero manifest, zero resx/resources, zero migrations, zero guards, zero Host/frontend; all other dirty/untracked files are pre-existing unrelated artifacts preserved untouched. Global recovery lock preserved exactly: lastAcceptedTask, lastAcceptedCommit, latestAcceptedImplementationWave, currentHostCheckpoint, nextHostFolder, repository-global workflowStop and automaticNextImplementationTask unchanged. Final certification authority remains W3 at d1cc2f480619ce1ec68cd730650018883684e6c7: COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 STRUCTURE_CERTIFIED. Exactly one R3 reconciliation commit 72ae77aab092d9316fac558271106d3dc9ce6eb6 pushed, HEAD == origin/main verified by fetch + rev-parse. STOP completely: no R4, no W4, no next module; waiting for Architect review.
Starting-HEAD-State: FC2005EB
Post-Result-Evidence-Commit-State: RECOGNIZED_EVIDENCE_ONLY
Production-Code-Changed-State: ZERO
Current-Certification-Authority-State: W3_D1CC2F48
Recovery-Authority-State: R2_3EB6E722
W3-Master-Recovery-Descriptor-State: RECONCILED_11_OF_11
Stable-Error-Catalog-State: AUTHORITATIVE_11_OF_11
Historical-W0-Error-Count-State: PRESERVED_STALE_HISTORICAL_METADATA
R2-Lineage-State: PRESERVED
Manifest-Structural-State: NOT_TOUCHED
Schema-Migration-State: UNCHANGED
Global-Host-Checkpoint-State: PRESERVED
Guards-Weakened-State: NONE
Baselines-Widened-State: NONE
Evidence-State: COMPLETE
Recovery-SoT-State: FINAL_CLOSED
Commit-SHA: 72ae77aab092d9316fac558271106d3dc9ce6eb6
HEAD-Equals-Origin-Main: YES
Working-Tree-State: CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS
User-Work-Preserved: YES
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PARTY_AMSC_001_W3_R3
STOP
END_TOOBA_WORKER_RESULT`;

async function main() {
    if (!content.includes("BEGIN_TOOBA_WORKER_RESULT") || !content.includes("END_TOOBA_WORKER_RESULT") || !content.includes(`Task-ID: ${TASK_ID}`)) {
        throw new Error("result contract markers missing");
    }

    const results = await fetch("http://127.0.0.1:17321/api/results", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channelId: "tooba-main", taskId: TASK_ID, content }),
    });
    console.log("RESULTS", results.status, await results.text());
    if (results.status < 200 || results.status >= 300) {
        throw new Error("result post failed");
    }

    const complete = await fetch(`http://127.0.0.1:17321/api/tasks/${TASK_ROW_ID}/complete`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: "{}",
    });
    console.log("COMPLETE", complete.status, await complete.text());

    const hb = await fetch("http://127.0.0.1:17321/api/workers/heartbeat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ workerId: "tooba-worker-01", channelId: "tooba-main", agentType: "cursor", status: "Idle" }),
    });
    console.log("IDLE", hb.status, await hb.text());
}

main().catch(err => { console.error(err); process.exit(1); });
