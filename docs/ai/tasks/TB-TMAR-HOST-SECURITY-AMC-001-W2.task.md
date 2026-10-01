PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SECURITY-AMC-001-W2
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001-W1-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_SECURITY_STRUCTURE_GUARD_SOT_RECONCILIATION
Title: Reconcile Host/Security exact 19-file guard and stale historical Security certification metadata
Estimated-Time-Minutes: 10
Hard-Timebox-Minutes: 14

ARCHITECT REVIEW STATE

W1-R1:
ACCEPTED

Architect independently verified:

W1 implementation SHA remains:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b
W1 behavior accepted
W1-R1 production change = ZERO
top-level Recovery split semantics are now self-consistent
result/SoT stamp authority points to R1 docs reconciliation commit
Host/Admin remains HOST_ADMIN_FULLY_CERTIFIED
Security structure reconciliation remains intentionally deferred to W2

CURRENT KNOWN STRUCTURE TRUTH

Host/Security production .cs count:
19 exact

Top-level files:

AuthSecurityHostOptions.cs
SecurityHeadersMiddleware.cs

Checkout:

CheckoutIdentityGate.cs
HostCheckoutActorPolicyAdapter.cs

Payment:

HostPaymentStorefrontAuthorizer.cs

Seller:

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

KNOWN STALE GUARD

src/backend/Host/Tooba.Host.Tests/Architecture/HostSecurityAmcGuardTests.cs

Currently:

ExpectedSellerFiles omits HostReviewsSellerAuthorizer.cs
exact total count still asserts 18
class summary still describes old Security AMC/R1 certification lineage
SoT assertion still depends on old historical KEEP/CERT wording

This W2 reconciles structural truth only.
No production behavior change.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

Use it for bounded test/SoT migration discipline.

DO NOT certify Security in W2.
DO NOT touch Security production .cs files.
DO NOT start another Host folder.
DO NOT reopen Host/Admin.

IN-SCOPE

Tests/guards:

src/backend/Host/Tooba.Host.Tests/Architecture/HostSecurityAmcGuardTests.cs
TmarDurableGuardTests only if current Recovery assertions require exact W2 pointer changes

SoT/current docs:

docs/architecture/tmar-current-state.json
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md if it carries current Security state
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md if it carries current Security state

Task/evidence docs.

OUT OF SCOPE

all src/backend/Host/Tooba.Host/Security/**/*.cs
all module production code
all Host/Admin production/tests except durable current-pointer guard if necessary
any localization/error redesign
certification
frontend/schema

REQUIRED CHANGES

HOSTSECURITY EXACT FILE GUARD

Update ExpectedSellerFiles to include:

HostReviewsSellerAuthorizer.cs

Expected Seller file count becomes:
14 exact

Expected whole Host/Security recursive production count becomes:
19 exact

Guard must continue to assert exact membership.
Do NOT replace with:

=

Contains-only without equality
optional file checks
wildcard file acceptance
EXACT SUBFOLDER/TREE TRUTH

Strengthen or preserve guard so current tree truth is explicit:

Top-level root files:
2 exact

Subfolders:

Checkout
Payment
Seller

Checkout files:
2 exact

Payment files:
1 exact

Seller files:
14 exact

Whole Security:
19 exact

No unexpected .cs file may silently enter.

If current guard cannot express exact folder membership, add bounded exact assertions without broad rewrite.

PATH ↔ NAMESPACE

Preserve current exact namespace checks:

Root:
Tooba.Host.Security

Checkout:
Tooba.Host.Security.Checkout

Payment:
Tooba.Host.Security.Payment

Seller:
Tooba.Host.Security.Seller

No alias/shim exception.

PRESERVE W1 SEMANTIC HYGIENE IN GUARD

Guard should now lock current W1 facts where appropriate:

SellerPanelAccess uses SemanticException
no PlatformHttpException in SellerPanelAccess
HostPartySellerAuthorizer denial uses SemanticException
HostSupportSellerAuthorizer denial uses SemanticException
HostOrderSellerAuthorizer catches SemanticException
no ex.Message / exception.Message classification
Host/Security runtime hard-coded exception titles = ZERO

Do not duplicate a giant certification guard; bounded structural/hygiene assertions are enough.

HISTORICAL SECURITY SOT METADATA

Inspect historical blocks such as:

hostSecurityAmc
hostSecurityAmcR1
any old Security certify evidence pointers/comments

Do NOT delete historical lineage.

Reclassify stale historical claims explicitly so they cannot be mistaken as current certification authority.

Required current meaning:

old 18-file Security certification = HISTORICAL / SUPERSEDED / STALE_METADATA
current live Security tree = 19
current W1 hygiene implementation = accepted
current certification = NOT_YET_REASSERTED
next phase = W3 CERT only after Architect review

If historical blocks contain a numeric production file count 18, either:
A. retain as HISTORICAL_SNAPSHOT_18 with explicit historical label, or
B. add current live count field 19 alongside it

Do NOT falsify history by rewriting an old historical snapshot as if it had been 19 at the time unless repo history proves that.

CURRENT SECURITY SOT BLOCK

Add/update a W2 block, recommended:

hostSecurityAmc001W2

with:

taskId
parentTaskId
state = STRUCTURE_GUARD_SOT_RECONCILED
securityProductionFileCount = 19
sellerProductionFileCount = 14
hostReviewsSellerAuthorizer = PRESENT_ACTIVE_DI
hardcodedRuntimeText = ZERO_W1_PRESERVED
deadAdapterCount = 0
productionCodeChange = NONE
implementationCommit = baa05e6b...
certificationState = NOT_YET_REASSERTED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_SECURITY_AMC_001_W2
TOP-LEVEL RECOVERY POINTER

This is docs/tests only.

On PASS:

lastAcceptedTask:
TB-TMAR-HOST-SECURITY-AMC-001-W2

lastAcceptedCommit:
MUST remain W1 implementation SHA:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b

lastAcceptedCommitKind:
IMPLEMENTATION_COMMIT

latestAcceptedImplementationWave:
TB-TMAR-HOST-SECURITY-AMC-001-W1

currentHostCheckpoint:
Security

workflowStop:
USER_REVIEW_HOST_SECURITY_AMC_001_W2

nextTask:
USER_REVIEW_HOST_SECURITY_AMC_001_W2

automaticNextImplementationTask:
NONE

staleCurrentPointerState:
ZERO

W2 docs/tests commit must be recorded separately as result evidence/SoT stamp according to canonical split convention.

Do NOT advance implementation SHA to W2 docs/tests commit.

CURRENT AUTHORITY DOCS

Where current-authority sections exist, update only current Security checkpoint wording.

Required current truth:

Security Analyze accepted
W1 implementation accepted
W1-R1 recovery repaired
W2 structural guard/SoT reconciled
Security certification pending W3
automatic next NONE
user review required

Historical sections remain historical.

HOST/ADMIN PROTECTION

Must remain:
HOST_ADMIN_FULLY_CERTIFIED

No Admin files changed except a central Recovery guard if exact current pointer forces it.

NO PRODUCTION CHANGE

Production code change:
ZERO

No .cs under:
src/backend/Host/Tooba.Host/Security/
may change.

No module production files may change.

FOCUSED VALIDATION

Run:

HostSecurityAmcGuardTests
TmarDurableGuardTests current Recovery assertions
focused compile of Host.Tests if required

Verify:

19 exact passes
Reviews file in allowlist
current pointer W2 user-review
W1 implementation SHA preserved
old 18 certification claims cannot act as current authority
W3 not started

No solution-wide tests.

One deterministic bounded correction + one rerun maximum.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-SECURITY-AMC-001-W2/

Required:

structure-reconciliation.md
historical-metadata.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-SECURITY-AMC-001-W2.task.md

GIT

Work from latest origin/main.

No reset.
No clean.
No rebase.
No force push.

Preserve unrelated user work.

Docs/tests only.
Push on PASS.

SUCCESS CRITERIA

PASS only if:

Security production files remain unchanged
exact whole Security count = 19
exact Seller count = 14
HostReviewsSellerAuthorizer explicitly present in guard
guard exact membership not weakened
W1 SemanticException hygiene remains guarded
old 18-file cert claim classified historical/stale, not current
current certification remains NOT_YET_REASSERTED
W1 implementation SHA remains baa05e6b...
W2 docs/tests commit separated from implementation authority
Host/Admin FULLY_CERTIFIED preserved
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_SECURITY_AMC_001_W2
focused tests PASS
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SECURITY-AMC-001-W2
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001-W1-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Reconciliation-State:
Production-Code-Change-State:
Security-Production-File-Count:
Seller-Production-File-Count:
HostReviewsSellerAuthorizer-State:
Exact-Allowlist-State:
Path-Namespace-State:
W1-Semantic-Hygiene-Guard-State:
Historical-18-File-Cert-State:
Current-Security-Certification-State:
Security-W1-Implementation-State:
Security-W1-Implementation-Commit-State:
W2-DocsTests-Stamp-State:
Host-Admin-Certification-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Stale-Current-Pointer-State:
Focused-Test-State:
Recovery-Guard-State:
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
Do not reopen Host/Admin.

Wait for Architect/user review.

END_TOOBA_TASK
