PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-CANON-010-FINAL-CERT
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-009
Parent-Commit: 2976d03a772a877166a6ed06229b066c30f84dcf
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Canonicalization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_CANONICALIZATION
Title: Final certify Host/Admin as canonical Host platform boundary

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-ADMIN-CANON-009 is ARCHITECT-ACCEPTED at:
2976d03a772a877166a6ed06229b066c30f84dcf

Verified:

Support foreign Application reference = ZERO
Wallet foreign Application reference = ZERO
stable 403/503 codes preserved
fail-closed behavior preserved
Host/Admin recursive file count = 15
CANON-008 structure preserved

PURPOSE

This is the FINAL CERTIFICATION wave for src/backend/Host/Tooba.Host/Admin.

This task is AUDIT + CERTIFICATION ONLY.

Do NOT perform production-code repairs in this task.
If any certification criterion fails:

return INCOMPLETE
report the exact file/symbol/criterion
STOP
do not repair it here

SCOPE

Audit the entire current recursive Host/Admin tree only:

src/backend/Host/Tooba.Host/Admin/**/*.cs

Expected recursive production file count: exactly 15.

CERTIFICATION STANDARD

Host/Admin may contain ONLY Host platform concerns:

panel access / platform authorization adapters
thin module endpoint-authorizer adapters
cross-module admin panel composition via lawful contracts/seams
generic admin grid HTTP boundary
Development-only admin bootstrap

Host/Admin MUST NOT contain:

module business ownership
module business persistence
module business command/write logic
foreign DbContext/IQueryable persistence reads
direct foreign *.Application
direct foreign *.Infrastructure
direct foreign *.Domain
cross-schema joins/FKs
SaveChanges/transactions
foreign aggregate/entity ownership
service locator via HttpContext.RequestServices
message-text parsing / ex.Message classification
capability fail-open
compatibility shims
old flat namespace/path residues
business endpoint files already evacuated to modules

ALLOWED CROSS-BOUNDARIES

Allowed:

Tooba.*.Contracts
module *.Endpoints authorizer interfaces/error-code seams where Host is the transport adapter
neutral BuildingBlocks security/platform abstractions
Host-owned helpers/composition/grid types

Any other module-layer dependency requires explicit evidence that it is a canonical seam; otherwise certification FAILS.

MANDATORY AUDITS

File/namespace structure

root flat .cs = ZERO
recursive .cs = 15
exact folders:
Access
Access/Authorizers
Panel
Grid
Development
path ↔ namespace EXACT

Foreign dependency audit across ALL 15 files

ZERO using Tooba.<Module>.Application
ZERO using Tooba.<Module>.Infrastructure
ZERO using Tooba.<Module>.Domain
ZERO foreign DbContext/IQueryable/EF usage
list every module boundary actually consumed and classify it as Contracts / Endpoints seam / neutral seam

Host ownership audit

no business aggregate/domain ownership
no module SaveChanges/transaction
no module-specific business endpoint file
no business mutation implementation in Host/Admin

Authorization audit

panel gate centralized on IAdminPanelAccess where applicable
simple adapters thin
capability adapters fail closed on Unavailable
ZERO RequestServices service locator
ZERO duplicate tenant/platform authorization policy where CANON-002 centralized it

Order audit

HostOrderAdminAuthorizer = neutral authorization + IAdminPanelAccess
HostOrderAdminEffectiveAccessReader = neutral IPlatformEffectiveAccessReader
Order effective-access contract authority remains Order.Contracts
ZERO AccessControl Application/Domain on Host reader

Panel composition audit

AdminPanelComposer business reads = Contracts-only
seller grid = Contracts-only toward modules
no direct DbContext

Development audit

AdminDevActorBootstrap = Identity.Contracts-only
ZERO Identity.Infrastructure
ZERO expected-flow catch (InvalidOperationException)
Development-only semantics preserved

Semantic error hygiene

no ex.Message classification
no localized text used as machine classification
existing stable auth/error codes remain unchanged

Historical seam preservation

