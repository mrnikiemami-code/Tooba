PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-AMSC-001-W3-R3
Parent-Task: TB-TMAR-PAYMENT-AMSC-001-W3-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: RECOVERY_HISTORICAL_TRUTH_PRESERVATION_ONLY
Title: Restore Payment W1 historical field and keep R2 correction additive

ARCHITECT VERDICT
Payment current production architecture and R2 authoritative 28/27/24 truth are accepted.

One Recovery integrity defect remains:
R2 was instructed to preserve the historical W1 text and reconcile it additively.
Instead, paymentAmsc001W1.stableErrorCodeState was rewritten from the historical value:
CONTRACTS_OWNED_28_DECLARED_24_REGISTERED_3_INTENTIONALLY_CONSUMED_ISKNOWN_GUARDED
to:
CONTRACTS_OWNED_28_DECLARED_24_REGISTERED_4_INTENTIONALLY_CONSUMED_ISKNOWN_GUARDED

At the same time R2 added:
stableErrorCodeStateReconciliation = HISTORICAL_W1_TEXT_SAID_3_CONSUMED; AUTHORITATIVE_COUNT_IS_4_FOREIGN_DECLARED_CONSUMED...

Therefore current SoT contradicts its own claim that W1 historical text was preserved.

This is a Recovery/history integrity repair only.
Do not change current authoritative production truth:
28 declared / 27 KnownCodes / 24 Payment-owned descriptors / 4 foreign declared-consumed / 3 foreign in KnownCodes / admin.authorization.denied excluded.

STARTING HEAD
83bc22175d92e53b838c3039cc09ee3e790c509c

CURRENT CERTIFICATION AUTHORITY
TB-TMAR-PAYMENT-AMSC-001-W3
502d73e0e9ccfb277a06ad397b1f0a511f586921
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002
STRUCTURE_CERTIFIED

CURRENT RECOVERY AUTHORITY
TB-TMAR-PAYMENT-AMSC-001-W3-R2
83bc22175d92e53b838c3039cc09ee3e790c509c

GOAL

restore the W1 historical field to its original 3-consumed value;
keep the R2 additive reconciliation field and R2 authoritative 28/27/24 truth intact;
make SoT and Master Recovery agree that W1 history is preserved, not rewritten;
preserve R1/R2 lineage and all W3 authority;
zero production/manifest/schema behavior change;
automaticNextImplementationTask = NONE.

PRECHECK

Verify branch = main.
Verify HEAD == origin/main == 83bc22175d92e53b838c3039cc09ee3e790c509c.
Verify R2 parent == affdfba4fc5fce402d05e68131bdb4a01899b93c.
Verify current W1 field is the rewritten 4-consumed value.
Verify R2 reconciliation metadata says historical W1 text was 3 consumed.
Verify R2 authoritative truth:
declaredStableCodeCount = 28
knownCodeGuardMemberCount = 27
paymentOwnedDescriptorCount = 24
foreignOwnedDeclaredConsumedCount = 4
foreignOwnedKnownGuardCount = 3
foreignOwnedNotInKnownGuard = admin.authorization.denied
Verify W3 authority remains 502d73e0...
If any fact differs materially: RECOVERY_CONFLICT + STOP.

GLOBAL RECOVERY LOCK
Preserve exactly:

lastAcceptedTask
lastAcceptedCommit
latestAcceptedImplementationWave
currentHostCheckpoint
nextHostFolder
repository-global workflowStop
repository-global automaticNextImplementationTask

