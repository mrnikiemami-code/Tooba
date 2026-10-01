PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-REVIEWS-AMC-001-R1
Parent-Task: TB-TMAR-HOST-REVIEWS-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Reviews AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: REVIEWS_POST_MIGRATION_REPAIR
Title: Remove Reviews message-text failure classification and finalize HOST_ZERO certification

ACCEPTED BASELINE

Reviews implementation commit:
b30190cd5e806785dcff2e6a0ab7f3673baa1045
Reviews docs/SoT stamp:
887168cf38d9bb8b07eebbc10bd620c5633dd047
Host/Reviews is ABSENT / CLOSED_HOST_ZERO.
HTTP ownership is Tooba.Reviews.Endpoints.
CQRS/MediatR is in place.
Offer/Catalog cross-module access is Contracts-only.
Do NOT reopen Host/Reviews.

ARCHITECT BLOCKER
ReviewsFailureMapper still classifies expected failures using exception message text:

exception.Message.Contains("قبلاً", StringComparison.Ordinal)

This violates:

NO message-text classification
typed/code-based expected failure transport
canonical SemanticError/ApiResponseFactory error flow

REPAIR REQUIREMENTS

REMOVE MESSAGE-TEXT CLASSIFICATION
Remove all ex.Message / Contains(...) / localized-text inspection used to determine Reviews error codes.
Message-text classification in Reviews production code must become ZERO.
MOVE EXPECTED FAILURE AUTHORITY TO THE OWNER
Expected Reviews failures must originate as stable typed/code-based failures at the appropriate Application/Domain/Directory boundary.

Preserve these public semantic codes:

reviews.duplicate
reviews.rejected
reviews.moderation.rejected

Use existing canonical mechanisms:

SemanticException + SemanticError
OR
a typed Reviews-owned exception/fault carrying a stable code, converted once at the Application boundary.

Do NOT:

create parallel error mappers based on strings
classify unknown InvalidOperationException as a known business code
swallow unexpected exceptions

Unknown exceptions must propagate.

PRESERVE ENDPOINT PRESENTATION
Reviews.Endpoints continues to use ApiResponseFactory.
No raw parallel error pipeline.
Endpoints -> Domain remains ZERO.
Endpoints -> Host remains ZERO.
ISender remains the dispatch path.
PRESERVE BEHAVIOR
Preserve:
duplicate review behavior/code
generic review rejection behavior/code
moderation rejection behavior/code
existing HTTP status mapping for these semantic codes
storefront/customer/seller/admin routes and DTOs
Offer seller-product scoping
Catalog title enrichment
paging/grid behavior
DURABLE GUARDS
Strengthen/update focused guards to prove:
Host/Reviews ABSENT
Reviews production message-text classification = ZERO
no exception.Message inspection for Reviews error-code selection
Endpoints -> Domain = ZERO
Endpoints -> Host = ZERO
Offer.Application leakage = ZERO
Catalog.Application leakage = ZERO
ApiResponseFactory canonical presentation remains in use
unknown exceptions are not remapped to known Reviews semantic codes

RECOVERY / SOT
On PASS reconcile all authoritative surfaces to Reviews R1:

lastAcceptedTask = TB-TMAR-HOST-REVIEWS-AMC-001-R1
lastAcceptedCommit = actual R1 implementation commit
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-REVIEWS-AMC-001-R1
currentHostEvacuation.activeModule = Reviews
currentHostCheckpoint = Reviews
currentTask = TB-TMAR-HOST-REVIEWS-AMC-001-R1
activeModuleState = REVIEWS_CLOSED_HOST_ZERO_R1_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_REVIEWS_AMC_001_R1_CLOSED_HOST_ZERO
nextTask = same user-review marker
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
parent Reviews AMC remains accepted lineage
Security R1 / Story R1 / Wishlist / Grid remain historical accepted lineage

Reconcile:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

No placeholders.
No conflict markers.

SCOPE LIMIT
Do NOT:

reopen Host/Reviews
perform full Reviews ARCH-COMPLETE-002 restructuring
move unrelated Reviews files/folders
redesign Review domain behavior
touch frontend
change schema/migrations
start another Host folder
run solution-wide refactors
create aliases/shims

FOCUSED VALIDATION ONLY

Build:

Reviews.Application
Reviews.Infrastructure
Reviews.Endpoints
Host
directly affected focused tests

Run:

HostReviewsAmcGuardTests
ReviewsFoundationTests
AdminDbNativeGridQueryTests if directly affected
focused duplicate/rejection/moderation tests
focused error-presentation tests
TmarDurableGuardTests

No solution-wide test run.

SUCCESS
PASS only if all are true:

Host/Reviews remains ABSENT
Reviews HOST_ZERO preserved
message-text classification = ZERO
expected failures are typed/code-based
duplicate/rejected/moderation codes preserved
unknown exceptions propagate
Endpoints -> Domain = ZERO
Endpoints -> Host = ZERO
Offer.Application leakage = ZERO
Catalog.Application leakage = ZERO
ApiResponseFactory remains canonical presentation
behavior parity preserved
schema unchanged
frontend unchanged
Recovery fully points to Reviews R1
staleCurrentPointerState = ZERO
automaticNextImplementationTask = NONE

EVIDENCE
Create/update:
docs/evidence/TB-TMAR-HOST-REVIEWS-AMC-001-R1/

repair.md
failure-semantics.md
endpoint-boundary.md
behavior-parity.md
recovery.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-REVIEWS-AMC-001-R1.task.md

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Preserve all user work.
Commit and push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-REVIEWS-AMC-001-R1
Parent-Task: TB-TMAR-HOST-REVIEWS-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Host-Reviews-State:
Reviews-Host-Zero-State:
Message-Text-Classification-State:
Expected-Failure-Transport-State:
Duplicate-Code-State:
Rejected-Code-State:
Moderation-Rejected-Code-State:
Unknown-Exception-Propagation-State:
Endpoints-Domain-Reference-State:
Endpoints-Host-Reference-State:
Offer-Application-Leakage-State:
Catalog-Application-Leakage-State:
Api-Error-Presentation-State:
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