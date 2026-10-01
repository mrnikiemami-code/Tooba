PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1
Parent-Task: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_ACCESS_CORE_ERROR_LOCALIZATION_HYGIENE
Title: Canonicalize core Admin panel access errors and localization without changing authorization semantics
Estimated-Time-Minutes: 17
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

Parent Analyze:
ACCEPTED

Architect independently verified:

Access tree = 13 production files
2 core Host platform access seam files
10 active thin Host authorizer adapters
1 dead authorizer residue (HostPromotionAdminAuthorizer) — DEFERRED TO W2
foreign Application/Infrastructure/Domain/DbContext = ZERO
core hard-coded admin.* titles exist
admin.actor.missing, admin.tenant.missing, admin.authorization.unavailable are unregistered
admin.authorization.denied is Foundation-owned
AdminPanelAccess production consumer is only HostAdminPanelAccess
HostAdminPanelAccess Marketplace Development branch is genuinely Host-owned
last accepted implementation SHA:
aee55d7f6ede1d3d817fc8afabe6ac4ab68fd80a

ADDITIONAL ARCHITECT FINDING — MUST FIX IN W1

FoundationErrorResourceSet currently owns only:

validation.*
platform.*

Therefore admin.* localization keys in Foundation catalog do NOT resolve through FoundationErrors.resx today and fall back to SafeTitleFallback.

This includes the previously added:

admin.dev.unavailable

W1 MUST correct Foundation resource ownership for canonical cross-cutting admin.* errors and add real EN/FA resource entries.

This is a narrow Foundation localization correction, not a redesign.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.

Do NOT start W2.
Do NOT delete HostPromotionAdminAuthorizer in W1.
Do NOT alter module authorizer capability logic.
Do NOT redesign Authentication / AccessControl / tenant resolution.
Do NOT change public routes.
Do NOT change schema/frontend.

IN-SCOPE PRODUCTION FILES

Core Host:

src/backend/Host/Tooba.Host/Admin/Access/AdminPanelAccess.cs
src/backend/Host/Tooba.Host/Admin/Access/HostAdminPanelAccess.cs

Foundation:

src/backend/BuildingBlocks/Tooba.BuildingBlocks/Presentation/Errors/FoundationErrorCodes.cs
src/backend/BuildingBlocks/Tooba.BuildingBlocks/Presentation/Errors/FoundationErrorCatalogContributor.cs
src/backend/BuildingBlocks/Tooba.BuildingBlocks/Localization/ResourceErrorMessageLocalizer.cs
src/backend/BuildingBlocks/Tooba.BuildingBlocks/Localization/Resources/FoundationErrors.resx
src/backend/BuildingBlocks/Tooba.BuildingBlocks/Localization/Resources/FoundationErrors.fa.resx

Focused tests/guards only as required.

ARCHITECTURE DECISION

Core Admin access failures are expected stable authorization/platform outcomes.

They MUST be represented by machine codes and canonical catalog/localizer presentation.

Do NOT use hard-coded prose as exception title.

Do NOT modify PlatformHttpException globally.

Preferred target:

use existing code-based SemanticError / SemanticException path for expected stable failures
OR
another already-existing canonical code-only exception path if repo truth proves it equivalent

Mandatory outcome:
NO Persian/English runtime prose passed from AdminPanelAccess or HostAdminPanelAccess into exception presentation.

Do not introduce a new exception abstraction.

REQUIRED W1 CHANGES

FOUNDATION ERROR CODES

Add stable constants to FoundationErrorCodes for:

AdminActorMissing = "admin.actor.missing"
AdminTenantMissing = "admin.tenant.missing"
AdminAuthorizationUnavailable = "admin.authorization.unavailable"

Keep existing:

AdminAuthorizationDenied = "admin.authorization.denied"

Do NOT rename or change code strings.

Do NOT create aliases in Host.

FOUNDATION ERROR DESCRIPTORS

Register each exactly once:

admin.actor.missing

HTTP 401
existing repo-compatible classification for authentication-required outcomes
(follow current Foundation convention; do not invent a new enum)

admin.tenant.missing

HTTP 503
Platform classification

admin.authorization.unavailable

HTTP 503
Platform classification

admin.authorization.denied

preserve HTTP 403 and existing classification

admin.dev.unavailable

preserve HTTP 404 / NotFound

Descriptor duplication = ZERO.

Use FoundationErrorCodes constants for the three new core codes and existing denied constant.

Do NOT move module-specific order/support/wallet codes into Foundation.

FOUNDATION RESOURCE OWNERSHIP

Update FoundationErrorResourceSet so canonical Foundation-owned admin.* keys can resolve from FoundationErrors resources.

Minimum safe target:
Foundation resources may own admin.* in addition to existing validation.* and platform.*.

Do NOT remove existing ownership.
Do NOT prevent module resource contributors from handling keys for which Foundation has no resource value.

Verify localizer iteration remains safe for unknown admin.* keys.

EN/FA RESOURCES

Add real localized resource entries for:

admin.actor.missing
admin.tenant.missing
admin.authorization.unavailable
admin.authorization.denied
admin.dev.unavailable

English resource:
FoundationErrors.resx

Persian resource:
FoundationErrors.fa.resx

Requirements:

clear user-facing titles
no machine code displayed as title when resource exists
no localization text hard-coded in production C# throw sites
exact keys match catalog LocalizationKey
no duplicate keys

This intentionally upgrades admin.dev.unavailable from catalog-fallback-only to real canonical localization.
Do NOT alter dev-context route/status/shape/code.

ADMINPANELACCESS HARD-CODED TEXT REMOVAL

Current semantics MUST remain exactly:

Actor resolution:

authenticated session UserId wins
Development-only X-Tooba-Dev-Actor-User-Id valid non-empty Guid fallback
otherwise admin.actor.missing / 401

Single-Store:

tenant missing -> admin.tenant.missing / 503
Allow -> actor
Unavailable -> admin.authorization.unavailable / 503
Deny -> admin.authorization.denied / 403

Replace hard-coded Persian titles with canonical code-based failure construction.

After W1 in AdminPanelAccess.cs:
hard-coded runtime user-facing FA/EN prose = ZERO.

Comments/XML Persian are not runtime presentation.

HOSTADMINPANELACCESS HARD-CODED TEXT REMOVAL

Preserve exact branch behavior:

If tenant.Current exists:
delegate to AdminPanelAccess

If tenant absent:

only Development + Marketplace may use synthetic MarketplacePlatformTenantId
otherwise admin.tenant.missing / 503

Marketplace decision:

Allow -> actor
Unavailable -> admin.authorization.unavailable / 503
Deny -> admin.authorization.denied / 403

No auth model changes.
No permission changes.
No MarketplacePlatformTenantId changes.
No ControlPlaneRegistry branch changes.

After W1:
hard-coded runtime user-facing FA/EN prose = ZERO.

ADMINPANELACCESS / HOSTADMINPANELACCESS STRUCTURE

Do NOT merge files in W1.

Reason:
Analyze established both responsibilities as coherent and active.
Structural consolidation is not required to close this quality blocker.

Keep:

AdminPanelAccess.cs
HostAdminPanelAccess.cs

AdminPanelAccess may remain internal static implementation helper.
HostAdminPanelAccess remains sole IAdminPanelAccess DI implementation.

No new files.

DEV ACTOR HEADER SECURITY

Preserve exactly:
X-Tooba-Dev-Actor-User-Id

Requirements:

only honored when environment.IsDevelopment()
authenticated session remains higher priority
ignored outside Development
Guid.Empty rejected
no logging of raw header value
MODULE AUTHORIZERS — W1 OUT OF SCOPE

Do NOT edit:

HostOrderAdminAuthorizer.cs
HostSupportAdminAuthorizer.cs
HostWalletAdminAuthorizer.cs
HostPromotionAdminAuthorizer.cs
pass-through authorizers
HostOrderAdminEffectiveAccessReader.cs

Their hard-coded title debt/dead residue is W2.

W1 hardcoded-text success criterion applies to:
CORE ACCESS FILES only.

PROTECTED STATE

Must remain unchanged:

Panel:
HOST_ADMIN_PANEL_AMC_CERTIFIED

Admin/Development:
CERTIFIED

dev-context route unchanged
status/code/shape unchanged
only resource localization quality may improve through shared Foundation resource resolution

Party sellers:
CERTIFIED

Admin/Grid:
HOST_ZERO / ABSENT

BOUNDARY / MICROSERVICE SAFETY

Core Access must remain:

BuildingBlocks/Host platform only
foreign .Application = ZERO
foreign .Infrastructure = ZERO
foreign .Domain = ZERO
DbContext = ZERO
persistence = ZERO

No module -> Host dependency introduced.

EXCEPTION MESSAGE RULE

ZERO:

ex.Message classification
exception.Message classification
message-text-to-code mapping

Expected failures selected by authorization state + stable code only.

OBSERVABILITY / SENSITIVE DATA

No new custom telemetry.

No logs of:

DevActorHeader raw value
authorization header
cookie
token
actor secrets
tenant secrets
TESTS / GUARDS

Add/update focused tests proving:

A. Foundation catalog:

admin.actor.missing exactly once / 401
admin.tenant.missing exactly once / 503
admin.authorization.unavailable exactly once / 503
admin.authorization.denied exactly once / 403
admin.dev.unavailable exactly once / 404

B. Localization:
For EN and FA:

all five admin.* keys resolve from canonical Foundation resources
returned title is not raw machine code
returned title is not fallback-only when resource exists

C. Core access:

no runtime Persian/English exception title literals
stable code branch parity
DevActorHeader Development-only behavior preserved
Marketplace branch preserved

D. Protected:

Panel CERT guard still PASS
Grid remains absent
Host/Admin recursive file count remains 18
no W2 authorizer changes
STALE GUARD METADATA

Do NOT perform broad metadata/comment cleanup in W1.

If a focused guard comment must be touched because assertions are directly changed, it may be corrected narrowly.

Full certification wording cleanup belongs to W3 CERT.

FOCUSED VALIDATION ONLY

Build:

BuildingBlocks
Host

Run focused:

BuildingBlocks localization/error tests affected
AdminPanelAuthorizationTests
directly affected HostAdmin Canon guards
HostAdminPanelAmcCertGuardTests
TmarDurableGuard only for Recovery truth

No solution-wide tests.
No open-ended test loop.

One deterministic bounded repair + one rerun maximum.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1/

Required:

core-access.md
error-catalog.md
localization.md
behavior-parity.md
boundary-security.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1.task.md

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1
lastAcceptedCommit = actual W1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentHostCheckpoint = Admin/Access
coreAdminAccessState = CANONICAL_CODE_BASED_ERRORS_LOCALIZED
coreAdminHardcodedRuntimeText = ZERO
adminActorMissingDescriptor = FOUNDATION_401
adminTenantMissingDescriptor = FOUNDATION_503
adminAuthorizationUnavailableDescriptor = FOUNDATION_503
adminAuthorizationDeniedDescriptor = FOUNDATION_403
adminDevUnavailableLocalization = FOUNDATION_RESOURCE_RESOLVED
Host/Admin recursive file count = 18
Promotion dead residue = DEFERRED_W2
authorizerFamilyHygiene = DEFERRED_W2
Panel certification = PRESERVED
Development certification = PRESERVED
Party sellers certification = PRESERVED
Admin/Grid = HOST_ZERO
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ADMIN_ACCESS_AMC_001_W1
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

If docs stamp separate:
lastAcceptedCommit remains implementation SHA.

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

three missing admin.* codes become Foundation constants
all required admin.* descriptors are unique and correct
FoundationErrorResourceSet can resolve Foundation-owned admin.* resources
EN/FA resources exist for all five listed admin.* keys
AdminPanelAccess runtime hard-coded FA/EN titles = ZERO
HostAdminPanelAccess runtime hard-coded FA/EN titles = ZERO
expected failures use stable code-based canonical presentation
HTTP 401/403/503 semantics preserved
Allow/Unavailable/Deny semantics preserved
DevActorHeader security preserved
Marketplace Development synthetic tenant behavior preserved
no global PlatformHttpException redesign
module authorizer files untouched
HostPromotionAdminAuthorizer still deferred
Host/Admin file count remains 18
Panel/Development/Party certifications preserved
Grid remains absent
schema NONE
frontend UNCHANGED
focused validation PASS
Recovery points actual implementation SHA
automaticNextImplementationTask = NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1
Parent-Task: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Core-Admin-Access-State:
AdminPanelAccess-State:
HostAdminPanelAccess-State:
AdminPanelAccess-Hardcoded-Runtime-Text-State:
HostAdminPanelAccess-Hardcoded-Runtime-Text-State:
Admin-Actor-Missing-Code-State:
Admin-Tenant-Missing-Code-State:
Admin-Authorization-Unavailable-Code-State:
Admin-Authorization-Denied-Code-State:
Admin-Dev-Unavailable-Code-State:
Foundation-Descriptor-Uniqueness-State:
Foundation-Admin-Resource-Ownership-State:
Foundation-English-Resources-State:
Foundation-Persian-Resources-State:
Admin-Dev-Unavailable-Localization-State:
Canonical-Code-Based-Failure-State:
Http-Status-Parity-State:
Authorization-Branch-Parity-State:
DevActorHeader-State:
Marketplace-Development-State:
Exception-Message-Classification-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Sensitive-Logging-State:
Observability-State:
Host-Admin-Recursive-File-Count:
Promotion-Dead-Residue-State:
Authorizer-Family-Hygiene-State:
Panel-Certification-State:
Admin-Development-Certification-State:
Party-Sellers-Certification-State:
Admin-Grid-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Guard-State:
Recovery-State:
Last-Accepted-Commit-State:
Last-Accepted-Commit-Kind:
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

Do not start W2.
Do not delete HostPromotionAdminAuthorizer.
Do not certify Access.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK