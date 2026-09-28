PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-REMAINDER-AUDIT-001
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-010-FINAL-CERT
Parent-Commit: b9cf989da03e6971e7b999d3a92ce6e05d960578
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_REMAINDER_AUDIT
Title: Inventory and classify the remaining Host surface after Admin certification

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-ADMIN-CANON-010-FINAL-CERT is ARCHITECT-ACCEPTED at:
b9cf989da03e6971e7b999d3a92ce6e05d960578

src/backend/Host/Tooba.Host/Admin/** is now:
CANONICAL_PLATFORM_BOUNDARY_CERTIFIED

Do not modify it.

GOAL

Audit the remaining production Host surface and produce the next bounded migration/certification order.

This task is ANALYSIS ONLY.

DO NOT migrate, refactor, rename, or repair production code.

SCOPE

Audit:
src/backend/Host/Tooba.Host/**

Exclude from classification work:

Admin/** (already certified; verify only that it remains untouched)
generated bin/**, obj/**

For every top-level Host production folder/file outside Admin, classify as exactly one:

PLATFORM_KEEP
READY_TO_MIGRATE
NEEDS_PRECERT_REPAIR
GLOBAL_HOST_BOUNDARY_REVIEW
DEVELOPMENT_ONLY_REVIEW

MANDATORY AUDITS

For each top-level Host area capture:

production .cs file count
route ownership / mapped HTTP endpoints, if any
direct dependencies on module:
Application
Infrastructure
Domain
Contracts
Endpoints
direct persistence:
DbContext
EF/IQueryable
SaveChanges
transaction
business ownership:
module-specific reads/writes
business rules
module-specific DTO/model ownership
service locator:
RequestServices
GetRequiredService in runtime HTTP paths
CQRS posture:
direct writes versus ISender/Contracts
semantic errors:
ex.Message/message parsing
hardcoded stable machine-code gaps
Development-only residue
existing architecture guards / certification evidence

SPECIAL CASES

Authentication:

previous architecture decision is KEEP_AS_GLOBAL_HOST_PLATFORM_BOUNDARY
re-audit only for drift
do not reopen ownership unless current code contradicts that lock

Admin:

state must remain CANONICAL_PLATFORM_BOUNDARY_CERTIFIED
no changes

OUTPUT / PRIORITY

Produce a ranked execution queue, but ranking is by architectural dependency/order, not subjective quality.

For each area include:

exact folder/file scope
classification
blockers
recommended next task ID/title
expected task size: TINY / SMALL / MEDIUM
expected execution time bucket:
<=10 min
10–15 min
15–25 min

The FIRST recommended next task must be the smallest high-confidence actionable area.
Do not start it.

ANTI-LOOP / BUDGET

Analysis only.

Allowed validation:

one Host source inventory/search pass
one focused architecture-guard/test discovery pass
optional dotnet build Tooba.Host ONCE only if needed to validate current baseline

MAX_VALIDATION_COMMAND_RUNS = 3
MAX_REPAIR_ITERATIONS = 0

No production repair.
No repeated scans.
No broad test suite.
No solution-wide tests.
No loop.

EVIDENCE

Create only:
docs/evidence/TB-TMAR-HOST-REMAINDER-AUDIT-001/

inventory.md
classification.md
execution-queue.md

Persist task:
docs/ai/tasks/TB-TMAR-HOST-REMAINDER-AUDIT-001.task.md

RECOVERY SOT

Append/update:
hostRemainderAudit001

Record:

parentCommit
adminState=CANONICAL_PLATFORM_BOUNDARY_CERTIFIED
totalNonAdminProductionFiles
platformKeepAreas
readyToMigrateAreas
needsPrecertRepairAreas
globalHostBoundaryReviewAreas
developmentOnlyReviewAreas
firstRecommendedTask
workflowStop=USER_REVIEW_HOST_REMAINDER_AUDIT_001

SUCCESS CRITERIA

PASS only if:

the remaining Host is inventoried without production edits
every top-level production area outside Admin is classified
concrete forbidden dependencies/residue are cited by file/symbol
execution queue is bounded and actionable
Admin certification remains untouched
evidence/task/SoT only are changed
commit pushed
HEAD == origin/main
tracked working tree clean

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-REMAINDER-AUDIT-001
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-010-FINAL-CERT
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Admin-Certification-Preservation-State:
NonAdmin-Production-File-Count:
TopLevel-Area-Count:
PlatformKeep-Areas:
ReadyToMigrate-Areas:
NeedsPrecertRepair-Areas:
GlobalHostBoundaryReview-Areas:
DevelopmentOnlyReview-Areas:
ForeignApplication-Residue-Summary:
ForeignInfrastructure-Residue-Summary:
ForeignDomain-Residue-Summary:
DirectPersistence-Residue-Summary:
ServiceLocator-Residue-Summary:
MessageParsing-Residue-Summary:
Authentication-State:
First-Recommended-Task:
Execution-Queue-State:
Focused-Validation:
Evidence-Path:
SoT-State:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
Validation-Command-Runs:
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP COMPLETELY.

Do not start the first recommended task.
Wait for Architect review.

END_TOOBA_TASK