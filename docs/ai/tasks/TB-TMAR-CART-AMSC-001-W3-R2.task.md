PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CART-AMSC-001-W3-R2
Parent-Task: TB-TMAR-CART-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: BOUNDED_EXPECTED_FAILURE_CATALOG_REPAIR
Title: Close reachable cart.line.currency_missing catalog gap

ARCHITECT VERDICT
Cart architecture/structure and W3-R1 are accepted. Do NOT reopen them.

STARTING HEAD
b5721b0f10c9f79319f329d7b97df9153f02eee9

DEFECT
Current disk truth:

CartErrorCodes declares cart.line.currency_missing.
It is localized and thrown/reachable fail-closed.
CartErrorCatalogContributor has 25 descriptors and does not register this code.
checkout.authentication_required is Foundation-owned and consumed-not-registered by Cart.
Therefore the expected LineCurrencyMissing fault can fall back to platform.unexpected.

GOAL
Register exactly one correct descriptor for cart.line.currency_missing and reconcile focused certification truth. Nothing else.

PRECHECK
Verify HEAD==origin/main. Re-discover the code value, all production throw/use sites, both resx keys, contributor count, SafeErrorMapper fallback, and repository semantic convention for the correct classification/status. If materially different: RECOVERY_CONFLICT + STOP.

ALLOWED PRODUCTION FILE — EXACTLY ONE
src/backend/Modules/Cart/Tooba.Cart.Endpoints/Errors/CartErrorCatalogContributor.cs

OTHER ALLOWED

existing focused Cart tests/guards needed to lock this exact repair
docs/architecture/tmar-current-state.json
Cart manifest certificationNote wording/count ONLY if stale
docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W3-R2/*
task/result artifacts required by repo convention

FORBIDDEN
Any other production file; error-code value changes; throw-site changes; resx changes; routes; DTOs; handlers/domain/infrastructure; schema/migrations; manifest structural fields; Host production; unrelated cleanup/drift; .tmp-baseline; guard weakening; baseline widening; full solution tests; W4.

IMPLEMENTATION

Add exactly ONE ErrorDescriptor for CartErrorCodes.LineCurrencyMissing.
Select ErrorClassification + HTTP status from semantic/repository evidence; do not guess silently.
LocalizationKey must be the existing stable code.
Severity/fallback must follow existing Cart expected-failure convention.
Do not register checkout.authentication_required in Cart.
Do not create/change any code or resource.

EXPECTED COUNT TRUTH AFTER EDIT

declaredOrConsumedCodeCount = 27
Cart registered/owned descriptors = 26
checkout.authentication_required = Foundation-owned, consumed-not-registered
duplicate descriptor ownership = ZERO
unregistered reachable Cart-owned codes = ZERO
Re-discover and prove these values.

SOT / CERTIFICATION
Close the previous LineCurrencyMissing residual risk.
Preserve COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002.
Preserve W3-R1 bounded behavior truth; extend its status set only if the newly proven descriptor mapping requires it.
Record this R2 as a bounded expected-failure repair, NOT behaviorChange=NONE.
Set minimal metadata:
certificationReconciliation=W3_R2_LINE_CURRENCY_CATALOG_CLOSED
evidenceW3R2=docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W3-R2/
workflowStop=USER_REVIEW_CART_AMSC_001_W3_R2
automaticNextImplementationTask=NONE

MANIFEST
Do not change structure. Only update Cart certificationNote if descriptor-count/behavior wording becomes stale.
Never change projects/rootAllowlist/forbiddenRootFiles/forbiddenTopLevelFolders/structureCertified/lockVersion.

DURABLE GUARD
Extend an existing focused Cart guard/test only as needed to prove:

LineCurrencyMissing descriptor exactly once
accepted classification/status
26 unique Cart descriptors
shared checkout code not registered by Cart
duplicate ownership ZERO
SoT no longer calls LineCurrencyMissing unregistered
No tautological assertions.

EVIDENCE
Create docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W3-R2/
with at least:

line-currency-catalog-repair.md
validation.md
Record before/after, classification/status rationale, 27/26/shared counts, zero duplicate ownership, exactly one bounded production-file delta, and zero route/DTO/code-value/resx/schema/migration/manifest-structure change.

BOUNDED VALIDATION
Run only:

focused Cart presentation/error-catalog tests
CartModuleAmsc001W3CertGuardTests
ErrorCatalogUniqueCodeGuardTests if needed
directly affected build if needed
JSON parse
git diff proof
No full suite. No unrelated repair.
One implementation pass + one validation pass; at most ONE direct correction for an R2-caused failure.

COMMIT/PUSH
If PASS: one R2 commit, push origin/main, verify HEAD==origin/main, preserve unrelated untracked artifacts.

EXPECTED FINAL
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
MANIFEST_DISK_EXACT
CONTRACTS_ONLY
FOREIGN_APP_INFRA_DOMAIN_COUPLING_ZERO
MICROSERVICE_EXTRACTABLE
CART_EXPECTED_FAILURE_CATALOG_COMPLETE
LINE_CURRENCY_MISSING_DESCRIPTOR_REGISTERED_EXACTLY_ONCE
DUPLICATE_DESCRIPTOR_OWNERSHIP_ZERO

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-CART-AMSC-001-W3-R2
Parent-Task: TB-TMAR-CART-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: B5721B0F | DIVERGED
Production-Scope-State: EXACT_ONE_ALLOWED_CART_CONTRIBUTOR | VIOLATION
Line-Currency-Reachability-State: VERIFIED | CONFLICT
Line-Currency-Descriptor-Before-State: ABSENT | CONFLICT
Line-Currency-Descriptor-After-State: EXACTLY_ONE | MISSING | DUPLICATE
Line-Currency-Classification-State: <classification>
Line-Currency-Http-Status-State: <status>
Declared-Or-Consumed-Code-Count-State: TWENTY_SEVEN | CONFLICT
Cart-Owned-Descriptor-Count-State: TWENTY_SIX | CONFLICT
Shared-Checkout-Authentication-State: FOUNDATION_OWNED_CONSUMED_NOT_REGISTERED | CONFLICT
Unregistered-Reachable-Cart-Code-State: ZERO | NONZERO
Duplicate-Descriptor-Ownership-State: ZERO | CONFLICT
Routes-State: UNCHANGED | REGRESSED
Dto-Shape-State: UNCHANGED | REGRESSED
Error-Code-Value-State: UNCHANGED | REGRESSED
Resource-State: UNCHANGED | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Manifest-Structural-State: UNCHANGED | REGRESSED
Cross-Module-Boundary-State: CONTRACTS_ONLY | REGRESSED
Foreign-App-Infra-Domain-Coupling-State: ZERO | REGRESSED
Focused-Cart-Guard-State: PASS | FAIL
Error-Catalog-Unique-Guard-State: PASS | NOT_REQUIRED | FAIL
Focused-Build-State: PASS | NOT_REQUIRED | FAIL
Evidence-State: COMPLETE | INCOMPLETE
Recovery-SoT-State: RECONCILED | STALE | CONFLICT
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_CART_AMSC_001_W3_R2
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP completely after result. No W4/R3. No unrelated repair. Wait for Architect review.

END_TOOBA_TASK
