PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001
Parent-Task: TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: ACCESSCONTROL_FINAL_CERTIFICATION
Title: Final AccessControl ARCH-COMPLETE-002 certification + Recovery SoT closure
Backend-Only: YES
Docs-And-Guards-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001

ACCEPTED-PARENT-COMMIT:
34476274bcb116e28de7ae96977e819d4f6d09eb

RECOVERY-SOT-PRECHECK-COMMIT:
ad63ed23f27cd342d00e1c058458aadc2d08bfe6

TIMEBOX:
Target <= 8 minutes.
Hard maximum 12 minutes.
If safe completion exceeds the hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.
No production code changes.

ONLY THREE LOGICAL CHANGES:

Certify AccessControl in the ARCH-COMPLETE-002 module manifest.
Close canonical Recovery SoT / Master Recovery for AccessControl and point recovery to the next Host folder.
Produce certification evidence and run only focused existing AccessControl guards.

Do not do anything else.

CURRENT VERIFIED ACCESSCONTROL STATE:

Host evacuation:

src/backend/Host/Tooba.Host/AccessControl = ABSENT / ZERO
namespace Tooba.Host.AccessControl = ZERO
MapAccessControlEndpoints = ZERO
Program keeps MapAccessControlModuleEndpoints exactly once

HTTP ownership:

module-owned Admin/Seller AccessControl endpoints
real MediatR 12.5 ISender dispatch
Host duplicate route ownership = ZERO

Structure:

Application root capability .cs files = ZERO
Endpoints root = AccessControlEndpointModule.cs only
Infrastructure root = AccessControlModule.cs only
path <-> namespace = EXACT for repaired files
no namespace-alias workaround

Validation:

endpoint-reachable requests = 19
VALIDATOR_REQUIRED = 6
validators present = 6
NO_VALIDATOR_REQUIRED = 13
validator gap = ZERO
automatic discovery via existing AddToobaCqrsFoundation/AddValidatorsFromAssembly
business validation remains Application/Domain owned

Boundaries:

AccessControl.Application -> Host = ZERO
AccessControl.Application -> foreign Application/Domain = ZERO
allowed Contracts: Catalog.Contracts, Identity.Contracts, OperatorProfile.Contracts
AccessControl.Infrastructure -> Catalog.Application/Domain = ZERO
AccessControl.Infrastructure -> Catalog.Contracts = ALLOWED
AccessControl.Endpoints -> Host = ZERO
AccessControl.Endpoints -> direct IAccessControlDirectory = ZERO

CERTIFICATION CHANGE 1 — MODULE MANIFEST:

Update:
docs/architecture/tmar-module-structure-manifests.json

Add AccessControl as:
module = AccessControl
structureCertified = true
lockVersion = ARCH-COMPLETE-002

Projects:

Tooba.AccessControl.Application

rootAllowlist = []
forbiddenRootFiles should include at minimum:
AccessControlContracts.cs
PermissionCatalog.cs

Tooba.AccessControl.Endpoints

rootAllowlist = [ "AccessControlEndpointModule.cs" ]
forbiddenRootFiles should include:
AccessControlAdminEndpoints.cs
AccessControlAdminSellerEndpoints.cs
AccessControlSellerEndpoints.cs

Tooba.AccessControl.Infrastructure

rootAllowlist = [ "AccessControlModule.cs" ]
forbiddenRootFiles should include at minimum:
AccessControlDirectory.cs
AccessControlInstrumentation.cs
AccessControlOutboxRegistration.cs

Do not invent unrelated forbidden entries.

CERTIFICATION CHANGE 2 — RECOVERY SOT CLOSURE:

Update:
docs/architecture/tmar-current-state.json

Required closure state:

