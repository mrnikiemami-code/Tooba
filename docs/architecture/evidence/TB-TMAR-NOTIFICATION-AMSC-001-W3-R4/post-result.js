const http = require('http');

const payload = {
  channelId: 'tooba-main',
  taskId: 'TB-TMAR-NOTIFICATION-AMSC-001-W3-R4',
  content: `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-NOTIFICATION-AMSC-001-W3-R4
Parent-Task: TB-TMAR-NOTIFICATION-AMSC-001-W3-R3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS
Summary: Bounded recovery/cert metadata reconciliation only (zero production, zero structural, zero behavior change). SoT: notificationModuleAmsc001W3R1.commit/commitFull = 7b8ab79e/7b8ab79e6f16d4eccd2eeaba5974936c109dba9b added (R1 certifiedCommit for W3 preserved at 763dab1393aaf823e78d8e58ea566f2ad0d99e63), notificationModuleAmsc001W3R2.commit/commitFull = 028ef769/028ef769c4face48308a9dffb7f9714826e1be7c added (state STRUCTURE_REPAIRED_READY_FOR_RECERTIFY and structureState READY_FOR_CERTIFY preserved), notificationModuleAmsc001W3R3.commit/commitFull = e3eb185b/e3eb185ba109779f395d64e35b3704e1439b40ae added with recoveryFollowupState = RECONCILED_BY_TB_TMAR_NOTIFICATION_AMSC_001_W3_R4 (historical recoveryFollowupRequired text preserved; every certification field preserved verbatim: NOTIFICATION_AMSC_001_RECERTIFIED / COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 / structureCertified true / CERTIFIED / gate W3-R2 / PROFESSIONAL_SHALLOW / ZERO / ZERO / EXACT / CONTRACTS_ONLY / ZERO / ZERO / ZERO / microserviceExtractable true / blockingResidualDebt ZERO / automaticNext NONE). Master Recovery: R3 checkpoint now records final SHA e3eb185b and explicit full lineage W0 5777ff4a -> W1 c660b933 -> W2 e2f23975 -> W3 763dab13 -> W3-R1 7b8ab79e -> W3-R2 028ef769 -> W3-R3 e3eb185b; states clearly original W3 certification historical/superseded for current structure authority, W3-R2 valid repaired Structure handoff, W3-R3 at e3eb185b current authoritative certification, W3-R4 closes Recovery/metadata truth only, global Host root checkpoint untouched; additive closing R4 checkpoint appended (R4 does not self-record its own SHA per task). Manifest: ONLY Notification -> Tooba.Notification.Application -> rootAllowlistJustification touched - stale phrase "one folder per use case each carrying request+handler and validator files" replaced with truthful current text ("all request and validator source files live directly on those axes with zero per-use-case leaf directories (single-file and per-use-case request leaf states are ZERO)"); module/project membership, rootAllowlist arrays, forbiddenRootFiles, forbiddenTopLevelFolders, module-level structureCertified=true and lockVersion=ARCH-COMPLETE-002 all unchanged; certificationNote not modified (already truthful). Additive SoT notificationModuleAmsc001W3R4 recorded (state = NOTIFICATION_AMSC_001_RECOVERY_CLOSED, productionCodeChanged false, currentCertificationAuthority TB-TMAR-NOTIFICATION-AMSC-001-W3-R3, currentCertifiedCommit e3eb185ba109779f395d64e35b3704e1439b40ae, r1CommitState RECORDED_7B8AB79E, r2CommitState RECORDED_028EF769, r3CommitState RECORDED_E3EB185B, manifestMetadataState RECONCILED_TO_CURRENT_SHALLOW_TREE, globalHostCheckpointState PRESERVED, manifestStructuralState STRUCTURAL_FIELDS_UNCHANGED_METADATA_TEXT_ONLY, schemaMigrationState UNCHANGED, frontendState FROZEN_UNCHANGED, guardsWeakened NONE, baselinesWidened NONE, workflowStop USER_REVIEW_NOTIFICATION_AMSC_001_W3_R4, automaticNextImplementationTask NONE, no self-referential commit field). Durable metadata guard: NotificationModuleAmsc001W3R3RecertGuardTests minimally extended with Recovery_chain_metadata_is_fully_reconciled asserting R1/R2/R3 full SHAs, R3 reconciled followup, R4 closing state/authority, stale phrase absence + current truth presence, module structureCertified/lockVersion intact, global Host checkpoint preserved, automatic next NONE; prior assertions untouched, no guard weakened (now 5/5 passed). Bounded validation PASS: JSON parse both SoT files OK; SoT/Master-Recovery SHA assertions exact; manifest stale-phrase-absence/current-phrase-presence exact; zero-child-directory scan under the four request axes flat; git diff scope proof shows only the eight allowed files (2 SoT/manifest JSON, Master Recovery, R3 recert guard metadata lock, task artifact, 3 evidence docs); no production/csproj/Host/frontend/validator/resx/migration file touched. Global recovery lock preserved exactly: lastAcceptedTask TB-TMAR-HOST-ROOT-FINAL-CERT-001, currentHostCheckpoint HOST_ROOT_FINAL_CERTIFIED, repository-global workflowStop USER_REVIEW_HOST_ROOT_FINAL_CERT_001 and automaticNextImplementationTask NONE unchanged. Final certification authority remains W3-R3 at e3eb185ba109779f395d64e35b3704e1439b40ae: COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 STRUCTURE_CERTIFIED. Exactly one R4 commit, pushed, HEAD == origin/main. STOP completely: no R5, no W4, no next module; waiting for Architect review.
Starting-HEAD-State: E3EB185B
Production-Scope-State: RECOVERY_CERT_METADATA_ONLY
Production-Code-Changed-State: ZERO
Current-Certification-Authority-State: W3_R3_E3EB185B
R1-Commit-State: RECORDED_7B8AB79E
R2-Commit-State: RECORDED_028EF769
R3-Commit-State: RECORDED_E3EB185B
Manifest-Metadata-State: RECONCILED_CURRENT_SHALLOW_TREE
Manifest-Structural-State: UNCHANGED
Structure-State: CERTIFIED
Folder-Granularity-State: PROFESSIONAL_SHALLOW
Single-File-Request-Leaf-State: ZERO
Per-UseCase-Request-Leaf-State: ZERO
Path-Namespace-State: EXACT
Cross-Module-Boundary-State: CONTRACTS_ONLY
Foreign-App-Infra-Domain-Endpoints-Coupling-State: ZERO
Schema-Migration-State: UNCHANGED
Global-Host-Checkpoint-State: PRESERVED
Guards-Weakened-State: NONE
Baselines-Widened-State: NONE
Json-Parse-State: PASS
Evidence-State: COMPLETE
Recovery-SoT-State: CLOSED_RECONCILED
Commit-SHA: fa5bc0e99b2eab2a73fc77bc63e402c9f673448e
HEAD-Equals-Origin-Main: YES
Working-Tree-State: CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS
User-Work-Preserved: YES
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_NOTIFICATION_AMSC_001_W3_R4
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
