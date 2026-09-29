PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R4
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Seller AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SELLER_R4_DASHBOARD
Title: Evacuate seller dashboard from Host/Seller and remove dashboard composer/models residue

MANDATORY SKILLS

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

ACCEPTED BASELINE

latest accepted implementation: TB-TMAR-HOST-SELLER-AMC-001-R3
commit: 5ab8bdc7f5e75c599313d8b444d18a4f6c80ffdf
Host/Seller files = 4
Host-owned Seller routes = 2
remaining routes: GET /v1/seller/dashboard, GET /v1/seller/dev-contexts

SCOPE — DASHBOARD ONLY
Evacuate:

GET /v1/seller/dashboard
SellerDashboardSummary
SellerPanelComposer display-name composition
Host dependency on Order.Application dashboard query
Host dependency on Party.Application for dashboard display

Do NOT touch dev-contexts, SellerDevActorBootstrap, frontend, R5 work.

OWNERSHIP
Current dashboard is Order-owned in substance: dynamic metrics are Order metrics; display name is enrichment; ActiveOffers is fixed 0.
Move route to Tooba.Order.Endpoints/Seller and use Order Application CQRS.
Order Application may consume Party.Contracts only for seller display enrichment.
No Party.Application, no foreign DbContext.

Required:
HTTP -> ISender -> Order Application query/handler -> Order-owned abstractions + Party.Contracts -> ApiResponseFactory.

AUTHORIZATION
Reuse existing Order seller authorization boundary/port.
Do not create a second seller-auth mechanism.
Host/Security/Seller remains thin adapter only.

BEHAVIOR PARITY
Preserve exact:

GET /v1/seller/dashboard
seller auth
seller.missing
response fields: sellerPartyId, sellerDisplayName, activeOffers, openOrders, paidOrders
ActiveOffers = 0
status/error semantics
no schema/frontend change

HOST FINAL STATE
SellerPanelEndpoints.cs retains ONLY /v1/seller/dev-contexts.

Delete if zero-consumer:

Host/Seller/SellerPanelComposer.cs
Host/Seller/SellerPanelModels.cs

The six Offer DTO global aliases are not justification to retain SellerPanelModels. If zero-consumer, delete; do not relocate dead aliases.

Expected Host/Seller after R4:

SellerPanelEndpoints.cs
SellerDevActorBootstrap.cs

Expected:

files 4 -> 2
Host Seller routes 2 -> 1
remaining route = GET /v1/seller/dev-contexts

Host/Seller must have ZERO:

Order.Application
Party.Application
dashboard composer
dashboard response model

GUARDS
Focused R4 guard proves:

dashboard absent from Host
dashboard Order.Endpoints-owned
duplicate route ZERO
Order endpoint ISender-only
endpoint DbContext ZERO
Party boundary = Contracts only
Composer absent
Models absent if aliases zero-consumer
Host route count = 1
Host file count = 2
R1A/R2/R3 invariants preserved
no sink-folder regression

FOCUSED VALIDATION ONLY
Build Order.Application, Order.Endpoints, Host, focused tests only.
Run R4 guard, affected Order dashboard tests, HostSellerAmcR1/R2/R3 guards, TmarDurableGuardTests, route ownership guard if needed.
No solution-wide tests.

RECOVERY / SOT — SAME TASK
On PASS:

lastAcceptedTask = TB-TMAR-HOST-SELLER-AMC-001-R4
lastAcceptedCommit = actual R4 implementation commit
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentTask/latestAcceptedImplementationWave = R4
currentHostCheckpoint = Seller
workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R4
nextTask = USER_REVIEW_HOST_SELLER_AMC_001_R4
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
Seller-R5 NOT_STARTED
full Host/Seller remains OPEN

Add hostSellerAmcR4 with:

dashboard route migrated = 1
Host Seller routes 2 -> 1
Host/Seller files 4 -> 2
SellerPanelComposer ABSENT
SellerPanelModels ABSENT
remaining Host route = dev-contexts only
fullSellerFolderCertification = NOT_YET
sellerR5State = NOT_STARTED

Reconcile CURRENT/AUTHORITATIVE sections:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Do not leave placeholder commit markers.
Do not create a separate recovery repair if implementation passes.

EVIDENCE
docs/evidence/TB-TMAR-HOST-SELLER-AMC-001-R4/

migration.md
route-ownership.md
validation.md
certification.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SELLER-AMC-001-R4.task.md

SUCCESS
PASS only if:

dashboard Order-owned
Host dashboard route ZERO
Host Seller routes = 1
Host/Seller files = 2
Composer removed
Models removed if zero-consumer
Host Order.Application = ZERO
Host Party.Application = ZERO
behavior parity preserved
Recovery synchronized
no placeholders
automaticNextImplementationTask = NONE
frontend unchanged

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Commit and push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R4
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
R3-State:
Dashboard-Route-Ownership-State:
Host-Seller-Route-Count-Before:
Host-Seller-Route-Count-After:
Host-Seller-File-Count-Before:
Host-Seller-File-Count-After:
SellerPanelComposer-State:
SellerPanelModels-State:
Order-Endpoints-ISender-State:
Order-Endpoint-DbContext-State:
Order-Dashboard-CQRS-State:
Party-Boundary-State:
Host-Order-Application-Leakage-State:
Host-Party-Application-Leakage-State:
Behavior-Parity-State:
Host-Security-Seller-State:
Schema-Change-State:
Frontend-State:
Sink-Folder-Regression-State:
Full-Seller-Folder-Certification-State:
Seller-R5-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Recovery-Placeholder-State:
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
Do not start Seller-R5.
Wait for Architect/user review.

END_TOOBA_TASK