ALLOWED FILES

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md only if a tiny R3 closure note is needed
docs/architecture/evidence/TB-TMAR-PAYMENT-AMSC-001-W3-R3/*
docs/ai/tasks/TB-TMAR-PAYMENT-AMSC-001-W3-R3.task.md

FORBIDDEN

production files
guards
manifest
csproj
Host production
frontend
resources/descriptors/error-code files
schema/migrations
changing R2 authoritative counts
changing W3 authority
changing semantic wave SHAs
unrelated cleanup
next module

IMPLEMENTATION

RESTORE HISTORICAL W1 FIELD
Set exactly:
paymentAmsc001W1.stableErrorCodeState =
CONTRACTS_OWNED_28_DECLARED_24_REGISTERED_3_INTENTIONALLY_CONSUMED_ISKNOWN_GUARDED

This is historical truth preservation.
Do NOT remove or weaken:
paymentAmsc001W1.stableErrorCodeStateReconciliation

That additive reconciliation must remain and continue to point to R2 current authoritative truth.

PRESERVE R2 AUTHORITATIVE TRUTH
Do not change:
declaredStableCodeCount = 28
knownCodeGuardMemberCount = 27
paymentOwnedDescriptorCount = 24
foreignOwnedDeclaredConsumedCount = 4
foreignOwnedKnownGuardCount = 3
foreignOwnedNotInKnownGuard = admin.authorization.denied
stableErrorTruthState = AUTHORITATIVE_28_DECLARED_27_KNOWN_24_OWNED_DESCRIPTORS
currentCertificationAuthority = TB-TMAR-PAYMENT-AMSC-001-W3
currentCertifiedCommit = 502d73e0e9ccfb277a06ad397b1f0a511f586921
ADD FINAL R3 CLOSURE BLOCK
Add paymentAmsc001W3R3:
task = TB-TMAR-PAYMENT-AMSC-001-W3-R3
parentTask = TB-TMAR-PAYMENT-AMSC-001-W3-R2
mode = RECOVERY_HISTORICAL_TRUTH_PRESERVATION_ONLY
startingHead = 83bc2217
state = PAYMENT_AMSC_001_RECOVERY_FINAL_CLOSED
productionCodeChanged = false
currentCertificationAuthority = TB-TMAR-PAYMENT-AMSC-001-W3
currentCertifiedCommit = 502d73e0e9ccfb277a06ad397b1f0a511f586921
recoveryAuthority = TB-TMAR-PAYMENT-AMSC-001-W3-R2
recoveryCommit = 83bc22175d92e53b838c3039cc09ee3e790c509c
historicalW1StableErrorCodeState = CONTRACTS_OWNED_28_DECLARED_24_REGISTERED_3_INTENTIONALLY_CONSUMED_ISKNOWN_GUARDED
historicalW1FieldState = RESTORED_AND_PRESERVED
additiveReconciliationState = PRESERVED
authoritativeStableErrorTruth = AUTHORITATIVE_28_DECLARED_27_KNOWN_24_OWNED_DESCRIPTORS
manifestStructuralState = NOT_TOUCHED
schemaMigrationState = UNCHANGED
globalHostCheckpointState = PRESERVED
guardsWeakened = NONE
baselinesWidened = NONE
workflowStop = USER_REVIEW_PAYMENT_AMSC_001_W3_R3
automaticNextImplementationTask = NONE

Do NOT add self-referential R3 commit placeholder.

MASTER RECOVERY
If needed, append one short R3 closure note stating:
R2 authoritative 28/27/24 truth remains unchanged;
R3 only restores the historical W1 3-consumed field so history matches the actual W1 record;
additive R2 reconciliation remains authoritative for current truth;
W3 remains certification authority;
production/manifest/schema unchanged;
automatic next NONE.

Do not rewrite W1 historical checkpoint prose except where necessary to preserve the original historical wording.

EVIDENCE
Create:
docs/architecture/evidence/TB-TMAR-PAYMENT-AMSC-001-W3-R3/

Required:

historical-truth-restoration.md
validation.md

Evidence must show:

W1 historical field before R2
rewritten value at R2
restored value at R3
R2 additive reconciliation field preserved
R2 authoritative 28/27/24 counts preserved
W3 authority preserved
production/manifest/schema diff ZERO
Host checkpoint preserved

BOUNDED VALIDATION

JSON parse SoT
assert W1 historical field == original 3-consumed value
assert W1 reconciliation field exists
assert R2 28/27/24 + 4/3/admin-excluded fields unchanged
assert W3 authority unchanged
git diff scope proof

No builds required.
No tests required.
No production edits.

PASS CRITERIA

historical W1 field restored exactly
R2 additive truth preserved
R2 authoritative counts unchanged
W3 authority unchanged
production/manifest/schema untouched
global Host checkpoint preserved
automatic next NONE

COMMIT/PUSH
If PASS:

exactly one R3 recovery-integrity commit
push main
verify HEAD == origin/main
STOP

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PAYMENT-AMSC-001-W3-R3
Parent-Task: TB-TMAR-PAYMENT-AMSC-001-W3-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: 83BC2217 | DIVERGED
Production-Code-Changed-State: ZERO | NONZERO
Current-Certification-Authority-State: W3_502D73E0 | CONFLICT
Recovery-Authority-State: R2_83BC2217 | CONFLICT
Historical-W1-Field-State: RESTORED_3_CONSUMED | REWRITTEN | CONFLICT
Additive-Reconciliation-State: PRESERVED | REGRESSED
Authoritative-Error-Truth-State: EXACT_28_27_24_4_3_ADMIN_EXCLUDED | CONFLICT
R1-Lineage-State: PRESERVED | REGRESSED
R2-Lineage-State: PRESERVED | REGRESSED
Manifest-Structural-State: NOT_TOUCHED | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Global-Host-Checkpoint-State: PRESERVED | REGRESSED
Guards-Weakened-State: NONE | NONZERO
Baselines-Widened-State: NONE | NONZERO
Json-Parse-State: PASS | FAIL
Evidence-State: COMPLETE | INCOMPLETE
Recovery-SoT-State: FINAL_CLOSED | STALE | CONFLICT
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PAYMENT_AMSC_001_W3_R3
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP completely after result.
No R4.
No W4.
No next module.
Wait for Architect review.

END_TOOBA_TASK