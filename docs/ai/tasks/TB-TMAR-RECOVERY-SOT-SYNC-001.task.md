PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-RECOVERY-SOT-SYNC-001
Parent-Task: TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Recovery Source-of-Truth Synchronization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: RECOVERY_SOT_CLOSURE
Title: Reconcile TMAR recovery documents to the actual post-Authorization checkpoint and stop

CURRENT ACCEPTED CHECKPOINT

Root Global Boundaries R3: FINAL CERTIFIED
Task: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R3
Commit: 7d8ea21155109def56866eee2acdab2067fb457b
Authorization Post-Cert Cleanup: PASS / Architect accepted for recovery synchronization
Task: TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001
Worker-result commit/stamp observed on main: 736f23d34acb4f3989144f27675d1768fc7a65a9
SoT currently records lastAcceptedTask correctly but contains stale/inconsistent recovery pointers.
Do NOT start another architecture migration in this task.

PURPOSE

Repair the durable TMAR recovery Source of Truth so a fresh chat/architect can recover the CURRENT checkpoint without falling back to stale Fulfillment, AddressBook, Authentication, Admin, or older Root-Global-Boundaries next-task markers.

This is a RECOVERY/DOCUMENTATION closure task only.

MANDATORY SKILL ORDER

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Do NOT run the migrate skill because production migration is forbidden in this task.

AUTHORITATIVE FILES TO RECONCILE

Read completely where relevant:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
latest relevant docs/evidence/*/recovery-sot.md files if present
current git log around:
7d8ea21155109def56866eee2acdab2067fb457b
Authorization post-cert implementation/SoT commits
736f23d34acb4f3989144f27675d1768fc7a65a9

MANDATORY RECONCILIATION

Make TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001 the unambiguous latest accepted TMAR checkpoint.

Resolve commit semantics truthfully.

Inspect repository convention before changing fields.
If lastAcceptedCommit means implementation commit while a later docs-only SoT stamp exists, preserve that distinction explicitly.
If needed, add/repair a separate lastAcceptedSoTStamp.
Do NOT pretend an implementation commit and a recovery-stamp commit are the same commit.
All recorded SHA values must exist on current main.

Remove/repair stale global "next task" statements that can incorrectly resume at:

Fulfillment
AddressBook
Authentication
Admin
Root Global Boundaries R2/R3
when those are historical checkpoints rather than the CURRENT continuation point.

Reconcile currentHostEvacuation.

It must NOT claim AddressBook is the active current task if that is historical.
Preserve historical AddressBook/Authentication/Admin facts in their dedicated history blocks.
The CURRENT state must reflect that Authorization post-cert cleanup is complete and the workflow is intentionally stopped for user/architect review.
Do NOT invent the next Host folder.
Do NOT start or nominate a new migration task.

Add a clear current stop state:

workflowStop = USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001
nextTask = USER_DECISION_REQUIRED
or the repository's equivalent canonical representation.
There must be NO automatic next implementation task.

Preserve all accepted historical architecture facts.

Do not delete useful historical task lineage.
Fix only misleading "current", "next", "active", "last accepted", or recovery-bootstrap pointers.

Ensure recovery bootstrap text tells a future architect to prefer:

tmar-current-state.json
latest accepted recovery block / SoT stamp
Master Recovery history
and explicitly warns that historical "Next task" lines inside old sections are non-authoritative.

PRODUCTION CODE PROHIBITION

ZERO production-code changes.

Do NOT modify:

src/backend/**
src/frontend/**
project files / package references
routes
schemas / migrations
tests unrelated to recovery consistency
module manifests except only if a recovery guard itself proves a documentation metadata inconsistency and the manifest is actually wrong; otherwise leave manifests untouched.

DURABLE RECOVERY GUARD

Strengthen or add the smallest recovery guard necessary to prove:

tmar-current-state.json.lastAcceptedTask == TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001
current stop/gate is user-review / user-decision, not an implementation task
current active Host checkpoint does not regress to AddressBook/Fulfillment/Authentication/Admin historical markers
Master Recovery contains the Authorization post-cert checkpoint
no top-level authoritative Next task: points to a historical implementation task
recorded current commit/SoT stamp SHA(s) exist in git history if the existing guard framework can validate this deterministically

Do not create a large new testing framework.

FOCUSED VALIDATION

Run only recovery/documentation guards necessary for this task.

At minimum:

existing recovery staleness guard(s)
durable TMAR recovery guard(s) affected by the edits
JSON parse/schema validation for tmar-current-state.json
focused grep/check proving current checkpoint and stop marker are unique and unambiguous

No solution-wide build.
No module build unless an existing recovery guard project requires compilation.
No unrelated test suites.
No production behavior validation.

EVIDENCE

Create:

docs/evidence/TB-TMAR-RECOVERY-SOT-SYNC-001/

Required:

analyze.md
reconciliation.md
validation.md
certification.md
recovery-sot.md

Persist exact task:
docs/ai/tasks/TB-TMAR-RECOVERY-SOT-SYNC-001.task.md

SoT dedicated block:
recoverySotSync001

Required PASS fields:

latestAcceptedTask = TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001
authorizationPostcertCleanupState = CERTIFIED
rootGlobalBoundariesR3State = CERTIFIED_PRESERVED
staleCurrentPointersState = ZERO
currentHostEvacuationState = RECONCILED_NOT_HISTORICAL_ADDRESSBOOK
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001
certificationState = PASS

SUCCESS CRITERIA

PASS only if:

recovery files agree on the CURRENT checkpoint
Authorization post-cert cleanup is discoverable from a fresh recovery
historical sections remain historical and cannot override current state
no stale active AddressBook/Fulfillment/Authentication/Admin pointer remains in authoritative current fields
commit/stamp semantics are explicit and truthful
no automatic next implementation task is selected
zero production code change
focused recovery validations pass
user work preserved

GIT

Work from latest main.

No reset.
No clean.
No rebase.
No force-push.
Preserve user work.

Commit and push main only on PASS.
On ambiguous recovery truth, return INCOMPLETE with exact conflicting fields/files and STOP.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-RECOVERY-SOT-SYNC-001
Parent-Task: TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Authorization-Postcert-Acceptance-State:
Root-Global-Boundaries-R3-Preservation-State:
Latest-Accepted-Task-State:
Commit-Semantics-State:
Master-Recovery-Current-State:
Tmar-Current-State-State:
Architect-Bootstrap-State:
Recovery-Context-State:
Current-Host-Evacuation-State:
Historical-Next-Pointers-State:
Automatic-Next-Implementation-Task-State:
Recovery-Guard-State:
Production-Code-Change-State:
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
Do not start another Host folder.
Do not create the next implementation task.
Wait for user/architect decision.

END_TOOBA_TASK