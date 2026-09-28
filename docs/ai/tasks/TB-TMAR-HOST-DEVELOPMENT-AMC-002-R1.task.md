PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-DEVELOPMENT-AMC-002-R1
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-AMC-002
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Development Recovery Closure
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_DEVELOPMENT_RECOVERY_REPAIR
Title: Close recovery/governance gaps for Development AMC-002 without production changes

CURRENT ACCEPTED CODE STATE

The production changes shipped by:

ba6cf54c738d443dcb61efc4264aedc8608f2b63
5919039b2313ddc8d02864e47e9f636328990cfe

are Architect-accepted for code behavior/architecture.

Do NOT reopen the Development migration.
Do NOT modify production behavior.
Do NOT start ProductWorkspaceDevelopmentBootstrap work.
Do NOT start CatalogAttributeSchemaSellableEnricher work.
Do NOT inspect or start another Host folder.

MANDATORY SKILL ORDER

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Do NOT run migrate skill. This is recovery/governance repair only.

EXACT REPAIR SCOPE

Repair only these three closure gaps:

MISSING CANONICAL TASK ARTIFACT
Persist this exact task at:
docs/ai/tasks/TB-TMAR-HOST-DEVELOPMENT-AMC-002-R1.task.md

Also preserve the historical fact that AMC-002 itself was executed without a canonical task artifact.
Do NOT fabricate an old task file as if it had existed before execution.
Record this honestly in evidence/SoT.

RECOVERY / SOT POINTERS
Reconcile docs/architecture/tmar-current-state.json so the latest accepted Development checkpoint is represented truthfully.

Required current state after PASS:

latest accepted implementation wave = TB-TMAR-HOST-DEVELOPMENT-AMC-002
current Host checkpoint = Development
Development AMC-002 state = accepted/code-pass with two explicit unresolved bounded debts
no next implementation task auto-selected
automaticNextImplementationTask = NONE
workflow stop = USER_REVIEW_HOST_DEVELOPMENT_AMC_002_R1
next task state = USER_DECISION_REQUIRED

Preserve historical Authorization and Recovery-SOT-SYNC checkpoints as history.
Do not erase unrelated recovery history.

VALIDATION COUNT CONSISTENCY
Resolve the inconsistency between:

Result text claiming 63/63
SoT/evidence currently recording 57/57

Determine the truthful count from the actual executed focused test command/evidence.
Record ONE canonical count consistently in:

tmar-current-state.json
AMC-002 evidence
R1 evidence
canonical worker result

Do NOT rerun unrelated suites merely to obtain a preferred number.
If the exact historical count cannot be proven deterministically, record:
VALIDATION_COUNT_RECONCILED_TO_VERIFIABLE_EVIDENCE
with the verifiable count and explain the discrepancy.

PRESERVE THESE ACCEPTED AMC-002 FACTS

Development production files: 12 -> 6
Deleted wrapper count: 7
Created seam:
Development/DevelopmentTenantCommerceContext.cs
Retained prior allowed files:
MarketplaceDevelopmentBootstrap.cs
MarketplaceAdminDevBootstrap.cs
MarketplaceSellerDevBootstrap.cs
Open bounded debts:
CatalogAttributeSchemaSellableEnricher.cs
ProductWorkspaceDevelopmentBootstrap.cs
No sink-folder regression
No schema change
No route change
Frontend unchanged
No automatic next implementation task

PRODUCTION CHANGE PROHIBITION

Do NOT modify:

src/backend/Host/Tooba.Host/**
src/backend/Modules/**
src/frontend/**
project/package files
migrations/schema
routes
guards except only a recovery-specific guard if strictly necessary to validate recovery metadata consistency

No architecture migration in this task.

RECOVERY FILES TO VERIFY / RECONCILE

At minimum:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/evidence/TB-TMAR-HOST-DEVELOPMENT-AMC-002/**

Do not rewrite historical sections unnecessarily.
Prefer the smallest truthful patch.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-DEVELOPMENT-AMC-002-R1/

Required:

analyze.md
recovery-repair.md
validation.md
certification.md

SoT block:
hostDevelopmentAmc002R1

Required PASS fields:

parentTask = TB-TMAR-HOST-DEVELOPMENT-AMC-002
productionCodeChangeState = ZERO
amc002CodeState = ACCEPTED
canonicalTaskArtifactState = R1_PRESENT_AMC002_HISTORICAL_ABSENCE_RECORDED
recoveryPointerState = RECONCILED
validationCountState = RECONCILED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_DEVELOPMENT_AMC_002_R1
certificationState = PASS

FOCUSED VALIDATION ONLY

Run only what is needed to prove:

JSON/recovery files parse and agree
current checkpoint is Development AMC-002
no automatic next implementation task exists
R1 task artifact exists
validation count is consistent across current SoT/evidence/result
production tree SHA/content for accepted AMC-002 code is unchanged by R1

Do NOT run solution-wide tests.
Do NOT reopen migration validation.
Do NOT fix unrelated failures.

SUCCESS CRITERIA

PASS only if:

zero production code change
recovery files agree on Development AMC-002 as latest accepted implementation checkpoint
AMC-002 missing historical task artifact is recorded honestly, not backfilled falsely
R1 canonical task artifact exists
validation count discrepancy is resolved truthfully
no automatic next implementation task is selected
both Development blockers remain unresolved and explicitly deferred
workflow stops for user/Architect decision

GIT

Work from latest main.

No reset.
No clean.
No rebase.
No force-push.
Preserve user work.

Commit and push main only on PASS.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-DEVELOPMENT-AMC-002-R1
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-AMC-002
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
AMC002-Code-Acceptance-State:
Production-Code-Change-State:
Canonical-Task-Artifact-State:
Historical-AMC002-Task-Artifact-State:
Recovery-Pointer-State:
Current-Host-Checkpoint-State:
Validation-Count-State:
Validation-Count:
CatalogAttributeSchemaSellableEnricher-State:
ProductWorkspaceDevelopmentBootstrap-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Focused-Validation-State:
Certification-State:
Evidence-Path:
SoT-State:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.

Do not ask what to do next.
Do not create another task.
Do not start either Development blocker.
Do not inspect the next Host folder.
Wait for Architect/user review.

END_TOOBA_TASK
