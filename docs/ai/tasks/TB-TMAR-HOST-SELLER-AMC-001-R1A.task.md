PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R1A
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Seller AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SELLER_R1_BOUNDARY_REPAIR
Title: Remove foreign Application leakage from Host/Security/Seller and reconcile Recovery/SoT

CURRENT BASELINE

Parent R1 commit: 3c13e4bcbf2b0a32ef4a701b48e8782a41d6b51f
Host/Security/Seller = 10 files, namespace Tooba.Host.Security.Seller
AccessControl Application/Domain leakage removed
Host/Seller still contains 5 business files for later R2–R6
Full Seller certification is NOT complete

ARCHITECT BLOCKER
R1 is not yet accepted because Host/Security/Seller still references foreign Application layers:

HostSellerOrderViewAccessReader.cs -> Tooba.Order.Application.Seller.Ports
HostOrderSellerAuthorizer.cs -> Tooba.Order.Application.Seller.SellerOrderErrors
HostSupportSellerAuthorizer.cs -> Tooba.Support.Application.Errors.SupportErrorCodes

This violates the Contracts-only / no foreign Application-Domain-Infrastructure-Persistence rule for Host security boundaries.

MANDATORY SKILLS

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

SCOPE
Repair ONLY this R1 boundary leak.
Do NOT start Seller-R2.
Do NOT move seller routes.
Do NOT change Seller settings/dashboard/dev bootstrap business behavior.
Do NOT modify frontend.
Do NOT broadly refactor Order or Support.

ARCHITECTURE DECISION

A) ORDER VIEW ACCESS
Host must not implement the Order Application port ISellerOrderViewAccessReader.
Move its implementation into Order-owned Infrastructure.
The Order Infrastructure implementation may consume the existing neutral Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader and implement the Order Application port intra-module.
Preserve exactly:

order.view
DeniedByCeiling
GlobalWithinOwner
Category scope projection
denied snapshot semantics

Final:

HostSellerOrderViewAccessReader.cs ABSENT
Order-owned implementation PRESENT
DI registration points the Order Application port to Order Infrastructure
Host has no Tooba.Order.Application.Seller.Ports reference

B) HOST SELLER SECURITY ERROR CODES
HostOrderSellerAuthorizer must not reference SellerOrderErrors.
HostSupportSellerAuthorizer must not reference SupportErrorCodes.

Create a small Host-owned canonical surface if needed:
Host/Security/Seller/SellerSecurityErrorCodes.cs
namespace Tooba.Host.Security.Seller

Include only shared Host security codes actually needed, e.g.:

seller.actor.missing
seller.identity.missing
seller.authorization.denied
seller.authorization.unavailable

Do not duplicate module-specific business error codes.

C) FINAL HOST SECURITY BOUNDARY
Every production file under Host/Tooba.Host/Security/Seller must have ZERO references to foreign:

.Application
.Domain
.Infrastructure
.Persistence
DbContext
DbSet

Allowed:

Tooba.BuildingBlocks
Tooba.BuildingBlocks.Security
module Endpoints ports where Host is a transport/security adapter
ASP.NET/Host primitives

Do NOT classify foreign Application ports as Contracts.

PATH/NAMESPACE
Preserve exact Host/Security/Seller/* -> Tooba.Host.Security.Seller.
Any new Order Infrastructure implementation must also have exact path-derived namespace.

BEHAVIOR LOCK
No change to:

routes/verbs
X-Tooba-Seller-Party-Id
X-Tooba-Dev-Actor-User-Id
status codes
DTOs
permission IDs
seller authorization behavior
fail-closed behavior
schema/migrations
frontend

GUARDS
Update/add focused guards proving:

Host/Security/Seller path↔namespace exact.
ZERO foreign Application/Domain/Infrastructure/Persistence across the entire boundary.
HostSellerOrderViewAccessReader.cs absent.
Order-owned implementation exists and implements the Order port.
HostOrderSellerAuthorizer has no SellerOrderErrors/Application reference.
HostSupportSellerAuthorizer has no SupportErrorCodes/Application reference.
stable seller security codes unchanged.
IPlatformEffectiveAccessReader remains the neutral seam.
service locator ZERO.
foreign persistence ZERO.
route/header/status/DTO unchanged.
sink-folder regression NONE.

Do not weaken R1 guards. Remove any explicit allowance for foreign Application references and replace it with ZERO.

FOCUSED VALIDATION ONLY
Build:

Tooba.Host
Tooba.Order.Infrastructure
Tooba.Host.Tests
relevant Order test project only if touched

Focused tests:

HostSellerAmcR1GuardTests
R1A guard
affected Order seller architecture/security tests
HostOrderReverseAuditGuardTests if registration/path touched
Support architecture guard only if needed
TmarDurableGuardTests

No solution-wide tests.
No broad module suites.

RECOVERY / SOT
Recovery is part of Definition of Done.

On PASS:

preserve Development closure as historical accepted lineage
record Seller R1A as the current Seller work checkpoint
full Host/Seller remains OPEN
Seller-R2 is NOT auto-started
no other Host folder starts

Update docs/architecture/tmar-current-state.json with block:
hostSellerAmcR1A

Required fields:

parentTask = TB-TMAR-HOST-SELLER-AMC-001-R1
parentImplementationCommit = 3c13e4bcbf2b0a32ef4a701b48e8782a41d6b51f
foreignApplicationBoundaryState = ZERO
foreignDomainBoundaryState = ZERO
foreignInfrastructureBoundaryState = ZERO
foreignPersistenceBoundaryState = ZERO
orderViewAccessImplementationState = ORDER_INFRASTRUCTURE_OWNED
sellerSecurityErrorCodeState = HOST_BOUNDARY_OWNED_STABLE_CODES
routeChangeState = NONE
schemaChangeState = NONE
frontendState = UNCHANGED
sinkFolderRegressionState = NONE
fullSellerFolderCertification = NOT_YET
automaticNextImplementationTask = NONE
nextTaskState = USER_DECISION_REQUIRED
workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R1A

Reconcile CURRENT/AUTHORITATIVE sections where required:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

They must state:

Seller R1A accepted only for security-boundary repair
full Host/Seller remains OPEN
no automatic next implementation task
workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R1A

Preserve historical lineage.
Never mislabel a docs/evidence commit as the implementation commit.

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-SELLER-AMC-001-R1A/

analyze.md
migration.md
boundary-map.md
validation.md
certification.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SELLER-AMC-001-R1A.task.md

SUCCESS CRITERIA
PASS only if:

Host/Security/Seller foreign Application = ZERO
foreign Domain = ZERO
foreign Infrastructure = ZERO
foreign Persistence = ZERO
Host no longer implements the Order Application port
Order-owned implementation preserves behavior
Host Support authorizer has no Support Application dependency
Host Order authorizer has no Order Application dependency
stable codes preserved
no route/header/status/DTO/schema/frontend change
no sink-folder regression
focused validation PASS
Recovery/SoT reconciled
automaticNextImplementationTask = NONE
user work preserved

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Preserve user work.
Commit and push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R1A
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Parent-R1-State:
Host-Seller-Security-Boundary-State:
Foreign-Application-State:
Foreign-Domain-State:
Foreign-Infrastructure-State:
Foreign-Persistence-State:
Order-View-Access-Implementation-State:
Order-Application-Port-In-Host-State:
Order-Authorizer-Application-Reference-State:
Support-Authorizer-Application-Reference-State:
Seller-Security-Error-Code-State:
Neutral-Effective-Access-Seam-State:
Service-Locator-State:
Route-Change-State:
Header-Change-State:
Status-Code-Change-State:
Dto-Change-State:
Schema-Change-State:
Frontend-State:
Sink-Folder-Regression-State:
Full-Seller-Folder-Certification-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
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
Do not start Seller-R2.
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK