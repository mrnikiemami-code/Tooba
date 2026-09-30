PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-GRID-AMC-001-R5-R1
Parent-Task: TB-TMAR-HOST-GRID-AMC-001-R5
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Grid AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: GRID_R5_REPAIR
Title: Remove Reviews→Catalog.Application leakage and reconcile Grid recovery

BASELINE

Grid implementation commit: d5f60f95c386515a55f4f778eb44aa42429e963c
SoT conflict-marker cleanup commit: 5982e63eb553f3f613d1bec8c6a8b1432afa766f
Host/Grid is physically ABSENT / HOST_ZERO.
Architect review found one real Contracts-only blocker:
Tooba.Reviews.Infrastructure.Grid.AdminReviewGridQueryEngine
imports and consumes Tooba.Catalog.Application.ICatalogLookupGateway.

ARCHITECTURE LOCK
Foreign module access from Reviews must be Contracts-only.
No Reviews -> Catalog.Application / Domain / Infrastructure / Persistence dependency.

SCOPE — REPAIR ONLY

Replace the remaining Catalog.Application dependency used only for product-title enrichment in the Reviews admin grid.
Reuse/extend a narrow Catalog.Contracts read seam for batch product-title lookup by product IDs; existing title filter/search ID resolution may stay on the existing Catalog.Contracts seam.
Implement the new/extended Catalog.Contracts seam inside Catalog.Infrastructure.
AdminReviewGridQueryEngine must consume Catalog.Contracts only.
Preserve pending-only/search/product-title filter/paging/sort/page-title enrichment/DTO/error behavior.
Do NOT move anything back to Host.
Host/Grid remains ABSENT.
No schema/frontend change.

FOCUSED AUDIT
Verify Grid R2/R4 destinations do not introduce foreign Application/Domain/Infrastructure/Persistence dependencies:

Story grid
Party sellers grid
Do not broaden into unrelated refactors. If clean, record clean. If a directly attributable boundary violation exists, repair only that violation in this same task.

RECOVERY / SOT
Current SoT is split-brain:

nextTask/workflowStop reference Grid
lastAcceptedTask/currentHostEvacuation still reference Storefront.

On PASS reconcile all authoritative surfaces to repaired Grid closure:

lastAcceptedTask = TB-TMAR-HOST-GRID-AMC-001-R5-R1
lastAcceptedCommit = actual repair implementation commit
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentHostEvacuation.activeModule = Grid
currentHostCheckpoint = Grid
currentTask/latestAcceptedImplementationWave = R5-R1
active state = GRID_CLOSED_HOST_ZERO_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_GRID_AMC_001_R5_R1_CLOSED_HOST_ZERO
nextTask = same user-review marker
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
Storefront R4 remains accepted historical lineage
Host/Grid = ABSENT / CLOSED_HOST_ZERO

Reconcile:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

No conflict markers. No placeholder commit markers.

FOCUSED VALIDATION ONLY
Build Catalog.Contracts, Catalog.Infrastructure, Reviews.Infrastructure, Host, directly affected focused test projects.
Run HostGridAmcR3GuardTests, HostGridAmcR5GuardTests, Reviews grid tests, Reviews foreign-boundary guard, TmarDurableGuardTests.
No solution-wide tests.

SUCCESS
PASS only if:

Reviews -> Catalog.Application = ZERO
Reviews -> Catalog.Domain/Infrastructure/Persistence = ZERO
Catalog.Contracts is the only Catalog boundary used by Reviews grid
Host/Grid remains ABSENT
Story/Party Grid destinations have no new forbidden foreign-layer leakage
behavior parity preserved
Recovery fully points to Grid R5-R1
stale pointer ZERO
automatic next NONE

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Commit and push main only on PASS.

CANONICAL RESULT
Return ONLY BRIDGE-WAKE-V1 Result with required fields.

STOP COMPLETELY AFTER RESULT.
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK
