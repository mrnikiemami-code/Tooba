PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SECURITY-AMC-001-W1
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_SECURITY_SELLER_ERROR_LOCALIZATION_HYGIENE
Title: Canonicalize Seller security failures to code-based semantics without changing authorization behavior
Estimated-Time-Minutes: 16
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

Parent Analyze:
ACCEPTED

Architect independently verified:

Host/Security exact production file count = 19
all 19 files are KEEP (2 global platform + 17 thin adapters)
dead adapters = 0
foreign Application/Infrastructure/Domain/DbContext = ZERO
hard-coded runtime title sites = 8
all 8 are in:
Seller/SellerPanelAccess.cs (6)
Seller/HostPartySellerAuthorizer.cs (1)
Seller/HostSupportSellerAuthorizer.cs (1)
message-text classification = ZERO
Host/Admin remains HOST_ADMIN_FULLY_CERTIFIED
latest accepted implementation lineage remains:
7a0d79b407c618d503acd1ef9866c519b4e5f070

ADDITIONAL ARCHITECT FINDING — REQUIRED W1 PARITY FIX

HostOrderSellerAuthorizer currently catches PlatformHttpException from ISellerPanelAccess
and converts its ErrorCode into SemanticError.

If SellerPanelAccess is migrated from PlatformHttpException to SemanticException,
HostOrderSellerAuthorizer MUST be updated in the same bounded wave so its
ResolveAsync behavior remains unchanged.

Therefore W1 production scope is FOUR files, not only the three title-producing files.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.

Do NOT certify Security.
Do NOT reconcile the stale 18→19 HostSecurity guard/count metadata in W1 unless a narrowly
required test assertion concerns exception semantics. Structural metadata reconciliation is W2.
Do NOT start another Host folder.
Do NOT reopen Host/Admin.

IN-SCOPE PRODUCTION FILES

src/backend/Host/Tooba.Host/Security/Seller/SellerPanelAccess.cs
src/backend/Host/Tooba.Host/Security/Seller/HostPartySellerAuthorizer.cs
src/backend/Host/Tooba.Host/Security/Seller/HostSupportSellerAuthorizer.cs
src/backend/Host/Tooba.Host/Security/Seller/HostOrderSellerAuthorizer.cs

Optional only if compile requires a direct constant reference adjustment:

SellerSecurityErrorCodes.cs

No other Security production file may change.

ARCHITECTURE DECISION

Expected Seller security failures must use stable machine codes and the existing
catalog/localization pipeline.

Use existing SemanticException + SemanticError.

Do NOT:

add a new exception type
redesign PlatformHttpException globally
create new seller error codes
change descriptor ownership
change HTTP statuses
change authorization permissions
change seller header semantics

EXISTING CANONICAL ERROR OWNERSHIP — PRESERVE

seller.actor.missing

HTTP 401
descriptor already exists in Order error catalog
EN/FA resource already exists in Order resources

seller.identity.missing

HTTP 400
descriptor already exists in Order error catalog
EN/FA resource already exists in Order resources

seller.authorization.unavailable

HTTP 503
descriptor already exists in Order error catalog
EN/FA resource already exists in Order resources

seller.authorization.denied

HTTP 403
canonical descriptor authority is Foundation
localization key resolves through existing Order seller.* resource set
do NOT duplicate descriptor in Party/Support/Foundation/Host

SellerSecurityErrorCodes strings MUST remain unchanged.

REQUIRED CHANGES

SELLERPANELACCESS — REMOVE SIX HARDCODED TITLES

Preserve exact actor behavior:

authenticated session UserId wins
Development-only X-Tooba-Dev-Actor-User-Id fallback
Guid.Empty rejected
otherwise seller.actor.missing / 401

Preserve exact seller context behavior:

X-Tooba-Seller-Party-Id is request context only
invalid/missing/empty Guid -> seller.identity.missing / 400

Preserve exact authorization behavior:

actor/seller empty -> seller.actor.missing / 401
edition unavailable -> seller.authorization.unavailable / 503
AuthorizationDecisionKind.Allow -> success
AuthorizationDecisionKind.Unavailable -> seller.authorization.unavailable / 503
any deny -> seller.authorization.denied / 403

Replace all six PlatformHttpException(title, code) expected failures with:
SemanticException(new SemanticError(stableCode))

After W1:
SellerPanelAccess runtime user-facing Persian/English exception prose = ZERO.

HOSTPARTYSELLERAUTHORIZER — REMOVE CAPABILITY TITLE

Preserve:

RequireViewAsync panel gate first
seller.settings.view mandatory
seller.settings.manage optional for CanManage
RequireManageAsync manage mandatory
IPlatformEffectiveAccessReader usage unchanged
GlobalWithinOwner + !DeniedByCeiling conditions unchanged

When required capability is missing:

code remains seller.authorization.denied
HTTP remains 403 through catalog
throw SemanticException(SemanticError(code))
no hard-coded title

Do NOT create a Party-specific seller settings denial code in this wave.

HOSTSUPPORTSELLERAUTHORIZER — REMOVE CAPABILITY TITLE

Preserve:

panel gate first
exact permissionId required
effective permissions semantics unchanged
GlobalWithinOwner + !DeniedByCeiling unchanged

When denied:

code remains seller.authorization.denied
HTTP remains 403
throw SemanticException(SemanticError(code))
no hard-coded title

Do NOT create a Support-specific denial code.

HOSTORDERSELLERAUTHORIZER — BEHAVIOR PARITY ADAPTER

Current ResolveAsync contract returns:
(actor, seller, SemanticError? Error)

Before W1:
it catches PlatformHttpException from seller panel seam and returns SemanticError(ex.ErrorCode).

After SellerPanelAccess moves to SemanticException:
update HostOrderSellerAuthorizer so expected Seller security failure still returns
SemanticError with the SAME stable code and does not escape as an exception.

Required:

catch SemanticException
return exception.Error (or an equivalent new SemanticError using only exception.Error.Code/Arguments)
do NOT inspect exception.Message
do NOT classify by text
preserve successful tuple exactly

Unknown exceptions must still propagate.

Do NOT keep a broad catch.
Do NOT catch Exception.
Do NOT convert unknown exceptions to actor missing.

If PlatformHttpException can still lawfully arise from another implementation of ISellerPanelAccess
on this path, retaining a typed PlatformHttpException compatibility catch is allowed ONLY if repository
truth proves it necessary; document it. Do not add message parsing.

DESCRIPTOR / LOCALIZATION VERIFICATION

Do NOT create new descriptors unless repo truth disproves parent Analyze.

Verify all four SellerSecurityErrorCodes resolve with exact statuses:

ActorMissing -> 401
IdentityMissing -> 400
AuthorizationUnavailable -> 503
AuthorizationDenied -> 403

Verify EN + FA resources exist and resolve.

Verify seller.authorization.denied has exactly one descriptor authority.

If any code is actually unregistered or duplicate in composed catalog:
STOP INCOMPLETE rather than silently inventing ownership.

HARDCODED RUNTIME TEXT TARGET

After W1, across ALL 19 Host/Security production files:

hard-coded runtime user-facing exception/presentation title sites = ZERO

Comments/XML documentation do not count.

Machine strings that are protocol/config identifiers do not count:

header names
permission IDs
error codes
CSP/header names
option keys

Do not remove legitimate protocol constants.

FAIL-CLOSED SEMANTICS

Must remain exact:

Seller panel:

missing actor => fail
invalid seller => fail
unavailable edition/auth => fail
denied => fail
only Allow succeeds

Party seller:

missing mandatory capability => fail 403

Support seller:

missing capability => fail 403

No fail-open branch introduced.

DEV HEADER SECURITY

Preserve:
X-Tooba-Dev-Actor-User-Id

session wins
Development-only
Guid non-empty
no logging

Preserve:
X-Tooba-Seller-Party-Id
as request context, never authorization authority by itself.

BOUNDARY PROTECTION

All four files remain Host thin security adapters/helper.

ZERO:

foreign Application
foreign Infrastructure
foreign Domain
foreign DbContext
cross-module persistence
service locator
business writes

Do not move files.

OBSERVABILITY / SENSITIVE DATA

No new logging.
No token/cookie/header value logging.
No custom ActivitySource/Meter/correlation.

OTHER SECURITY FILES — UNTOUCHED

Do NOT edit:

Root:

AuthSecurityHostOptions.cs
SecurityHeadersMiddleware.cs

Checkout:

CheckoutIdentityGate.cs
HostCheckoutActorPolicyAdapter.cs

Payment:

HostPaymentStorefrontAuthorizer.cs

Seller pass-through/core files other than four in scope:

HostCatalogSellerAuthorizer.cs
HostNotificationSellerAuthorizer.cs
HostOfferSellerAuthorizer.cs
HostPromotionSellerAuthorizer.cs
HostReturnSellerAuthorizer.cs
HostReviewsSellerAuthorizer.cs
HostSettlementSellerAuthorizer.cs
HostStorySellerAuthorizer.cs
HostSellerPanelAccess.cs

SellerSecurityErrorCodes.cs only if a direct compile/reference adjustment is required;
otherwise unchanged.

HOST/ADMIN PROTECTION

src/backend/Host/Tooba.Host/Admin/
must remain untouched and:
HOST_ADMIN_FULLY_CERTIFIED

STRUCTURE

Host/Security production count remains:
19

No files added/deleted/moved.

Path↔namespace remains exact.

STALE SECURITY GUARD — DEFERRED

Known stale state:
HostSecurityAmcGuardTests currently expects 18 and omits HostReviewsSellerAuthorizer.

This is a known metadata/guard drift from before W1.

Do NOT weaken it.
Do NOT use it as W1 success evidence.
Do NOT perform broad 18→19 reconciliation in W1.

W2 will reconcile exact structural guard/SoT truth before certification.

W1 may update only behavior-focused Seller tests that must change from PlatformHttpException
to SemanticException.

FOCUSED TESTS

Update/run focused behavior tests:

SellerPanelAuthorizationTests
HostOrder seller authorization/adaptation tests
Party seller authorizer focused tests
Support seller authorizer focused tests
any existing seller semantic-presentation test directly affected

Assertions must verify codes/status through canonical catalog where relevant, not exception title text.

Do NOT run stale HostSecurityAmcGuardTests as a gating W1 test until W2 reconciliation.
No solution-wide tests.

Build:

Host
directly affected Endpoints projects only if compile requires

One deterministic bounded repair + one rerun maximum.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-SECURITY-AMC-001-W1/

Required:

seller-failure-semantics.md
order-adapter-parity.md
localization.md
behavior-parity.md
boundary-security.md
validation.md
recovery.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SECURITY-AMC-001-W1.task.md

RECOVERY / SOT

On PASS record:

lastAcceptedTask = TB-TMAR-HOST-SECURITY-AMC-001-W1
lastAcceptedCommit = actual W1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentHostCheckpoint = Security
sellerSecurityFailureSemantics = SEMANTIC_EXCEPTION_CODE_BASED
sellerSecurityHardcodedRuntimeText = ZERO
hostOrderSellerAdapterParity = PRESERVED_SEMANTIC_ERROR_RETURN
securityProductionFileCount = 19
deadAdapterCount = 0
structureGuardReconcile = DEFERRED_W2
historicalSecurityCertification = NOT_REASSERTED_YET
Host/Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_SECURITY_AMC_001_W1
staleCurrentPointerState = ZERO
nextHostFolderStarted = false
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

8 hard-coded runtime title sites removed
SellerPanelAccess uses stable code-based SemanticException failures
Party capability denial uses same seller.authorization.denied code
Support capability denial uses same seller.authorization.denied code
HostOrderSellerAuthorizer preserves ResolveAsync SemanticError return behavior
no exception.Message/ex.Message classification
four seller codes resolve to 401/400/503/403 exactly
descriptor ownership remains unique
EN/FA localization resolves
fail-closed behavior preserved
DevActorHeader security preserved
SellerPartyHeader remains context-only
foreign App/Infra/Domain/DbContext ZERO
sensitive logging ZERO
no custom observability added
Security file count remains 19
Host/Admin FULLY_CERTIFIED preserved
stale structural guard explicitly deferred to W2
focused tests/build PASS
Recovery points actual implementation SHA
automaticNext NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SECURITY-AMC-001-W1
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Seller-Panel-Access-State:
Party-Seller-Authorizer-State:
Support-Seller-Authorizer-State:
Order-Seller-Adapter-State:
HostOrder-ResolveAsync-Parity-State:
Hardcoded-Runtime-User-Facing-Text-State:
Hardcoded-Title-Site-Count:
Seller-Actor-Missing-State:
Seller-Identity-Missing-State:
Seller-Authorization-Unavailable-State:
Seller-Authorization-Denied-State:
Descriptor-Uniqueness-State:
Localization-State:
Authorization-FailClosed-State:
DevActorHeader-State:
SellerPartyHeader-State:
Exception-Message-Classification-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Cross-Module-Persistence-State:
Sensitive-Logging-State:
Observability-State:
Security-Production-File-Count:
Dead-Adapter-Count:
Structure-Guard-Reconcile-State:
Historical-Security-Certification-State:
Host-Admin-Certification-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
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
Do not certify Security.
Do not start another Host folder.
Do not reopen Host/Admin.

Wait for Architect/user review.

END_TOOBA_TASK
