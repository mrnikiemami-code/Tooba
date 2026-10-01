PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT
Parent-Task: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_ACCESS_CERTIFY_AND_ADMIN_CLOSURE
Title: Independently certify Admin/Access and, only if all protected sub-surfaces pass, close the whole Host/Admin tree
Estimated-Time-Minutes: 17
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

W2:
ACCEPTED FOR CERTIFICATION ENTRY

Architect independently verified:

Order/Support/Wallet Host authorizers use SemanticException + stable codes
hard-coded runtime titles on Access authorizer family = ZERO
order.operation.denied descriptor added exactly once
Order EN/FA localization present
Support EN/FA resource set present and registered
Wallet EN/FA resource set present and registered
admin.authorization.denied remains Foundation-owned
HostPromotionAdminAuthorizer is physically ABSENT
Access production files = 12
Host/Admin recursive files = 17
Grid remains ABSENT/HOST_ZERO
W1 core Access state preserved
W2 implementation commit:
7a0d79b407c618d503acd1ef9866c519b4e5f070

IMPORTANT STALE-METADATA FACT

HostAdminCanonicalCertificationGuardTests currently has stale comments that still say:

"Host/Admin is CERTIFIED"
mentions "generic admin grid HTTP boundary"
remarks mention old file counts
while actual assertions now operate on 17 files and Grid is absent.

Certification MUST reconcile these comments/metadata if the certification passes.
Do not leave contradictory certification wording behind.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY means independently re-verify.
Do NOT trust W1/W2 result claims.

No production redesign.
No migration except deterministic certification metadata/guard/comment correction.
If material production repair is needed:
STOP INCOMPLETE and name the exact repair task.

PRIMARY CERTIFICATION SCOPE

A. Host/Admin/Access

Exact expected production files = 12.

Access root:

AdminPanelAccess.cs
HostAdminPanelAccess.cs

Access/Authorizers:

HostLocalizationAdminAuthorizer.cs
HostOperatorProfileAdminAuthorizer.cs
HostOrderAdminAuthorizer.cs
HostOrderAdminEffectiveAccessReader.cs
HostPaymentAdminAuthorizer.cs
HostReturnAdminAuthorizer.cs
HostSettlementAdminAuthorizer.cs
HostSupportAdminAuthorizer.cs
HostUserPreferenceAdminAuthorizer.cs
HostWalletAdminAuthorizer.cs

HostPromotionAdminAuthorizer:
ABSENT

B. WHOLE HOST/ADMIN CLOSURE SYNTHESIS

Only after Access independently passes, re-verify protected accepted sub-surfaces:

Panel:

HOST_ADMIN_PANEL_AMC_CERTIFIED
3 files
dashboard only
thin Host cross-module composition

Development:

2 files
dev-context route
canonical Result/ApiResponseFactory
Foundation-localized admin.dev.unavailable

Grid:

ABSENT / HOST_ZERO

Party sellers:

GET/POST Party-owned

Whole Host/Admin exact production file count:
17

Only if Access + Panel + Development + Grid closure all independently pass may you record:
HOST_ADMIN_FULLY_CERTIFIED

If any protected sub-surface fails current truth:
do NOT claim whole Host/Admin certification.
Report exact blocker.

MANDATORY ACCESS CERTIFICATION AUDITS

PHYSICAL TREE / PATH-NAMESPACE

Re-enumerate Access tree from disk.

Verify:

exact 12 files
exact namespaces:
Tooba.Host.Admin.Access
Tooba.Host.Admin.Access.Authorizers
no root flat misplaced file
no duplicate old copies
no alias
no shim
no TypeForwardedTo
no HostPromotionAdminAuthorizer residue
CONSUMER / DI AUTHORITY

Verify:

HostAdminPanelAccess is sole Host implementation of IAdminPanelAccess
AdminPanelAccess is production-consumed only as HostAdminPanelAccess implementation helper
all 10 active Host authorizer adapters have real DI/production consumers
dead authorizer count = ZERO
no unregistered orphan adapter remains
CORE ADMIN ACCESS FAILURE SEMANTICS

Verify exact behavior:

admin.actor.missing -> 401
admin.tenant.missing -> 503
admin.authorization.unavailable -> 503
admin.authorization.denied -> 403

Verify:

stable codes
SemanticException/SemanticError path
catalog descriptor exactly once
EN + FA resource localization
zero hard-coded runtime FA/EN throw title
no PlatformHttpException in core access expected-failure path
no exception.Message / ex.Message classification
DEV ACTOR HEADER SECURITY

Verify:
X-Tooba-Dev-Actor-User-Id

authenticated session wins
header only honored in Development
valid non-empty Guid required
outside Development fail-closed
no raw header logging
MARKETPLACE DEVELOPMENT PATH

Verify:

HostAdminPanelAccess stays Host-owned
ControlPlaneRegistry edition branch unchanged
synthetic MarketplacePlatformTenantId unchanged
only Development + Marketplace may use synthetic path
missing tenant outside accepted branch -> canonical 503
Allow/Unavailable/Deny exact
MODULE AUTHORIZER FAMILY

Per-file verify:

Pass-through:

Localization
OperatorProfile
Payment
Return
Settlement
UserPreference

Capability:

Order
Support
Wallet

Effective access:

HostOrderAdminEffectiveAccessReader

For each:

actual interface
production registration
thin Host adapter only
no module business ownership
no module persistence
no foreign Application/Infrastructure/Domain
AUTHORIZER FAILURE SEMANTICS

Order:

order.authorization.unavailable -> 503 / Order owner / EN+FA
order.operation.denied -> 403 / Order owner / EN+FA

Support:

support.authorization.unavailable -> 503 / Support owner / EN+FA
denial -> admin.authorization.denied / Foundation 403 / EN+FA

Wallet:

wallet.authorization.unavailable -> 503 / Wallet owner / EN+FA
denial -> admin.authorization.denied / Foundation 403 / EN+FA

Verify descriptor uniqueness in composed catalog, not merely per contributor.

Do not accept fallback-only localization as sufficient where resources are required.

LOCALIZATION RESOURCE OWNERSHIP

Verify:

FoundationErrorResourceSet:

validation.*
platform.*
admin.*

OrderErrorResourceSet:

order.* includes both Access authorizer keys

SupportErrorResourceSet:

support.*
registered once by SupportEndpointModule

WalletErrorResourceSet:

wallet.*
registered once by WalletEndpointModule

Verify no resource-set ownership collision causes the wrong set to swallow keys with missing resource values.

Verify EN/FA resolve for every Access-path user-facing code.

HARDCODED RUNTIME TEXT

Scan all 12 Access production files.

Runtime user-facing FA/EN prose = ZERO.

Distinguish:

XML comments/documentation: allowed
machine identifiers: allowed
permission IDs/codes: allowed
runtime exception/presentation text: forbidden
CONTRACT / MICROSERVICE BOUNDARY

Access tree must have ZERO:

foreign Application
foreign Infrastructure
foreign Domain
foreign DbContext
cross-module persistence
cross-module join
module -> Host dependency
RequestServices/service locator in Access

Allowed:

BuildingBlocks neutral seams
module Endpoints auth seam interfaces/codes
module Contracts where already accepted
BUSINESS AUTHORITY

Verify Host Access owns only:

session/tenant/platform access composition
edge authorization adaptation
neutral capability checks through IAuthorizationService
effective-access adaptation

Must own ZERO:

business command logic
domain policy
entity persistence
business writes
module-specific state mutation
SENSITIVE LOGGING / OBSERVABILITY

Verify:

tokens ZERO
cookies ZERO
auth header ZERO
raw DevActorHeader ZERO
password ZERO
custom ActivitySource ZERO
custom Meter ZERO
manual traceparent ZERO
parallel correlation ZERO
COHESION

Verify responsibilities remain shallow and clear.

AdminPanelAccess:
SingleStore access helper + actor resolve

HostAdminPanelAccess:
IAdminPanelAccess + Marketplace Host branch

Authorizers:
one adapter responsibility each

EffectiveAccessReader:
one mapping seam

No god file.

WHOLE HOST/ADMIN FINAL TREE

If Access passes, re-enumerate whole:

Host/Admin/

Expected top-level dirs:

Access
Development
Panel

Grid:
ABSENT

Expected recursive production .cs count:
17

Expected:

Access 12
Development 2
Panel 3

No flat root .cs.

PROTECTED PANEL / DEVELOPMENT / PARTY RECHECK

Panel:

3 exact files
dashboard unique Host owner
ApiResponseFactory
Result
IAdminPanelAccess
Contracts-only composer
no seller/dev-context residue

Development:

2 exact files
dev-context unique owner
admin.dev.unavailable 404
ApiResponseFactory
localized Foundation resource
no hard-coded runtime title

Party:

sellers GET/POST unique Party owners
no Host duplicate

Grid:

Admin/Grid ABSENT
STALE CERTIFICATION METADATA CLEANUP

Certification may and should correct stale comments/test metadata in:
HostAdminCanonicalCertificationGuardTests
and directly related certification guard comments.

Required final wording must reflect current truth:

Grid is ABSENT/HOST_ZERO
Host/Admin exact files = 17
Access certified by this task
Panel/Development previously certified and reverified
whole Host/Admin certification only if current certification passes

Do NOT modify production behavior.

Test method names/comments containing stale "18" while asserting 17 should be corrected if present.

Do not delete useful guards.

DURABLE ACCESS CERTIFICATION GUARD

Create or strengthen a focused durable guard:
HostAdminAccessAmcCertGuardTests

It must prove at minimum:

Access exact 12
HostPromotion absent
core code-based exceptions
code/status descriptor matrix
EN/FA localization ownership
active authorizer exact set
no forbidden dependencies
hard-coded runtime title zero
DevActorHeader safety invariants
whole Host/Admin exact 17 / Grid absent
protected Panel/Development/Party facts

Do not rely only on old CANON guards.

CERTIFICATION LABELS

On PASS, required:

Access:
HOST_ADMIN_ACCESS_AMC_CERTIFIED

Core access:
PLATFORM_ACCESS_SEAM_CERTIFIED

Authorizer family:
THIN_HOST_AUTH_ADAPTERS_CERTIFIED

Grid:
HOST_ZERO_CERTIFIED

Panel:
PANEL_KEEP_CERTIFIED_PRESERVED

Development:
ADMIN_DEVELOPMENT_DEV_CONTEXT_CERTIFIED_PRESERVED

Whole Host/Admin:
HOST_ADMIN_FULLY_CERTIFIED

The last label is allowed ONLY if every whole-tree recheck in this task passes.

FOCUSED VALIDATION

Build:

BuildingBlocks
Order.Endpoints
Support.Endpoints
Wallet.Endpoints
Host

Focused tests:

new HostAdminAccessAmcCertGuardTests
AdminPanelAuthorizationTests
HostAdmin Canon relevant guards
HostAdminPanelAmcCertGuardTests
Order presentation/localization focused tests
Support presentation/localization focused tests
Wallet presentation/localization focused tests
composed error catalog uniqueness focused test
TmarDurableGuard for SoT

No solution-wide tests.
No open-ended repair loop.

One deterministic docs/guard metadata correction + one rerun maximum.

If production repair is needed:
STOP INCOMPLETE.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT/

Required:

certification-summary.md
physical-tree.md
consumer-di.md
authorization-semantics.md
error-localization.md
boundary-microservice.md
hardcoded-runtime-text.md
security-observability.md
protected-surfaces.md
whole-admin-closure.md
validation.md
recovery.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT.task.md

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT

latest implementation lineage remains W2:
7a0d79b407c618d503acd1ef9866c519b4e5f070
unless certification unexpectedly changes production code (should not)

accessCertificationState = HOST_ADMIN_ACCESS_AMC_CERTIFIED

coreAccessState = PLATFORM_ACCESS_SEAM_CERTIFIED

authorizerFamilyState = THIN_HOST_AUTH_ADAPTERS_CERTIFIED

deadHostAuthorizerCount = 0

accessProductionFileCount = 12

hostAdminRecursiveFileCount = 17

adminGridState = HOST_ZERO_CERTIFIED

panelState = PANEL_KEEP_CERTIFIED_PRESERVED

developmentState = ADMIN_DEVELOPMENT_DEV_CONTEXT_CERTIFIED_PRESERVED

partySellersState = CERTIFIED_PRESERVED

wholeHostAdminState = HOST_ADMIN_FULLY_CERTIFIED

currentHostCheckpoint = Admin/Access

automaticNextImplementationTask = NONE

workflowStop = USER_REVIEW_HOST_ADMIN_ACCESS_AMC_001_W3_CERT

nextHostFolderStarted = false

staleCurrentPointerState = ZERO

If certification commit is docs/tests/guards only:
record it separately as certification/docs SoT stamp.
Do NOT overwrite latest implementation SHA.

GIT

Work from latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Commit/push main only on PASS.

SUCCESS CRITERIA

PASS only if:

Access exact 12
active authorizer exact set proven
dead authorizer count ZERO
HostPromotion absent
core access code/status/localization exact
authorizer code/status/localization exact
composed descriptor uniqueness PASS
EN/FA localization resolution PASS for all Access-path codes
hard-coded runtime user-facing text ZERO across Access
exception-message classification ZERO
foreign App/Infra/Domain ZERO
foreign DbContext/persistence ZERO
module business authority ZERO
DevActorHeader security preserved
Marketplace branch preserved
sensitive logging ZERO
custom telemetry ZERO
cohesion PASS
Panel recheck PASS
Development recheck PASS
Party sellers recheck PASS
Grid absent
whole Host/Admin exact 17
stale certification metadata corrected
focused builds/tests PASS
no production repair required
Access label exactly HOST_ADMIN_ACCESS_AMC_CERTIFIED
whole Host/Admin label exactly HOST_ADMIN_FULLY_CERTIFIED
automaticNextImplementationTask NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT
Parent-Task: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Access-Certification-State:
Core-Access-Certification-State:
Authorizer-Family-Certification-State:
Whole-Host-Admin-Certification-State:
Access-Production-File-Count:
Host-Admin-Recursive-File-Count:
Host-Admin-Exact-Tree-State:
Admin-Grid-State:
Panel-State:
Admin-Development-State:
Party-Sellers-State:
Active-Authorizer-Adapter-Count:
Dead-Host-Authorizer-Count:
Promotion-Host-Authorizer-State:
Consumer-DI-State:
Core-Authorization-Semantics-State:
Authorizer-Authorization-Semantics-State:
Http-Status-Parity-State:
Descriptor-Uniqueness-State:
Localization-Resolution-State:
Foundation-Admin-Localization-State:
Order-Authorization-Localization-State:
Support-Authorization-Localization-State:
Wallet-Authorization-Localization-State:
Hardcoded-Runtime-User-Facing-Text-State:
Exception-Message-Classification-State:
DevActorHeader-Security-State:
Marketplace-Development-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Cross-Module-Persistence-State:
Module-Business-Authority-State:
Sensitive-Logging-State:
Observability-State:
Cohesion-State:
Stale-Certification-Metadata-State:
Durable-Access-Cert-Guard-State:
Production-Repair-Required-State:
Focused-Build-State:
Focused-Test-State:
Guard-State:
Recovery-State:
Last-Accepted-Implementation-Commit-State:
Certification-Commit-State:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.

Do not start another Host folder.
Do not reopen certified modules.

Wait for Architect/user review.

END_TOOBA_TASK
