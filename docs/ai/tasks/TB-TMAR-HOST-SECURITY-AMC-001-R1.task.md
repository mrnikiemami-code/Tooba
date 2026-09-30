PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SECURITY-AMC-001-R1
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Security AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SECURITY_CERTIFICATION_REPAIR
Title: Close Security certification blockers without changing KEEP_THIN_PLATFORM disposition

ACCEPTED BASELINE

Security implementation commit under review:
468f14c0bca4ba814c6b98bf01cce227643012a9
Docs/SoT tip:
2c2388458917f08e1a85c1db2ae53af081f5f11f
Disposition is ACCEPTED:
Host/Security = KEEP_THIN_PLATFORM_SECURITY_BOUNDARY
Host/Security MUST remain present.
Do NOT attempt HOST_ZERO.
Seller R1A boundary remains canonical.
Existing 18-file Security allowlist is the accepted starting point.

ARCHITECT REVIEW BLOCKERS

HOST SECURITY -> PAYMENT.APPLICATION LEAKAGE
Current:
Host/Security/Checkout/HostCheckoutActorPolicyAdapter.cs
references:
Tooba.Payment.Application.Ports.ICheckoutActorPolicyPort

Required:

Move/expose the checkout actor policy seam through the correct Contracts boundary.
Prefer Payment.Contracts if Payment owns the port.
Repoint Payment consumer/DI and Host adapter.
Host/Security -> Payment.Application = ZERO.
Do not introduce a compatibility shim or alias.
Do not move business authority into Host.
HARDCODED ToobaEdition.SingleStore
Current:
Host/Security/Seller/SellerPanelAccess.cs
builds AuthorizationCallContext with:
Edition = ToobaEdition.SingleStore

Required:

Audit the canonical runtime edition/context source already available in platform foundations.
Use the effective current edition from the canonical runtime context/seam.
Do not infer edition from request text or environment naming.
Do not introduce a new parallel edition source.
Preserve Marketplace and SingleStore semantics.
If the authorization contract genuinely does not require edition differentiation, remove the hardcoded value only if the canonical API supports that safely; otherwise inject/read the correct current edition through the existing platform abstraction.
No hardcoded ToobaEdition.SingleStore anywhere in Seller security authorization flow after repair.
CHECKOUT EXPECTED FAILURE TYPE
Current:
CheckoutIdentityGate.EnsureCheckoutActorAsync
throws:
InvalidOperationException("checkout.authentication_required")

Required:

Replace expected failure transport with stable typed/code-based failure.
No message-text classification.
Preserve code:
checkout.authentication_required
Prefer existing canonical SemanticException / SemanticError path or a Contracts-owned typed fault if that is the established boundary.
Unknown exceptions must still propagate.
No new parallel error pipeline.

CERTIFICATION GUARD HARDENING
Update/add durable guards so Security certification proves:

Host/Security remains exactly the accepted platform boundary.
Seller subtree has ZERO foreign Application / Domain / Infrastructure / Persistence.
Checkout subtree has ZERO foreign Application / Domain / Infrastructure / Persistence references.
Payment storefront authorizer has no service locator.
Review seller auth uses ISellerPanelAccess.
No hardcoded ToobaEdition.SingleStore in Host/Security/Seller.
Checkout expected auth failure is not InvalidOperationException/message-coded.
No message-text classification in Host/Security.
No new Host business authority.
No sink-folder regression.

PATH/NAMESPACE
Audit exact path↔namespace for all 18 Security files.
Repair only real mismatches.
Do not move files unless necessary.
Do not broaden scope.

RECOVERY / SOT
On PASS reconcile all authoritative recovery surfaces to R1:

lastAcceptedTask = TB-TMAR-HOST-SECURITY-AMC-001-R1
lastAcceptedCommit = actual R1 implementation commit
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-SECURITY-AMC-001-R1
currentHostEvacuation.activeModule = Security
currentHostCheckpoint = Security
currentTask = TB-TMAR-HOST-SECURITY-AMC-001-R1
activeModuleState = SECURITY_KEEP_THIN_PLATFORM_CERTIFIED_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_SECURITY_AMC_001_R1_KEEP_THIN_PLATFORM_CERTIFIED
nextTask = same user-review marker
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
parent Security AMC remains accepted lineage
Story R1 / Wishlist / Grid remain historical accepted lineage

Reconcile:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

No placeholders.
No conflict markers.

SCOPE LIMIT
Do NOT:

remove Host/Security
reopen Seller R1A beyond required seam/context change
redesign AccessControl
redesign Authentication
change frontend
change schema/migrations
start another Host folder
run solution-wide refactors
create aliases/shims

FOCUSED VALIDATION ONLY
Build:

affected Contracts project for checkout actor policy seam
Payment.Application if required by repointing
Host
directly affected test projects

Run:

HostSecurityAmcGuardTests
HostSellerAmcR1GuardTests
focused Checkout identity/security tests
focused Payment endpoint/security tests
focused authorization/security tests
TmarDurableGuardTests

No solution-wide test run.

SUCCESS
PASS only if all are true:

Host/Security remains PRESENT
disposition remains KEEP_THIN_PLATFORM_SECURITY_BOUNDARY
accepted 18-file boundary preserved unless a directly necessary move is justified
Host/Security -> Payment.Application = ZERO
Host/Security -> any foreign Application/Domain/Infrastructure/Persistence = ZERO
hardcoded ToobaEdition.SingleStore in Seller security flow = ZERO
effective edition/context comes from canonical runtime platform source
Checkout expected auth failure is typed/code-based
checkout.authentication_required preserved
message-text classification = ZERO
Payment authorizer service locator = ZERO
Review seller auth remains ISellerPanelAccess-based
path↔namespace exact
Recovery fully points to Security R1
staleCurrentPointerState = ZERO
automaticNextImplementationTask = NONE

EVIDENCE
Create/update:
docs/evidence/TB-TMAR-HOST-SECURITY-AMC-001-R1/

repair.md
contracts-boundary.md
edition-context.md
checkout-failure.md
security-allowlist.md
recovery.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SECURITY-AMC-001-R1.task.md

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Preserve all user work.
Commit and push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SECURITY-AMC-001-R1
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Security-Disposition-State:
Host-Security-File-Count:
Host-Security-Foreign-Layer-State:
Checkout-Payment-Application-Leakage-State:
Checkout-Actor-Policy-Contract-State:
Seller-Edition-Hardcode-State:
Seller-Edition-Context-State:
Checkout-Expected-Failure-State:
Message-Text-Classification-State:
Payment-Service-Locator-State:
Review-Seller-Access-State:
Path-Namespace-State:
Behavior-Parity-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
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
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK