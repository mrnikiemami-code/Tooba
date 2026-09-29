PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R3
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Seller AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SELLER_R3_PARTY_SETTINGS
Title: Evacuate Seller settings from Host/Seller to Party-owned endpoints

MANDATORY SKILLS

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

ACCEPTED BASELINE

latest accepted implementation: TB-TMAR-HOST-SELLER-AMC-001-R2
commit: bc7f43cf04eccd068c12fdc13dcca7901df1c476
Host/Seller routes = 4
Host/Seller files = 5
Host/Security/Seller canonical and protected

SCOPE — PARTY SETTINGS ONLY
Evacuate ONLY:

GET /v1/seller/settings
PUT /v1/seller/settings
SellerSettingsEndpoints.cs
seller settings request/response transport models
seller.settings.view/manage capability checks

DO NOT touch dashboard, dev-contexts, SellerPanelComposer dashboard work, SellerDevActorBootstrap, frontend, R4/R5.

TARGET
Party owns Seller settings.

If Tooba.Party.Endpoints does not exist, create the minimal canonical Endpoints project and wire it normally.

Required final ownership:

Party.Endpoints/Seller owns both routes
Party.Application owns CQRS read/write requests/handlers
Party.Infrastructure owns persistence where appropriate
Endpoints use ISender only
no DbContext in Endpoints
write validator required; read validator classification explicit
ApiResponseFactory/SemanticError canonical
exact path↔namespace

AUTHORIZATION
Keep seller platform authorization in Host.
Party.Endpoints may define a narrow IPartySellerAuthorizer port.
Host implementation must live under Host/Security/Seller and use canonical seller boundary + IPlatformEffectiveAccessReader.
Host adapter: ZERO foreign Application/Domain/Infrastructure/Persistence, no service locator.

Eliminate from Host/Seller for this slice:

Tooba.AccessControl.Application
Tooba.AccessControl.Domain
IAccessControlDirectory
Party Application settings dependencies

BEHAVIOR PARITY
Preserve exact paths, verbs, seller auth, seller.settings.view/manage, response shape, canManage, update behavior, 404 missing, 400 rejected, and stable codes:

seller.settings.missing
seller.settings.rejected
seller.authorization.denied

No ex.Message HTTP classification.
No schema/migration change.

HOST FINAL STATE
Delete Host/Seller/SellerSettingsEndpoints.cs.

Host/Seller must then contain exactly 4 files:

SellerPanelEndpoints.cs
SellerPanelComposer.cs
SellerPanelModels.cs
SellerDevActorBootstrap.cs

Host-owned Seller routes: 4 -> 2
Remaining:

GET /v1/seller/dashboard
GET /v1/seller/dev-contexts

No duplicate routes.

GUARDS
Focused guard must prove:

settings routes Party-owned
Host settings routes ZERO
SellerSettingsEndpoints absent
Party Endpoints ISender-only
Party Endpoints DbContext ZERO
validator coverage correct
Host/Seller AccessControl Application/Domain settings leakage ZERO
Host security adapter neutral/contracts only
behavior parity
R1A/R2 security invariants preserved
no sink-folder regression

FOCUSED VALIDATION ONLY
Build Party.Application, Party.Infrastructure if touched, Party.Endpoints, Host, focused tests only.
Run R3 guard, relevant Party seller settings tests, HostSellerAmcR1GuardTests, HostSellerAmcR2GuardTests, TmarDurableGuardTests, route ownership guard if needed.
No solution-wide tests.

RECOVERY / SOT — SAME TASK
On PASS:

lastAcceptedTask = TB-TMAR-HOST-SELLER-AMC-001-R3
lastAcceptedCommit = actual R3 implementation commit
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentTask/latestAcceptedImplementationWave = R3
currentHostCheckpoint = Seller
workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R3
nextTask = USER_REVIEW_HOST_SELLER_AMC_001_R3
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
full Host/Seller remains OPEN
Seller-R4 NOT_STARTED

Add hostSellerAmcR3 with:

Party settings routes migrated = 2
Host Seller routes 4 -> 2
Host/Seller files 5 -> 4
SellerSettingsEndpoints ABSENT
fullSellerFolderCertification = NOT_YET
sellerR4State = NOT_STARTED

Reconcile CURRENT/AUTHORITATIVE sections:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

No separate recovery repair if implementation passes.

EVIDENCE
docs/evidence/TB-TMAR-HOST-SELLER-AMC-001-R3/

analyze.md
migration.md
route-ownership.md
validation.md
certification.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SELLER-AMC-001-R3.task.md

SUCCESS
PASS only if:

settings routes Party-owned
Host duplicate routes ZERO
Host/Seller route count = 2
Host/Seller file count = 4
SellerSettingsEndpoints absent
Host settings layer leakage removed
behavior parity preserved
Recovery synchronized
automaticNextImplementationTask = NONE
frontend unchanged
user work preserved

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Commit and push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R3
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
R2-State:
Party-Endpoints-Foundation-State:
Party-Seller-Settings-Route-Ownership-State:
Routes-Migrated:
Host-Seller-Route-Count-Before:
Host-Seller-Route-Count-After:
Host-Seller-File-Count-Before:
Host-Seller-File-Count-After:
SellerSettingsEndpoints-State:
Party-Endpoints-ISender-State:
Party-Endpoint-DbContext-State:
Party-Seller-CQRS-State:
Validator-State:
Host-Seller-Settings-Layer-Leakage-State:
Seller-Authorization-State:
Behavior-Parity-State:
Error-Code-State:
Host-Security-Seller-State:
Schema-Change-State:
Frontend-State:
Sink-Folder-Regression-State:
Full-Seller-Folder-Certification-State:
Seller-R4-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Certification-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.
Do not start Seller-R4.
Wait for Architect/user review.

END_TOOBA_TASK