CANON-001 through CANON-009 critical guards/surfaces still present

NO REPAIR RULE

If any production file violates the certification standard:
Status = INCOMPLETE.
Do not edit production code to make the certification pass.

Permitted changes in this task:

certification guard/test only
evidence
SoT
task artifact

No production behavior file edits.

DURABLE FINAL GUARD

Add:
HostAdminCanonicalCertificationGuardTests

It must enforce at minimum:

exact 15-file structure
exact namespaces
recursive foreign Application/Infrastructure/Domain = ZERO
DbContext/IQueryable/EntityFramework = ZERO in Host/Admin
RequestServices/GetRequiredService = ZERO in Host/Admin
SaveChanges/BeginTransaction/TransactionScope = ZERO in Host/Admin
ex.Message = ZERO in Host/Admin
no fail-open branch pattern on Unavailable
key seams from CANON-001..009
forbidden evacuated endpoint filenames absent

Do not overbuild a general architecture framework.

VALIDATION / ANTI-LOOP

Certification validation only:

build Tooba.Host
build Tooba.Host.Tests
run ONLY:
HostAdminCanonicalCertificationGuardTests
HostAdminCanon001GuardTests through HostAdminCanon009GuardTests

Do NOT run the broad Host test suite.
Do NOT run solution-wide tests.

No production repair is allowed.
If a certification guard fails because of production code:

do not repair
Status = INCOMPLETE
STOP

If the new guard itself has a trivial compile/test defect:

maximum ONE repair attempt to the GUARD ONLY
rerun only the failed command once
second failure => INCOMPLETE + STOP

MAX_GUARD_REPAIR_ITERATIONS = 1
MAX_VALIDATION_COMMAND_RUNS = 5
Do not loop.

EVIDENCE

Create only:
docs/evidence/TB-TMAR-HOST-ADMIN-CANON-010-FINAL-CERT/

audit.md
boundary-map.md
validation.md
certification.md

boundary-map.md must enumerate all 15 production files with:

responsibility
dependencies
classification: PLATFORM_KEEP
certification state

Persist task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-CANON-010-FINAL-CERT.task.md

RECOVERY SOT

On PASS append/update:
hostAdminCanonicalCertification

Required fields:

parentCommit
state=CANONICAL_PLATFORM_BOUNDARY_CERTIFIED
recursiveFileCount=15
flatRootFiles=0
foreignApplication=ZERO
foreignInfrastructure=ZERO
foreignDomain=ZERO
foreignDbContext=ZERO
serviceLocator=ZERO
businessOwnership=ZERO
failOpen=ZERO
pathNamespace=EXACT
canon001Through009=PRESERVED
certificationGuard=PASS
workflowStop=HOST_ADMIN_CERTIFIED_USER_REVIEW

Do not mark certified on INCOMPLETE.

SUCCESS CRITERIA

PASS only if the full recursive Host/Admin tree satisfies every certification criterion with no production-code repair required in this task.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-CANON-010-FINAL-CERT
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-009
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Canon009-Preservation-State:
Host-Admin-Recursive-File-Count:
Host-Admin-Flat-Root-State:
Path-Namespace-State:
Foreign-Application-State:
Foreign-Infrastructure-State:
Foreign-Domain-State:
Foreign-DbContext-State:
ServiceLocator-State:
BusinessOwnership-State:
BusinessWrite-State:
Authorization-FailOpen-State:
MessageParsing-State:
AdminPanelComposer-Boundary-State:
SellerGrid-Boundary-State:
OrderAuthorizer-Boundary-State:
OrderEffectiveAccess-Boundary-State:
DevelopmentBootstrap-Boundary-State:
Evacuated-Business-Endpoint-Residue-State:
Canon001-009-Seams-State:
Final-Certification-State:
Focused-Validation:
Evidence-Path:
SoT-State:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
Guard-Repair-Iterations:
Validation-Command-Runs:
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP COMPLETELY.

Do not begin another Host folder.
Do not begin module audit.
Wait for Architect review.

END_TOOBA_TASK