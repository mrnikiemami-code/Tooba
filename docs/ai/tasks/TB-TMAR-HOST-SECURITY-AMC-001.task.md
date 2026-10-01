PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SECURITY-AMC-001
Parent-Task: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_SECURITY_ANALYZE
Title: Analyze Host/Security platform middleware, checkout/payment adapters, and seller authorization seams
Estimated-Time-Minutes: 18
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

Parent certification:
ACCEPTED

Architect independently verified:

Host/Admin/Access = HOST_ADMIN_ACCESS_AMC_CERTIFIED
whole Host/Admin = HOST_ADMIN_FULLY_CERTIFIED
Host/Admin exact production file count = 17
Grid = HOST_ZERO_CERTIFIED
Panel/Development/Party sellers protected certifications preserved
certification commit 23acf1bfa514f48d4a111a732d5bcaae45f78cbd changed docs/tests/guards only
latest accepted implementation lineage remains:
7a0d79b407c618d503acd1ef9866c519b4e5f070

NEXT ACTIVE RECOVERY UNIT

src/backend/Host/Tooba.Host/Security/

Repository snapshot currently shows these areas:

Root:

AuthSecurityHostOptions.cs
SecurityHeadersMiddleware.cs

Checkout:

CheckoutIdentityGate.cs
HostCheckoutActorPolicyAdapter.cs

Payment:

HostPaymentStorefrontAuthorizer.cs

Seller:

HostCatalogSellerAuthorizer.cs
HostNotificationSellerAuthorizer.cs
HostOfferSellerAuthorizer.cs
HostOrderSellerAuthorizer.cs
HostPartySellerAuthorizer.cs
HostPromotionSellerAuthorizer.cs
HostReturnSellerAuthorizer.cs
HostReviewsSellerAuthorizer.cs
HostSellerPanelAccess.cs
HostSettlementSellerAuthorizer.cs
HostStorySellerAuthorizer.cs
HostSupportSellerAuthorizer.cs
SellerPanelAccess.cs
SellerSecurityErrorCodes.cs

Expected current production file count from repository tree:
19

Do NOT assume. Re-enumerate from disk.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

ANALYSIS ONLY.

Do NOT edit production code.
Do NOT migrate.
Do NOT certify.
Do NOT start another Host folder.
Do NOT reopen Host/Admin.

PROTECTED ACCEPTED STATE

Must remain untouched:

HOST_ADMIN_FULLY_CERTIFIED
Admin/Grid HOST_ZERO
Panel/Development certifications
Party sellers certification
Storefront AMC accepted historical ownership moves
Payment certified module state
Offer certified module state
Order accepted/certified module state
frontend frozen

MANDATORY ANALYSIS

EXACT TREE / FILE ENUMERATION

Enumerate start and end:

src/backend/Host/Tooba.Host/Security/

Record:

exact recursive production .cs count
exact filenames
exact namespaces
top-level and nested folders

Verify current expected subfolders:

Checkout
Payment
Seller

Verify root flat files.

PER-FILE DISPOSITION

For EVERY Security production file classify exactly one:

KEEP_AS_GLOBAL_HOST_SECURITY_PLATFORM
KEEP_AS_THIN_HOST_SECURITY_ADAPTER
MOVE_TO_MODULE
DEAD_ZERO_CONSUMER_RESIDUE
MUST_SPLIT
BLOCKED_NEEDS_ARCHITECT_DECISION

Do not classify whole folder with one label before per-file evidence.

CONSUMER / DI INVENTORY

For each type:

interface implemented/exposed
Program DI registration
all production consumers
test-only references separately
module owner of consumed seam

Explicitly inspect:

AuthSecurityHostOptions
SecurityHeadersMiddleware
CheckoutIdentityGate
HostCheckoutActorPolicyAdapter
HostPaymentStorefrontAuthorizer
HostSellerPanelAccess
SellerPanelAccess
SellerSecurityErrorCodes
every Host*SellerAuthorizer

No consumer guessing.

ROOT GLOBAL SECURITY FILES

AuthSecurityHostOptions:
determine whether it is truly global Host configuration/security infrastructure.

Audit:

options ownership
validation/fail-fast
secrets
environment-specific branches
module-specific policy leakage
hard-coded runtime user-facing text

SecurityHeadersMiddleware:
determine whether it is legitimate Host HTTP platform middleware.

Audit:

CSP/HSTS/frame/content headers
environment branches
unsafe permissive headers
module-specific behavior
no business authority
no sensitive logging

Do not redesign headers in Analyze.

CHECKOUT SUBFOLDER

