PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001-W2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_SECURITY_CERTIFY
Title: Independently certify the current 19-file Host/Security thin platform boundary
Estimated-Time-Minutes: 16
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

W2:
ACCEPTED

Architect independently verified:

Host/Security production file count = 19 exact
Seller file count = 14 exact
HostReviewsSellerAuthorizer is present, active and now in exact allowlist
HostSecurityAmcGuardTests exact membership is no longer weakened/stale
W1 SemanticException seller hygiene is locked by guard
historical 18-file Security certification metadata is explicitly stale/historical
current Security certification = NOT_YET_REASSERTED
production code unchanged by W2
latest accepted implementation remains W1:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b
Host/Admin remains HOST_ADMIN_FULLY_CERTIFIED

W2 docs/tests reconciliation commit:
b7a55c9905049c3419713c8142a054d581705c26

W2 tip/stamp-materialization commit:
2c9424223dd78a51422b5c8c3e9e54398a272108

STAMP SEMANTICS

The implementation authority MUST remain:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b

W2/W3 docs/tests certification stamps are not implementation commits.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY means independent re-verification.
Do NOT trust Analyze/W1/W2 claims without checking disk/current code.

No production redesign.
No migration.
No production repair.

If any material production defect is found:
Status = INCOMPLETE
Production-Repair-Required-State = YES
name the exact bounded repair task
STOP.

PRIMARY CERTIFICATION SCOPE

src/backend/Host/Tooba.Host/Security/

Expected exact structure:

Root — 2:

AuthSecurityHostOptions.cs
SecurityHeadersMiddleware.cs

Checkout — 2:

CheckoutIdentityGate.cs
HostCheckoutActorPolicyAdapter.cs

Payment — 1:

HostPaymentStorefrontAuthorizer.cs

Seller — 14:

SellerPanelAccess.cs
SellerSecurityErrorCodes.cs
HostSellerPanelAccess.cs
HostCatalogSellerAuthorizer.cs
HostNotificationSellerAuthorizer.cs
HostOfferSellerAuthorizer.cs
HostOrderSellerAuthorizer.cs
HostPartySellerAuthorizer.cs
HostPromotionSellerAuthorizer.cs
HostReturnSellerAuthorizer.cs
HostReviewsSellerAuthorizer.cs
HostSettlementSellerAuthorizer.cs
HostStorySellerAuthorizer.cs
HostSupportSellerAuthorizer.cs

Expected whole Security count:
19 exact

EXPECTED OWNERSHIP CLASSIFICATION

2 files:
KEEP_AS_GLOBAL_HOST_SECURITY_PLATFORM

17 files:
KEEP_AS_THIN_HOST_SECURITY_ADAPTER

HOST_ZERO is NOT the target.

CERTIFICATION LABELS ON PASS

Primary:
HOST_SECURITY_AMC_CERTIFIED

Boundary:
KEEP_THIN_PLATFORM_SECURITY_BOUNDARY_CERTIFIED_CURRENT_19

Global platform:
HOST_SECURITY_GLOBAL_PLATFORM_CERTIFIED

Thin adapters:
HOST_SECURITY_THIN_ADAPTERS_CERTIFIED

These labels are allowed ONLY if every mandatory audit passes.

MANDATORY CERTIFICATION AUDITS

PHYSICAL TREE / EXACT ALLOWLIST

Re-enumerate from disk.

Verify:

whole recursive .cs count = 19
Root = 2 exact
Checkout = 2 exact
Payment = 1 exact
Seller = 14 exact
no unexpected subfolder
no unexpected .cs file
HostReviewsSellerAuthorizer present
no stale/duplicate physical copies
no aliases/shims/TypeForwardedTo
PATH ↔ NAMESPACE

Verify exact:

Root:
Tooba.Host.Security

Checkout:
Tooba.Host.Security.Checkout

Payment:
Tooba.Host.Security.Payment

Seller:
Tooba.Host.Security.Seller

No exception to path-derived namespace.

GLOBAL PLATFORM FILES

AuthSecurityHostOptions:
independently certify:

Host-owned configuration only
validation/fail-fast behavior coherent
no module business policy
no secrets hard-coded
no foreign module Application/Infra/Domain
no user-facing runtime prose debt
no sensitive logging

SecurityHeadersMiddleware:
independently certify:

Host HTTP platform middleware only
no module business ownership
security headers behavior remains bounded
no sensitive data logging
no foreign business-layer dependency
no custom parallel correlation/tracing

Do NOT change header policy in Cert.

CHECKOUT SECURITY

CheckoutIdentityGate:
verify:

Contracts-only Catalog checkout policy dependency
session/security composition only
expected failure uses SemanticException stable code
checkout.authentication_required descriptor/localization valid
no InvalidOperation/message-text remap
no hard-coded runtime presentation text
fail-closed where authentication is required

HostCheckoutActorPolicyAdapter:
verify:

implements Payment.Contracts port
thin delegation only
no Payment.Application/Infrastructure/Domain
no business workflow ownership
no service locator
PAYMENT SECURITY

HostPaymentStorefrontAuthorizer:
verify:

active production DI/consumer
Payment.Endpoints seam only where accepted
thin session/identity adaptation
no payment business policy
no hard-coded runtime presentation text
no service locator
no foreign Payment.Application/Infrastructure/Domain
SELLER PANEL CORE

HostSellerPanelAccess:
verify:

sole DI implementation of ISellerPanelAccess
SellerPanelAccess remains implementation helper, not duplicate DI authority

SellerPanelAccess exact semantics:

authenticated session actor wins
DevActorHeader only in Development
DevActor Guid non-empty
SellerPartyHeader context only
invalid seller => seller.identity.missing / 400
missing actor => seller.actor.missing / 401
unavailable edition/auth => seller.authorization.unavailable / 503
deny => seller.authorization.denied / 403
allow only success
expected failures use SemanticException/SemanticError
PlatformHttpException expected-failure usage = ZERO
hard-coded runtime title = ZERO
fail-open = ZERO

SellerSecurityErrorCodes:
verify stable exact strings unchanged:

seller.actor.missing
seller.identity.missing
seller.authorization.denied
seller.authorization.unavailable
SELLER AUTHORIZER FAMILY

Independently classify and verify every active authorizer.

Pass-through:

Catalog
Notification
Offer
Promotion
Return
Reviews
Settlement
Story

Order adapter:

HostOrderSellerAuthorizer
success tuple unchanged
catches SemanticException only for expected panel failures
returns ex.Error / same SemanticError semantics
unknown exceptions propagate
no broad Exception catch
no ex.Message classification

Capability adapters:

HostPartySellerAuthorizer
HostSupportSellerAuthorizer

Verify:

panel gate first
effective permission seam is BuildingBlocks-neutral
permission checks remain exact
GlobalWithinOwner + !DeniedByCeiling preserved
deny uses seller.authorization.denied / canonical 403
no module business authority leaked into Host
ACTIVE / DEAD CONSUMER AUDIT

Verify current production:

all expected edge adapters are registered/consumed
dead adapters = ZERO
unregistered orphan = ZERO
HostReviewsSellerAuthorizer active DI confirmed
no duplicate module/Host authority registration

Record exact active adapter count using current definition from Analyze/W2.
Do not force 14 if repo truth defines helper/DI count differently; explain exact counted set.

ERROR DESCRIPTOR / LOCALIZATION MATRIX

Certify Seller access path codes:

seller.actor.missing -> 401
seller.identity.missing -> 400
seller.authorization.unavailable -> 503
seller.authorization.denied -> 403

Verify:

descriptor exists exactly once in composed relevant catalog authority
no status mismatch
EN resolves
FA resolves
no fallback-only where a resource is expected
no hard-coded title bypass

Also certify:
checkout.authentication_required
through its canonical Foundation owner.

If full-repo catalog uniqueness test still has unrelated reservation.policy.* historical duplicate:
document as OUTSIDE_SECURITY_CERT_SCOPE only if the Security-path codes are unique and the defect is pre-existing.
Do NOT repair unrelated Catalog/Order production code in this Cert.

HARDCODED RUNTIME USER-FACING TEXT

Scan ALL 19 Security production files.

Forbidden:

hard-coded Persian runtime exception title
hard-coded English user-facing exception title
ProblemDetails/Results prose from Security
SemanticError created from prose instead of stable code

Allowed:

XML comments
machine error codes
header names
permission IDs
security header values/protocol values
option section names

Required state:
ZERO runtime user-facing hard-coded presentation text.

EXCEPTION MESSAGE CLASSIFICATION

Across all 19:
ZERO:

ex.Message classification
exception.Message classification
Message.Contains/StartsWith/Equals for business/error selection
broad InvalidOperationException expected-failure remap

Unknown exceptions propagate unless current canonical boundary explicitly owns them.

CONTRACT / MICROSERVICE BOUNDARY

Across all 19 verify ZERO:

foreign Application
foreign Infrastructure
foreign Domain
foreign DbContext
cross-module persistence
cross-module joins
module internal implementation dependency
RequestServices/service locator

Allowed:

BuildingBlocks neutral security/platform seams
module Contracts
module Endpoints auth seam interfaces/codes where accepted edge-adapter pattern applies

No module -> Host dependency.

MODULE BUSINESS AUTHORITY

Host/Security may own:

HTTP security middleware
security options
session/tenant/edition composition
seller context security
thin authorization adapters

Host/Security must own ZERO:

business commands
domain rules
product/order/payment business state
persistence
workflow orchestration
pricing/inventory/order mutations
FAIL-CLOSED AUDIT

Verify:

seller unavailable => fail
seller denied => fail
seller invalid identity => fail
Party mandatory capability missing => fail
Support permission missing => fail
Checkout authentication-required path => fail
no unavailable/deny branch silently returns success
SENSITIVE LOGGING

Verify ZERO unsafe logging of:

Authorization header
cookies
bearer/token
password
raw DevActorHeader
raw SellerPartyHeader
secrets

Actor/seller/tenant IDs:
if logged anywhere under Security, verify structured legitimate operational need and no sensitive context leakage.
Expected current state from Analyze: no logs.

OBSERVABILITY / CORRELATION

Verify ZERO custom parallel:

ActivitySource
Meter
traceparent
correlation ID system
custom logging scope

Security relies on canonical global observability.

OPTIONS / SECURITY HEADER SANITY

Without redesign, inspect:

options validator actually protects invalid configuration
CORS/HSTS/body/rate-limit related values have sane fail-fast handling where applicable
SecurityHeadersMiddleware does not add obviously contradictory duplicate headers
Production-only HSTS behavior remains deliberate

If this exposes a material security flaw:
STOP INCOMPLETE; do not repair in Cert.

COHESION

Verify:

root files global platform only
Checkout capability isolated
Payment capability isolated
Seller security cohesive
no god file requiring split
no mixed business responsibility
HISTORICAL CERTIFICATION METADATA

Verify:

historical 18-file cert blocks remain clearly HISTORICAL/SUPERSEDED/STALE
they cannot be mistaken for current authority
current W2 block says 19 and NOT_YET_REASSERTED before this Cert

On PASS:
current certification must become authoritative 19-file certification.

Do not erase historical lineage.

DURABLE CERTIFICATION GUARD

Create:

HostSecurityAmcCertGuardTests

or promote/extend existing guard if a separate guard would be redundant.

Preferred: a dedicated current certification guard.

It must prove at minimum:

exact 19 tree / folder counts
exact namespaces
Reviews adapter present
core seller SemanticException semantics
Order adapter parity
seller code/status matrix
EN/FA localization resolution
no hard-coded runtime titles
no ex.Message classification
no forbidden foreign layers
no DbContext/persistence/service locator
active adapter/DI seams
Checkout/Payment thin boundary
Host/Admin certification preserved
historical 18 cert not current authority

Do not weaken HostSecurityAmcGuardTests.

PROTECTED HOST/ADMIN

Recheck only enough to ensure no regression:

current SoT still contains HOST_ADMIN_FULLY_CERTIFIED
no Host/Admin production change in Cert commit

Do not reopen Admin.

PRODUCTION CHANGE RULE

Expected:
Production-Repair-Required-State = NONE

Certification may change:

tests/guards
docs/evidence
SoT/current authority metadata

It must NOT change:
src/backend/Host/Tooba.Host/Security/**/*.cs

If production change is necessary:
STOP INCOMPLETE.

FOCUSED VALIDATION

Build:

Host
Host.Tests
directly referenced module Endpoints only if certification guard compile requires

Run focused:

HostSecurityAmcGuardTests
HostSecurityAmcCertGuardTests (new)
SellerPanelAuthorizationTests
focused Checkout security tests
focused Payment storefront auth tests
focused seller Party/Support/Order adapter tests
relevant error catalog/localization tests
TmarDurableGuard current-state assertions

No solution-wide tests.
No open-ended loop.

One deterministic certification guard/docs correction + one rerun maximum.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT/

Required:

certification-summary.md
physical-tree.md
consumer-di.md
global-platform.md
checkout-payment.md
seller-authorization.md
error-localization.md
hardcoded-runtime-text.md
boundary-microservice.md
security-observability.md
historical-certification.md
validation.md
recovery.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT.task.md

RECOVERY / SOT

On PASS:

Top-level:

lastAcceptedTask = TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT
lastAcceptedCommit remains:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-SECURITY-AMC-001-W1
currentHostCheckpoint = Security
nextTask = USER_REVIEW_HOST_SECURITY_AMC_001_W3_CERT
workflowStop = USER_REVIEW_HOST_SECURITY_AMC_001_W3_CERT
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Certification/docs/tests commit:
record separately in result-evidence/SoT-stamp fields.

Add/update current block:
hostSecurityAmc001W3Cert

Required:

certificationState = HOST_SECURITY_AMC_CERTIFIED
boundaryState = KEEP_THIN_PLATFORM_SECURITY_BOUNDARY_CERTIFIED_CURRENT_19
globalPlatformState = HOST_SECURITY_GLOBAL_PLATFORM_CERTIFIED
thinAdaptersState = HOST_SECURITY_THIN_ADAPTERS_CERTIFIED
securityProductionFileCount = 19
sellerProductionFileCount = 14
deadAdapterCount = 0
hardcodedRuntimeText = ZERO
exceptionMessageClassification = ZERO
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
crossModulePersistence = ZERO
moduleBusinessAuthority = ZERO
productionRepairRequired = false
implementationCommit = baa05e6b...
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_SECURITY_AMC_001_W3_CERT

Historical 18-file blocks remain historical.

GIT

Work from latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Push only on PASS.

SUCCESS CRITERIA

PASS only if:

exact Security tree 19
exact Seller 14
Reviews active/present
path/namespace exact
two root global platform files justified
Checkout/Payment thin adapters justified
Seller panel semantics exact
all seller adapters independently valid
dead adapter ZERO
seller error code/status matrix exact
EN/FA resolution PASS
checkout auth error canonical
hard-coded runtime user-facing text ZERO
message-text classification ZERO
foreign App/Infra/Domain ZERO
DbContext/persistence ZERO
module business authority ZERO
fail-closed PASS
sensitive logging PASS
observability PASS
cohesion PASS
historical 18 cert non-authoritative
durable current certification guard PASS
Host/Admin certification preserved
production repair required NONE
focused builds/tests PASS
certification label exactly HOST_SECURITY_AMC_CERTIFIED
boundary label exactly KEEP_THIN_PLATFORM_SECURITY_BOUNDARY_CERTIFIED_CURRENT_19
implementation SHA remains baa05e6b...
automaticNext NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001-W2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Boundary-Certification-State:
Global-Platform-Certification-State:
Thin-Adapters-Certification-State:
Security-Production-File-Count:
Seller-Production-File-Count:
Exact-Tree-State:
Path-Namespace-State:
HostReviewsSellerAuthorizer-State:
Consumer-DI-State:
Dead-Adapter-Count:
Global-Options-State:
Security-Headers-Middleware-State:
Checkout-Security-State:
Payment-Security-State:
Seller-Panel-Core-State:
Seller-Authorizer-Family-State:
Order-Seller-Adapter-Parity-State:
Seller-Error-Code-Status-State:
Checkout-Error-Code-State:
Descriptor-Uniqueness-State:
Localization-Resolution-State:
Hardcoded-Runtime-User-Facing-Text-State:
Exception-Message-Classification-State:
Authorization-FailClosed-State:
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
Historical-18-File-Cert-State:
Durable-Cert-Guard-State:
Host-Admin-Certification-State:
Production-Repair-Required-State:
Production-Code-Change-State:
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
Do not reopen Host/Admin.
Do not repair unrelated reservation.policy catalog debt.

Wait for Architect/user review.

END_TOOBA_TASK
