// TB-TMAR-PAGECOMPOSITION-AMSC-001-W3-R2 — post the complete Task Result to Bridge and close the lifecycle.
const TASK_ROW_ID = "38acfe2b-5eab-47fd-8181-7f446e1e828d";
const TASK_ID = "TB-TMAR-PAGECOMPOSITION-AMSC-001-W3-R2";

const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PAGECOMPOSITION-AMSC-001-W3-R2
Parent-Task: TB-TMAR-PAGECOMPOSITION-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS
Summary: Bounded recovery-lineage reconciliation only (zero production, zero manifest, zero schema/migration, zero behavior change; global recovery lock preserved). PRECHECK PASS: branch main, HEAD == origin/main == 9770b884243b9e586fbeb163e5d0d538f488ea0a (required starting HEAD); exact parent relationships verified: 11e22747 parent = 6a28c921, 361a1837 parent = 11e22747, 07e9032a parent = 361a1837, 9770b884 parent = 07e9032a; W3 reconfirmed PAGECOMPOSITION_AMSC_001_CERTIFIED / COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 / structureCertified true / endpointReachableRequests 8 / validatorRequiredCount 8 / crossModuleBoundaryState NONE_SELF_CONTAINED / microserviceExtractable true; shallow spot checks PASS (Admin/Commands, Admin/Queries, Admin/Validators, Storefront/Queries, Storefront/Validators each 0 child directories). SoT (tmar-current-state.json): pageCompositionAmsc001W3R1.commit = 9770b884 and commitFull = 9770b884243b9e586fbeb163e5d0d538f488ea0a added additively (certifiedCommit preserved verbatim 07e9032a20e1d0d52afb0bc70f61c821d1bb0fad; R1 state PAGECOMPOSITION_AMSC_001_RECOVERY_RECONCILED, verdict COMPLETE_REFERENCE_PATTERN, lockVersion ARCH-COMPLETE-002, structureCertified true, productionCodeChanged false, crossModuleBoundaryState NONE_SELF_CONTAINED, foreignAppInfraDomainCoupling ZERO, microserviceExtractable true, automaticNextImplementationTask NONE all preserved). W1 reconciliation truth: pageCompositionAmsc001W1.commit = 6a28c921 NOT replaced (W1 implementation authority preserved); R2 block records w1ImplementationCommit = 6a28c921f14a5cc7127b03d41fd35ee625695da5, w1MetadataReconciliationCommit = 11e22747b6f9d4f02d9bdc27ac6706925c7b4fcc, w2StartingHead = 11e22747, w1ReconciliationState = RECORDED_METADATA_ONLY_NOT_IMPLEMENTATION_WAVE (11e22747 NOT classified as an implementation wave). Additive SoT pageCompositionAmsc001W3R2 closure block recorded (task/parentTask per contract, mode RECOVERY_LINEAGE_RECONCILIATION_ONLY, startingHead 9770b884, state PAGECOMPOSITION_AMSC_001_RECOVERY_CLOSED, productionCodeChanged false, currentCertificationAuthority TB-TMAR-PAGECOMPOSITION-AMSC-001-W3, currentCertifiedCommit 07e9032a20e1d0d52afb0bc70f61c821d1bb0fad, w0ImplementationCommit 652036ccaeab8ce6bf9c34ac62da2cb312f94b29, w2Commit 361a18374efef4ffb7737023aedd10a805752895, w3Commit 07e9032a20e1d0d52afb0bc70f61c821d1bb0fad, w3R1Commit 9770b884243b9e586fbeb163e5d0d538f488ea0a, actualParentChainState RECONCILED, r1CommitState RECORDED_9770B884, manifestStructuralState NOT_TOUCHED_THIS_WAVE, schemaMigrationState UNCHANGED, frontendState FROZEN_UNCHANGED, globalHostCheckpointState PRESERVED, guardsWeakened NONE, baselinesWidened NONE, workflowStop USER_REVIEW_PAGECOMPOSITION_AMSC_001_W3_R2, automaticNextImplementationTask NONE; no self-referential PENDING commit field added to R2). Master Recovery: appended PageComposition AMSC W3-R2 recovery closure checkpoint (authoritative, module-local) distinguishing semantic AMSC waves W0 652036cc -> W1 6a28c921 -> W2 361a1837 -> W3 07e9032a from the actual git/recovery handoff chain W0 652036cc -> W1 implementation 6a28c921 -> W1 metadata reconciliation 11e22747 -> W2 361a1837 -> W3 07e9032a -> W3-R1 9770b884, and stating explicitly: 11e22747 is metadata reconciliation only and does not replace W1 implementation authority; W2 legitimately started from 11e22747; current certification authority remains W3 07e9032a; R2 closes recovery lineage only; historical AMC-001 lineage remains historical/superseded, not rewritten; global Host checkpoint preserved; automaticNextImplementationTask NONE. Manifest: zero bytes touched in R2 (git status proof; OperatorProfile and Notification certified truth each present exactly once, single merged 25-module modules array intact). Bounded validation PASS: JSON parse SoT OK, exact R1 SHA assertion PASS, exact W1 implementation/reconciliation assertions PASS, exact parent-chain assertions PASS, exact Master Recovery lineage assertions PASS, shallow-structure child-directory spot checks PASS (0 child dirs on all five request axes), focused PageComposition recovery/cert guard not modified (already green 22/22 at R1; guardsWeakened NONE), git diff scope proof shows only allowed files (SoT json +48/-1, Master Recovery +2 lines, R2 evidence 3 files, persisted task artifact); all other dirty/untracked files are pre-existing unrelated artifacts preserved untouched. Global recovery lock preserved exactly: lastAcceptedTask, lastAcceptedCommit, latestAcceptedImplementationWave, currentHostCheckpoint, nextHostFolder, repository-global workflowStop and automaticNextImplementationTask unchanged. Final certification authority remains W3 at 07e9032a20e1d0d52afb0bc70f61c821d1bb0fad: COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 STRUCTURE_CERTIFIED. Exactly one R2 commit, pushed, HEAD == origin/main. STOP completely: no R3, no W4, no next module; waiting for Architect review.
Starting-HEAD-State: 9770B884
Production-Scope-State: RECOVERY_LINEAGE_ONLY
Production-Code-Changed-State: ZERO
Current-Certification-Authority-State: W3_07E9032A
R1-Commit-State: RECORDED_9770B884
W1-Implementation-Commit-State: PRESERVED_6A28C921
W1-Metadata-Reconciliation-State: RECORDED_11E22747_METADATA_ONLY
W2-Starting-Head-State: 11E22747
Actual-Parent-Chain-State: RECONCILED
Structure-State: CERTIFIED
Folder-Granularity-State: PROFESSIONAL_SHALLOW
Per-UseCase-Request-Leaf-State: ZERO
Validator-State: EXHAUSTIVE_8_OF_8
Cross-Module-Boundary-State: NONE_SELF_CONTAINED
Foreign-App-Infra-Domain-Endpoints-Coupling-State: ZERO
Manifest-Structural-State: NOT_TOUCHED
Schema-Migration-State: UNCHANGED
Global-Host-Checkpoint-State: PRESERVED
Guards-Weakened-State: NONE
Baselines-Widened-State: NONE
Json-Parse-State: PASS
Evidence-State: COMPLETE
Recovery-SoT-State: CLOSED_RECONCILED
Commit-SHA: 348c3baa57564e2d76df8824f83a0f388379f116
HEAD-Equals-Origin-Main: YES
Working-Tree-State: CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS
User-Work-Preserved: YES
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PAGECOMPOSITION_AMSC_001_W3_R2
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