Historical accepted Storefront R2 moved:

CheckoutIdentityGate
HostCheckoutActorPolicyAdapter
into Host/Security/Checkout.

Independently re-evaluate current truth.

For each:

current interface/consumer
Catalog.Contracts / Order.Contracts / neutral seam dependencies
whether Host ownership is still justified
whether any module business policy leaked into Host
whether code is active/dead
hard-coded presentation text
exception semantics
ex.Message usage

Do NOT reopen Storefront implementation.

PAYMENT SUBFOLDER

HostPaymentStorefrontAuthorizer:

Audit:

exact interface
active DI registration
Payment.Endpoints/Contracts dependency
whether it remains a thin edge security adapter
code/status failure semantics
hard-coded runtime text
message parsing
dead/duplicate module-owned authorizer possibility

Do NOT alter Payment.

SELLER SECURITY FAMILY — FULL AUDIT

This is the largest part.

For:

HostSellerPanelAccess
SellerPanelAccess
SellerSecurityErrorCodes
all Host*SellerAuthorizer files

Build a matrix:

File
Interface
DI registration
Production consumers
Module seam
Pass-through vs capability logic
Stable codes
Hard-coded runtime text
Foreign dependencies
Disposition

Determine:

whether HostSellerPanelAccess is the single canonical seller-panel DI seam
whether SellerPanelAccess is an implementation helper or duplicate authority
whether any Host seller authorizer is dead because module now owns its authorizer
whether any adapter contains module business policy rather than transport/security adaptation
SELLER ERROR / LOCALIZATION MATRIX

Inventory ALL stable error codes used under Security/Seller.

For every code:
Code
Producer
HTTP
Descriptor owner
Localization key
EN resource
FA resource
Duplicate?
Hard-coded title/message?

At minimum inspect:

seller session/actor/tenant/authorization codes
seller authorization unavailable/denied codes
module-specific seller capability codes used by Order/Support/etc.

Flag:

unregistered code
fallback-only localization
duplicated descriptor
inline Persian/English runtime title
status mismatch

Do not repair yet.

HARDCODED USER-FACING TEXT

Strict lock:
NO hard-coded runtime Persian/English user-facing presentation text in production layers.

Scan all 19 expected files for:

PlatformHttpException title
SemanticError prose
inline Results/ProblemDetails text
exception title/message
user-facing English/Persian fallbacks

Distinguish:

comments/XML docs allowed
machine identifiers/codes allowed
header values/constants allowed when protocol-level, not user-facing prose

Report exact file + line/symbol + code for every violation.

EXCEPTION / MESSAGE CLASSIFICATION

Must audit:

ex.Message
exception.Message
Message.Contains/StartsWith/Equals
message-to-code/status classification
broad InvalidOperationException remapping

Expected state target is ZERO.
If present, classify blocker precisely.

CONTRACT / MICROSERVICE BOUNDARY

For all Security files classify dependencies.

Forbidden:

foreign Application
foreign Infrastructure
foreign Domain
foreign DbContext
cross-module persistence
cross-module joins
module internal implementation references

Allowed if justified:

BuildingBlocks neutral security
module Endpoints seam interfaces/codes
module Contracts
Host-only platform configuration

Produce:
File | Dependency | Layer | Allowed? | Reason

BUSINESS AUTHORITY TEST

Host Security may own:

HTTP security middleware
host/session/tenant security composition
thin authorization adapters
platform options

Host Security must not own:

business commands
domain rules
product/order/payment business policy
persistence
module state mutation
business workflow orchestration

Flag exact violations.

AUTHORIZATION FAIL-CLOSED AUDIT

For every capability adapter:

Allow path
Deny path
Unavailable path
tenant/store context
edition
permission resource/relation
fail-open possibility

No speculative redesign.
Document current semantics.

SECURITY / SENSITIVE DATA

Audit:

Authorization header logging
cookies
tokens
passwords
actor ids
tenant ids
seller ids
dev-only headers
exception detail leakage

Record whether any logs exist and whether they are safe.

OBSERVABILITY / CORRELATION

Audit for:

custom ActivitySource
custom Meter
manual traceparent
custom correlation IDs
parallel logging scopes

Target:
no parallel mechanisms.

PATH / NAMESPACE / COHESION

Verify exact namespace ↔ path.

Check:

flat root files justified
nested capability folder names
aliases
shims
TypeForwardedTo
duplicate types
god files
mixed responsibilities
HISTORICAL / SOT CLAIM RECONCILIATION

Inspect Recovery/SoT for historical Security-related claims from:

Storefront AMC R2
Payment Host residue closure
Offer Host residue closure
Seller migrations
Checkout/Order host adapter work

Classify each as:

STILL_CURRENT
SUPERSEDED
STALE_METADATA
NOT_APPLICABLE

Do not let historical "KEEP" claims substitute for current analysis.

TEST / GUARD INVENTORY

Identify all focused guards/tests that protect Security files.

For each:

exact asserted file/member
current usefulness
stale count/allowlist
whether future delete/move would need exact guard update

Do not weaken guards.

DECISIVE MIGRATION PLAN

Produce the FEWEST safe waves <=20 minutes each.

Do not arbitrarily group by file count.

Expected possible shape only if evidence supports it:

W1 — Global/core Security hygiene

root global files + shared seller access core
canonical error/localization cleanup if common

W2 — Seller authorizer family

dead adapters removal + module-specific code/localization hygiene

W3 — Checkout/Payment adapters

only if actual debt exists

W4 — Security certification

But choose a different plan if evidence shows:

files are already clean and only need cert
dead adapters should be removed first
one subfolder has a material architecture blocker

Every proposed wave must include:

exact files
exact ownership outcome
exact behavior preserved
exact guards
time estimate <=20
PROTECTED HOST/ADMIN

Explicitly verify Analysis does not change or reopen:
src/backend/Host/Tooba.Host/Admin/

Host/Admin must stay:
HOST_ADMIN_FULLY_CERTIFIED

PRODUCTION CHANGE RULE

Production code change:
ZERO

If an urgent defect is discovered:
document only.
Do not fix.

FOCUSED ANALYSIS VALIDATION

No solution-wide tests.
No production build unless needed to resolve an ambiguity.

Repository inspection + existing focused tests/guards are sufficient.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-SECURITY-AMC-001/

Required:

analyze.md
file-dispositions.md
consumers-di.md
dependency-boundary.md
authorization-semantics.md
error-localization.md
security-observability.md
historical-claims.md
guard-impact.md
migration-plan.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-SECURITY-AMC-001.task.md

RECOVERY / SOT

Analysis-only.

Do NOT advance latest implementation SHA.

Record:

currentHostCheckpoint = Security
mode = ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
securityProductionFileCount = actual count
subfolders = actual set
disposition summary
recommended wave count
recommended next task
Host/Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_SECURITY_AMC_001
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Latest accepted implementation lineage remains:
7a0d79b407c618d503acd1ef9866c519b4e5f070

GIT

Work from latest origin/main.

No reset.
No clean.
No rebase.
No force push.

Docs-only task/evidence/SoT stamp allowed.
No production code change.

Preserve unrelated user work.

SUCCESS CRITERIA

PASS only if:

exact Security tree enumerated
all production files dispositioned
consumer/DI mapping complete
root platform security ownership decided
Checkout ownership decided
Payment ownership decided
Seller family completely audited
every stable error code/localization path classified
hard-coded runtime text matrix complete
message-classification scan complete
foreign dependency matrix complete
business authority audit complete
fail-closed semantics documented
sensitive logging/observability audited
historical claims reconciled
guard impact exact
fewest safe migration waves proposed
Host/Admin certification preserved
production code change ZERO
implementation SHA unchanged
automatic next NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SECURITY-AMC-001
Parent-Task: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Mode-State:
Active-Host-Folder:
Initial-Production-File-Count:
Final-Production-File-Count:
Folder-Enumeration-State:
Subfolder-State:
File-Disposition-State:
Global-Security-State:
Checkout-Security-State:
Payment-Security-State:
Seller-Security-State:
Production-Consumer-Audit-State:
DI-Registration-Audit-State:
Active-Adapter-Count:
Dead-Adapter-Count:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Cross-Module-Persistence-State:
Module-Business-Authority-State:
Hardcoded-Runtime-User-Facing-Text-State:
Hardcoded-Title-Site-Count:
Exception-Message-Classification-State:
Error-Code-Matrix-State:
Descriptor-Ownership-State:
Localization-State:
Authorization-FailClosed-State:
Sensitive-Logging-State:
Observability-State:
Path-Namespace-State:
Cohesion-State:
Historical-Claims-State:
Guard-Impact-State:
Host-Admin-Certification-Protection-State:
Recommended-Wave-Count:
Recommended-Next-Task:
Production-Code-Change-State:
Recovery-State:
Last-Accepted-Implementation-Commit-State:
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

Do not start Security W1.
Do not start another Host folder.
Do not reopen Host/Admin.

Wait for Architect/user review.

END_TOOBA_TASK