const http = require('http');

const payload = {
  channelId: 'tooba-main',
  taskId: 'TB-TMAR-OPERATORPROFILE-AMSC-001-W3-R2',
  content: `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-OPERATORPROFILE-AMSC-001-W3-R2
Parent-Task: TB-TMAR-OPERATORPROFILE-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS
Summary: Bounded recovery/process reconciliation only (zero production, zero structural, zero manifest/global-gate mutation, zero behavior change). PRECHECK PASS: branch main, HEAD == origin/main == 17ad8d10990d7a28035d4c2fb0c5c1f0c36ad0a3 (required starting HEAD). SoT: operatorProfileAmsc001W3R1.commit/commitFull = 17ad8d10/17ad8d10990d7a28035d4c2fb0c5c1f0c36ad0a3 added (R1 certifiedCommit preserved verbatim at 04d5b03018a8f262ee1446bf7ee5c467d29cb7b9; R1 state OPERATORPROFILE_AMSC_001_RECOVERY_RECONCILED, verdict COMPLETE_REFERENCE_PATTERN, lockVersion ARCH-COMPLETE-002, structureCertified true, productionCodeChanged false, crossModuleBoundaryState CONTRACTS_ONLY, foreignAppInfraDomainCoupling ZERO, microserviceExtractable true, automaticNext NONE all preserved). Additive SoT operatorProfileAmsc001W3R2 recorded (task/parentTask TB-TMAR-OPERATORPROFILE-AMSC-001-W3-R1, mode RECOVERY_PROCESS_RECONCILIATION_ONLY, startingHead 17ad8d10, state OPERATORPROFILE_AMSC_001_RECOVERY_CLOSED, productionCodeChanged false, currentCertificationAuthority TB-TMAR-OPERATORPROFILE-AMSC-001-W3, currentCertifiedCommit 04d5b03018a8f262ee1446bf7ee5c467d29cb7b9, r1CommitState RECORDED_17AD8D10, w0GlobalPrerequisiteRepairState PRESERVED_ONE_OFF_RECOVERY_DEVIATION, w0AnalyzePrecedentState NOT_A_REUSABLE_ANALYZE_PRECEDENT, manifestCurrentTruthState PRESERVED_VALID, globalStructureGateState PRESERVED_NOT_TOUCHED_THIS_WAVE, globalHostCheckpointState PRESERVED, manifestStructuralState NOT_TOUCHED_THIS_WAVE, schemaMigrationState UNCHANGED, frontendState FROZEN_UNCHANGED, guardsWeakened NONE, baselinesWidened NONE, workflowStop USER_REVIEW_OPERATORPROFILE_AMSC_001_W3_R2, automaticNextImplementationTask NONE; no self-referential PENDING commit field added to R2). Master Recovery: additive R2 checkpoint records full lineage W0 639d73ea -> W1 a89e94bb -> W2 14b16690 -> W3 04d5b030 -> W3-R1 17ad8d10, current certification authority remains W3 at 04d5b030, R2 closes recovery/process metadata only, W0 global manifest/guard prerequisite repair documented as historical ONE-OFF recovery deviation NOT a reusable Analyze precedent (Analyze normally analysis-only; W0 changed repository-global non-production recovery/guard files because pre-existing duplicate-key manifest regression made certified truth unreadable; repair retained because reverting would restore invalid repository truth; future global defects must surface as prerequisite blocker or separate Architect-authorized repair), no W0 history rewrite, no manifest/global-gate mutation in R2, global Host checkpoint preserved. Manifest: zero bytes touched in R2 (git diff scope proof; OperatorProfile and Notification certified truth each present exactly once, no duplicate top-level modules key, TmarCompleteReferenceStructureGateTests untouched). Bounded validation PASS: JSON parse SoT (340 top keys) OK, JSON parse manifest (25 modules) OK, exact R1 SHA assertion PASS, exact W3 certified SHA assertion PASS, Master Recovery lineage assertion PASS, focused OperatorProfile recovery/cert family 27/27 PASS without modification, shallow-structure spot check PASS (Admin/Commands, Admin/Queries, Admin/Validators each 0 child directories), git diff scope proof shows only allowed files (SoT json, Master Recovery, 3 R2 evidence docs, persisted task artifact); all other dirty/untracked files are pre-existing unrelated artifacts preserved untouched. Global recovery lock preserved exactly: lastAcceptedTask, lastAcceptedCommit, latestAcceptedImplementationWave, currentHostCheckpoint, nextHostFolder, repository-global workflowStop and automaticNextImplementationTask unchanged. Final certification authority remains W3 at 04d5b03018a8f262ee1446bf7ee5c467d29cb7b9: COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 STRUCTURE_CERTIFIED. Exactly one R2 commit, pushed, HEAD == origin/main. STOP completely: no R3, no W4, no next module; waiting for Architect review.
Starting-HEAD-State: 17AD8D10
Production-Scope-State: RECOVERY_PROCESS_METADATA_ONLY
Production-Code-Changed-State: ZERO
Current-Certification-Authority-State: W3_04D5B030
R1-Commit-State: RECORDED_17AD8D10
W0-Global-Prerequisite-Repair-State: PRESERVED_ONE_OFF_RECOVERY_DEVIATION
W0-Analyze-Precedent-State: NOT_REUSABLE
Manifest-Current-Truth-State: PRESERVED_VALID
Global-Structure-Gate-State: PRESERVED_NOT_TOUCHED
Structure-State: CERTIFIED
Folder-Granularity-State: PROFESSIONAL_SHALLOW
Per-UseCase-Request-Leaf-State: ZERO
Path-Namespace-State: EXACT
Validator-State: EXHAUSTIVE_2_OF_2
Cross-Module-Boundary-State: CONTRACTS_ONLY
Foreign-App-Infra-Domain-Endpoints-Coupling-State: ZERO
Schema-Migration-State: UNCHANGED
Global-Host-Checkpoint-State: PRESERVED
Guards-Weakened-State: NONE
Baselines-Widened-State: NONE
Json-Parse-State: PASS
Evidence-State: COMPLETE
Recovery-SoT-State: CLOSED_RECONCILED
Commit-SHA: PENDING_FILL_AT_POST
HEAD-Equals-Origin-Main: YES
Working-Tree-State: CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS
User-Work-Preserved: YES
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_OPERATORPROFILE_AMSC_001_W3_R2
STOP
END_TOOBA_WORKER_RESULT`
};

const data = JSON.stringify(payload);
const req = http.request({
  hostname: '127.0.0.1',
  port: 17321,
  path: '/api/results',
  method: 'POST',
  headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(data) }
}, res => {
  let body = '';
  res.on('data', c => body += c);
  res.on('end', () => console.log('status:', res.statusCode, 'body:', body));
});
req.on('error', e => { console.error('ERROR:', e.message); process.exit(1); });
req.write(data);
req.end();