AccessControl state = COMPLETE_REFERENCE_PATTERN
httpApplicability = HTTP_OWNING
endpointOwnership = MODULE_ENDPOINTS
cqrs = MEDIATR_12_5
structureCertifiedUnderArchComplete002 = true
validatorCoverage = COMPLETE_6_OF_6_REQUIRED_PRESENT_13_NO_VALIDATOR_REQUIRED
endpointReachableRequests = 19
hostResidue = ZERO
pathNamespace = EXACT
rootAllowlist = ENFORCED
contractsBoundary = CLEAN_CONTRACTS_ONLY
module map = MapAccessControlModuleEndpoints

Add "AccessControl" to structureLock.certifiedModules.

Do not remove/reorder historical certified modules unnecessarily.

Update currentHostEvacuation:

AccessControl closure = COMPLETE
inspect current Host tree cheaply
expected next Host folder by protocol is AddressBook if still present
do not start AddressBook work

Set nextTask only after confirming folder:
preferred:
TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001

If AddressBook is absent, record the actual next Host folder and matching INVENTORY task id.
Do not guess.

Update:
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

Append/update one concise AccessControl final closure section:

Host ZERO
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
19 / 6 / 6 / 13 validator coverage
Contracts-only boundaries
final certification task
next Host folder/task
frontend frozen
Checkout paused

Do not broadly rewrite historical content.

CERTIFICATION CHANGE 3 — EVIDENCE + FOCUSED GUARDS:

Create:
docs/evidence/TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001/accesscontrol-final-certification.md

Evidence must include:

Host ZERO proof
endpoint ownership proof
structure/root allowlists
path/namespace proof
exact 19 request inventory summary
6 required / 6 present / 13 no-validator-required
validator discovery
boundary audit
manifest entry proof
certifiedModules proof
COMPLETE_REFERENCE_PATTERN SoT proof
next Host folder/task
known repository-wide baseline debt classification

EXISTING FOCUSED GUARDS ONLY:

Run only:

AccessControlValidatorTests
AccessControl_module_boundary_static_checks

If an already-existing exact AccessControl manifest/SoT guard exists, run it too.

Do not create a broad guard suite.

FOCUSED BUILDS:
No build required for docs-only changes.

NO solution build.
NO broad integration suite.
NO broad architecture suite.
NO retry cascade.

REPOSITORY-WIDE BASELINE DEBT:

Known pre-existing debt:

tmar-source-size-baseline.json stale
tmar-infra-to-foreign-application.json stale
source-size-inventory.json stale

These are repository-wide and NOT AccessControl-specific certification blockers when the dedicated AccessControl structure/boundary/validator guards pass.

DO NOT MODIFY those files in this task.
DO NOT broaden into repository-wide cleanup.
Record debt only.

CERTIFICATION VERDICT RULE:

PASS only if all AccessControl-specific certification gates pass.

On PASS:
AccessControl =
COMPLETE_REFERENCE_PATTERN
+
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

If any AccessControl-specific gate fails:
Status = INCOMPLETE
record exact blocker
STOP

Do not partially certify.

PROTECTED STATE:
Frontend = FROZEN
Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

No production code changes.
No schema/migrations.
No route changes.
No new validators.
No Host AccessControl recreation.
No AddressBook implementation work.

CANONICAL TASK ARTIFACT:

Commit this exact task at:
docs/ai/tasks/TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001.task.md

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001
Parent-Task: TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Host-Zero-State:
Endpoint-Ownership-State:
CQRS-State:
Application-Root-State:
Endpoints-Root-State:
Infrastructure-Root-State:
Path-Namespace-State:
Validator-Inventory-State:
Validator-Coverage-State:
Validation-Discovery-State:
Application-Boundary-State:
Infrastructure-Boundary-State:
Endpoint-Boundary-State:
Manifest-State:
Structure-Certified-State:
Complete-Reference-Pattern-State:
Certified-Modules-State:
Current-State-Json-State:
Master-Recovery-State:
Next-Host-Folder:
Next-Task-State:
Focused-Tests:
Repository-Wide-Baseline-Debt-State:
Production-Code-Changes:
Test-Code-Changes:
Canonical-Task-State:
Evidence:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start AddressBook.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK