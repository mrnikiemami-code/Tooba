PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2
Parent-Task: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_ACCESS_AUTHORIZER_FAMILY_HYGIENE
Title: Remove authorizer hard-coded runtime titles, complete module localization seams, and delete dead Promotion Host adapter
Estimated-Time-Minutes: 18
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

W1:
ACCEPTED

Architect independently verified:

core AdminPanelAccess / HostAdminPanelAccess now use SemanticException + stable Foundation codes
core hard-coded runtime titles = ZERO
admin.actor.missing / tenant.missing / authorization.unavailable descriptors registered
admin.authorization.denied preserved
admin.dev.unavailable now has real Foundation resource localization
FoundationErrorResourceSet owns admin.*
EN/FA Foundation resources exist
HTTP 401/403/503 parity preserved
DevActorHeader Development-only behavior preserved
Marketplace synthetic tenant behavior preserved
Host/Admin recursive file count remains 18
implementation commit:
6f06f76631413e94efbcbaeabc8b7218688389d7

W1 result/docs stamp:
7d03a4dd0406a5490d7a0e21b404978a7ad74f31

ADDITIONAL ARCHITECT FINDINGS FOR W2

HostOrderAdminAuthorizer uses:
order.authorization.unavailable / 503
order.operation.denied / 403

OrderErrorCatalogContributor currently registers:

order.authorization.unavailable
but DOES NOT register:
order.operation.denied

Order resources currently include:

order.authorization.unavailable
but DO NOT include:
order.operation.denied

Therefore W2 MUST close the missing Order descriptor/resource before replacing hard-coded title throws.

Support and Wallet register descriptors for:
support.authorization.unavailable
wallet.authorization.unavailable

but currently have no dedicated resource sets/resx for these keys.
If converted to SemanticException without module resources, presentation would rely on SafeTitleFallback only.

Architecture lock requires real canonical localization rather than inline/hard-coded runtime prose.

W2 MUST add narrow EN/FA resource sets for these authorization-unavailable keys.

Support/Wallet deny uses:
admin.authorization.denied
which is already Foundation-owned and W1-localized.
Do NOT duplicate it in module resources/catalogs.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.
Do NOT certify Access in W2.
Do NOT start another Host folder.
Do NOT redesign authorization.

IN-SCOPE HOST FILES

Modify:

src/backend/Host/Tooba.Host/Admin/Access/Authorizers/HostOrderAdminAuthorizer.cs
src/backend/Host/Tooba.Host/Admin/Access/Authorizers/HostSupportAdminAuthorizer.cs
src/backend/Host/Tooba.Host/Admin/Access/Authorizers/HostWalletAdminAuthorizer.cs

Delete:

src/backend/Host/Tooba.Host/Admin/Access/Authorizers/HostPromotionAdminAuthorizer.cs

Do NOT edit the seven active pass-through authorizers or HostOrderAdminEffectiveAccessReader unless a test/compile reference requires no-production semantic change.

IN-SCOPE MODULE PRESENTATION FILES

Order:

Tooba.Order.Endpoints/Errors/OrderErrorCatalogContributor.cs
Tooba.Order.Endpoints/Resources/OrderErrors.resx
Tooba.Order.Endpoints/Resources/OrderErrors.fa.resx

Support:

add Support Endpoints resource ownership + EN/FA resources for support.authorization.unavailable
update SupportEndpointModule presentation registration as required

Wallet:

add Wallet Endpoints resource ownership + EN/FA resources for wallet.authorization.unavailable
update WalletEndpointModule presentation registration as required

Use existing BuildingBlocks IErrorResourceSet pattern.
Do not invent another localization abstraction.

REQUIRED W2 CHANGES

ORDER AUTHORIZER — CODE-BASED FAILURES

Preserve exact capability semantics:

panel gate through IAdminPanelAccess
permission resource = Permission/{permissionId}
permission relation = Check
SingleStore CallContext
tenant id fallback behavior unchanged
Allow = return
Unavailable = order.authorization.unavailable / 503
Deny = order.operation.denied / 403

Replace hard-coded Persian titles with code-based SemanticException/SemanticError.

After W2:
HostOrderAdminAuthorizer runtime user-facing FA/EN prose = ZERO.

No ex.Message classification.

ORDER MISSING DESCRIPTOR

Register OrderErrorCodes.OperationDenied exactly once in OrderErrorCatalogContributor:

code = order.operation.denied
HTTP = 403
classification = Forbidden
localization key = same code

Preserve existing AuthorizationUnavailable descriptor:

503
Platform

Do not move either code to Foundation.

Descriptor duplication = ZERO.

ORDER LOCALIZATION RESOURCES

Add EN and FA values for:

order.operation.denied

Verify existing EN/FA values for:

order.authorization.unavailable

Both must resolve from OrderErrorResourceSet.

No inline title in Host throw.

SUPPORT AUTHORIZER — CODE-BASED FAILURES

Preserve exact capability semantics:

panel gate through IAdminPanelAccess
permission check unchanged
Allow unchanged
Unavailable = support.authorization.unavailable / 503
Deny = admin.authorization.denied / 403

Replace both hard-coded Persian titles with code-based SemanticException/SemanticError.

For deny use canonical:
FoundationErrorCodes.AdminAuthorizationDenied
or exact existing seam constant only if descriptor authority remains Foundation.
Do NOT create duplicate descriptor.

After W2:
HostSupportAdminAuthorizer runtime user-facing FA/EN prose = ZERO.

SUPPORT LOCALIZATION

Keep existing descriptor:
SupportAdminAuthorizationCodes.AuthorizationUnavailable
= support.authorization.unavailable
503 Platform.

Add narrow Support Endpoints resources:

Preferred shape:

Resources/SupportErrorResources.cs
Resources/SupportErrors.resx
Resources/SupportErrors.fa.resx

Resource set must own only the Support namespace needed by canonical Support presentation, minimally:
support.*

Register IErrorResourceSet from SupportEndpointModule.

At minimum EN/FA key:
support.authorization.unavailable

Do NOT duplicate Foundation-owned:
admin.authorization.denied

If Support already has another canonical localization mechanism discovered during execution, reuse it instead of adding parallel files.

WALLET AUTHORIZER — CODE-BASED FAILURES

Preserve exact capability semantics:

panel gate through IAdminPanelAccess
permission check unchanged
Allow unchanged
Unavailable = wallet.authorization.unavailable / 503
Deny = admin.authorization.denied / 403

Replace both hard-coded Persian titles with code-based SemanticException/SemanticError.

After W2:
HostWalletAdminAuthorizer runtime user-facing FA/EN prose = ZERO.

WALLET LOCALIZATION

Keep existing descriptor:
WalletAdminAuthorizationCodes.AuthorizationUnavailable
= wallet.authorization.unavailable
503 Platform.

Add narrow Wallet Endpoints resources:

Preferred shape:

Resources/WalletErrorResources.cs
Resources/WalletErrors.resx
Resources/WalletErrors.fa.resx

Resource set should own minimally:
wallet.*

Register IErrorResourceSet from WalletEndpointModule.

At minimum EN/FA key:
wallet.authorization.unavailable

Do NOT duplicate:
admin.authorization.denied

If an existing canonical Wallet localization mechanism is discovered, reuse it.

DELETE DEAD PROMOTION HOST ADAPTER

Delete:

src/backend/Host/Tooba.Host/Admin/Access/Authorizers/HostPromotionAdminAuthorizer.cs

Reason:
DEAD_ZERO_CONSUMER_RESIDUE

Verified parent Analyze:

no Program DI registration
PromotionEndpointModule owns active PromotionAdminAuthorizer
no production consumer

After deletion:

stale production reference = ZERO
no shim
no alias
no replacement Host adapter

Host/Admin recursive file count:
18 -> 17

Access files:
13 -> 12

Active Host authorizer adapters remain:
10

Dead Host authorizers after W2:
ZERO

EXACT ALLOWLIST / GUARDS

Update exact Host/Admin structure guards:

remove HostPromotionAdminAuthorizer.cs
exact recursive count 18 -> 17

Update only assertions/metadata directly impacted.

Do NOT weaken exact checks.

Panel/Development/Party cert guards must continue to pass.

PASS-THROUGH AUTHORIZERS

These remain untouched behaviorally:

HostLocalizationAdminAuthorizer
HostOperatorProfileAdminAuthorizer
HostPaymentAdminAuthorizer
HostReturnAdminAuthorizer
HostSettlementAdminAuthorizer
HostUserPreferenceAdminAuthorizer
HostOrderAdminEffectiveAccessReader

Verify they contain no hard-coded runtime titles.

CORE W1 PROTECTION

Do NOT regress:

AdminPanelAccess
HostAdminPanelAccess
Foundation admin.* codes/descriptors/resources
DevActorHeader
Marketplace branch

Core Access hardcoded runtime text remains ZERO.

ERROR / LOCALIZATION MATRIX AFTER W2

Must prove:

admin.actor.missing -> Foundation / 401 / EN+FA
admin.tenant.missing -> Foundation / 503 / EN+FA
admin.authorization.unavailable -> Foundation / 503 / EN+FA
admin.authorization.denied -> Foundation / 403 / EN+FA
admin.dev.unavailable -> Foundation / 404 / EN+FA

order.authorization.unavailable -> Order / 503 / EN+FA
order.operation.denied -> Order / 403 / EN+FA

support.authorization.unavailable -> Support / 503 / EN+FA
wallet.authorization.unavailable -> Wallet / 503 / EN+FA

Descriptor ownership exactly once for each.

No hard-coded throw title on these Access paths.

BOUNDARY / MICROservice SAFETY

Host authorizers may depend only on:

BuildingBlocks neutral abstractions
module Endpoints seam/interfaces/codes
module Contracts where already accepted

Forbidden:

foreign Application
foreign Infrastructure
foreign Domain
foreign DbContext
cross-module persistence
module -> Host dependency

Do not change this posture.

SECURITY / OBSERVABILITY

Preserve:

fail-closed Unavailable behavior
no sensitive logs
no custom telemetry
no manual correlation/tracing

No password/token/cookie/header logging.

PROTECTED STATE

Must remain:

Panel CERTIFIED
Admin/Development CERTIFIED
Party sellers CERTIFIED
Admin/Grid HOST_ZERO
schema NONE
frontend UNCHANGED
FOCUSED VALIDATION

Build:

BuildingBlocks only if directly required by shared types (expected no W2 BuildingBlocks change)
Order.Endpoints
Support.Endpoints
Wallet.Endpoints
Host

Run focused:

Order error catalog/resource tests
Support semantic presentation/auth tests
Wallet focused presentation/auth tests
Host Admin authorizer tests/guards
HostAdminPanelAmcCertGuardTests
exact Host/Admin structure guards
TmarDurableGuard only for Recovery truth

No solution-wide tests.
No open-ended loop.

One deterministic bounded repair + one rerun maximum.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2/

Required:

authorizer-family.md
promotion-deletion.md
error-catalog.md
localization.md
behavior-parity.md
boundary-security.md
structure.md
validation.md
recovery.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2.task.md

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2
lastAcceptedCommit = actual W2 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentHostCheckpoint = Admin/Access
authorizerFamilyHygiene = COMPLETE
authorizerHardcodedRuntimeText = ZERO
HostPromotionAdminAuthorizer = REMOVED
deadHostAuthorizerCount = 0
activeHostAuthorizerAdapterCount = 10
Access production file count = 12
Host/Admin recursive file count = 17
orderOperationDeniedDescriptor = ORDER_403
orderAuthorizationUnavailable = ORDER_503_LOCALIZED
supportAuthorizationUnavailable = SUPPORT_503_LOCALIZED
walletAuthorizationUnavailable = WALLET_503_LOCALIZED
coreW1State = PRESERVED
Panel certification = PRESERVED
Admin/Development certification = PRESERVED
Party sellers certification = PRESERVED
Admin/Grid = HOST_ZERO
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ADMIN_ACCESS_AMC_001_W2
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

If docs/stamp separate:
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

Order/Support/Wallet Host authorizer hard-coded runtime titles = ZERO
stable machine codes unchanged
HTTP 403/503 parity preserved
Allow/Unavailable/Deny branches preserved
order.operation.denied descriptor registered exactly once
Order EN/FA localization exists for operation.denied + authorization.unavailable
Support EN/FA localization resolves support.authorization.unavailable
Wallet EN/FA localization resolves wallet.authorization.unavailable
admin.authorization.denied still Foundation-owned only
HostPromotionAdminAuthorizer removed
dead Host authorizer count = ZERO
Access production file count = 12
Host/Admin recursive file count = 17
exact guards updated, not weakened
pass-through adapters unchanged
foreign App/Infra/Domain/DbContext = ZERO
no message-text classification
sensitive logging ZERO
protected certifications preserved
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
Task-ID: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2
Parent-Task: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Authorizer-Family-Hygiene-State:
Order-Authorizer-State:
Support-Authorizer-State:
Wallet-Authorizer-State:
Authorizer-Hardcoded-Runtime-Text-State:
Order-Authorization-Unavailable-State:
Order-Operation-Denied-State:
Order-Descriptor-State:
Order-Localization-State:
Support-Authorization-Unavailable-State:
Support-Localization-State:
Wallet-Authorization-Unavailable-State:
Wallet-Localization-State:
Admin-Authorization-Denied-Ownership-State:
Promotion-Host-Authorizer-State:
Dead-Host-Authorizer-Count:
Active-Host-Authorizer-Adapter-Count:
Access-Production-File-Count:
Host-Admin-Recursive-File-Count:
Host-Admin-Exact-Allowlist-State:
PassThrough-Authorizers-State:
Core-W1-State:
Authorization-Branch-Parity-State:
Http-Status-Parity-State:
Exception-Message-Classification-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Cross-Module-Persistence-State:
Sensitive-Logging-State:
Observability-State:
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

Do not start W3 CERT.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK