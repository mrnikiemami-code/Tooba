PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-WISHLIST-AMC-001-W4-R1
Parent-Task: TB-TMAR-WISHLIST-AMC-001-W4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Wishlist
Mode: CERTIFICATION_DEFECT_REPAIR
Track: WISHLIST_AMSC_CERT_REPAIR
Title: Close remaining semantic-fault and canonical-evidence defects after W4 certification claim

ARCHITECT VERDICT

W4 is NOT architect-accepted yet.

Structure is strong and the core AMSC migration is substantially correct, but independent review of current main found two production semantic-fault defects and one canonical-evidence inaccuracy that conflict with the Certify skill.

CURRENT VERIFIED MAIN

Expected starting HEAD:
f486ca46111b16f4f0d651eabc94004e9a617a41

W0-W4 lineage verified:

W0 14d129a4 — Analyze
W1 5a7c9f91 — /Modules/Wishlist/ solution grouping
W2 a7ca84e3 — capability-first Domain/Application/Infrastructure/Contracts
W3 044a8145 — Result pipeline + Contracts error catalog
W4 f486ca46 — Structure + Certify claim

Verified good and must be preserved:

/Modules/Wishlist/ solution group with 5 projects
Structure-State READY_FOR_CERTIFY
capability-first shallow Application structure
Path-Namespace EXACT
Physical-Copy CLEAN
root allowlists enforced
module-owned endpoints
ISender + ApiResponseFactory in customer endpoints
4 endpoint-reachable requests with 3 validators + 1 explicit no-validator case
foreign Application/Infrastructure/Domain coupling ZERO
Catalog communication through Catalog.Contracts
Host Wishlist business/endpoint/persistence residue ZERO
schema/migrations unchanged by AMC
frontend untouched

INDEPENDENTLY VERIFIED BLOCKERS

Domain expected-failure prose exception remains:

src/backend/Modules/Wishlist/Tooba.Wishlist.Domain/Aggregates/WishlistItem.cs

Current:
throw new InvalidOperationException("شناسهٔ مالک و محصول الزامی است.");

This violates the canonical semantic-failure direction for a certified touched business surface. Expected business/input faults must use stable machine codes / SemanticException rather than prose InvalidOperationException.

Infrastructure directory expected-failure prose exception remains:

src/backend/Modules/Wishlist/Tooba.Wishlist.Infrastructure/Directories/WishlistDirectory.cs

Current:
if (actorUserId == Guid.Empty) throw new InvalidOperationException("Actor معتبر الزامی است.");

This is an expected semantic rejection and must not remain prose-based.

Canonical evidence overstates the Order.Contracts boundary as Development-seed-only.

Current W4 evidence / SoT / manifest wording says Order.Contracts.Fulfillment is only a Development seed seam.

But production endpoint code also references:
src/backend/Modules/Wishlist/Tooba.Wishlist.Endpoints/Customer/WishlistCustomerActorResolver.cs

using Tooba.Order.Contracts.Fulfillment;
StorefrontGuestActor.ActorId

This is still Contracts-only and is NOT itself a foreign App/Infra/Domain violation, but the canonical boundary description is inaccurate and must be corrected honestly.

SCOPE

Bounded repair only.

Allowed production files:

src/backend/Modules/Wishlist/Tooba.Wishlist.Domain/Aggregates/WishlistItem.cs
src/backend/Modules/Wishlist/Tooba.Wishlist.Domain/Tooba.Wishlist.Domain.csproj only if same-module Wishlist.Contracts reference is required for canonical stable codes
src/backend/Modules/Wishlist/Tooba.Wishlist.Infrastructure/Directories/WishlistDirectory.cs
src/backend/Modules/Wishlist/Tooba.Wishlist.Contracts/Errors/WishlistErrorCodes.cs only if an additional module-owned stable code is genuinely required
src/backend/Modules/Wishlist/Tooba.Wishlist.Contracts/Errors/WishlistErrorCatalogContributor.cs only if descriptor ownership must change
Wishlist error resources only if a new user-facing module-owned code is introduced

Allowed guard/evidence/SoT files:

focused Wishlist W3/W4 architecture guard(s)
docs/architecture/evidence/TB-TMAR-WISHLIST-AMC-001-W4/*
docs/architecture/evidence/TB-TMAR-WISHLIST-AMC-001-W4-R1/*
docs/architecture/tmar-current-state.json
docs/architecture/tmar-module-structure-manifests.json certificationNote/boundary wording only if needed; do not alter structural allowlists
exact task artifact

Forbidden:

unrelated Wishlist refactor
new CQRS requests
route changes
API response shape changes
schema/migrations
Host production
other modules' production code
frontend
broad test rewrites
weakening existing guards/baselines
creating compatibility aliases/shims
new shared error layer
changing business behavior beyond replacing the two expected prose faults with canonical stable semantic faults

REQUIRED REPAIR

A. Replace both expected prose InvalidOperationException paths with canonical SemanticException/SemanticError using stable codes.

Preferred behavior-preserving mapping:

empty actor -> existing shared session-required semantics (customer.session.required) through the existing canonical WishlistErrorCodes alias; do not register a duplicate descriptor
empty product id -> existing Wishlist product-id-required code where semantically exact

If the Domain needs Wishlist-owned stable constants, a same-module Domain -> Wishlist.Contracts reference is acceptable if it introduces no cycle and matches repository rules. Do not hard-code machine-code strings in Domain if a canonical constant already exists.

Do not catch/map unexpected exceptions.

B. Strengthen the focused Wishlist architecture guard so this exact regression cannot return.

The guard must be scoped and semantic:

detect prose InvalidOperationException expected-failure paths in Wishlist Domain aggregate / Infrastructure directory
do not globally forbid every InvalidOperationException in unrelated/internal plumbing
preserve current W3/W4 assertions

C. Correct canonical boundary wording.

Record honestly that:

production cross-module boundary remains Contracts-only
Catalog.Contracts is consumed by Application/Infrastructure
Order.Contracts.Fulfillment is consumed by the production Customer actor resolver AND Development seed
foreign Application/Infrastructure/Domain coupling remains ZERO

Update W4 evidence, SoT boundary field, and manifest certification note if they currently state "Development seed only".

Do not redesign the actor seam in this repair. This task only makes the current legal Contracts dependency accurately recorded.

D. Preserve structure certification.

No folder move is required unless the repair itself creates a concrete Structure violation.
Expected structural state after repair remains:

PROFESSIONAL_SHALLOW
CANONICAL solution grouping
PATH_NAMESPACE EXACT
PHYSICAL_COPY CLEAN
ROOT_ALLOWLIST ENFORCED

VALIDATION

Run only focused validation required for this bounded repair.

Required:

build Wishlist.Contracts if changed
build Wishlist.Domain
build Wishlist.Application only if transitive reference impact requires it
build Wishlist.Infrastructure
build Wishlist.Endpoints only if dependency graph changed
run focused Wishlist W3/W4 architecture guards
run the smallest existing Wishlist behavior/foundation tests needed to prove add/remove/list/membership behavior parity
JSON parse for SoT/manifest if changed
search proof:
zero prose InvalidOperationException expected-failure path in WishlistItem.Create
zero prose InvalidOperationException expected-failure path in WishlistDirectory.EnsureActor
zero foreign Application/Infrastructure/Domain project references
canonical evidence no longer claims Order.Contracts.Fulfillment is Development-seed-only

TEST DISCIPLINE

Tests are evidence, not navigation.

If one focused failure has one clear deterministic local cause, perform one bounded repair and rerun only the affected validation.
If the failure persists or requires speculation/broader work, STOP with INCOMPLETE.
Do not run the full repository suite.
Do not weaken guards.
Do not broaden into another module.

EVIDENCE

Create:
docs/architecture/evidence/TB-TMAR-WISHLIST-AMC-001-W4-R1/

At minimum:

semantic-fault-repair.md
boundary-evidence-correction.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-WISHLIST-AMC-001-W4-R1.task.md

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-WISHLIST-AMC-001-W4-R1
Parent-Task: TB-TMAR-WISHLIST-AMC-001-W4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: F486CA46 | DIVERGED
WishlistItem-Semantic-Fault-State: CANONICAL | INVALID
WishlistDirectory-Actor-Fault-State: CANONICAL | INVALID
Stable-Code-State: CANONICAL_NO_DUPLICATE_DESCRIPTOR | INVALID
OrderContracts-Boundary-Evidence-State: ACCURATE_PRODUCTION_PLUS_DEVELOPMENT | STALE
Foreign-App-Infra-Domain-Coupling-State: ZERO | VIOLATION
Structure-State: READY_FOR_CERTIFY_PRESERVED | REGRESSION
Path-Namespace-State: EXACT | MISMATCH
Schema-Change-State: NONE | CHANGED
Host-Final-Closure-State: PRESERVED | REGRESSION
Frontend-State: UNTOUCHED | CHANGED
Focused-Guard-State: PASS | FAIL
Focused-Behavior-State: PASS | FAIL
Recovery-SoT-State: UPDATED | STALE | CONFLICT
Implementation-Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | <state>
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_WISHLIST_AMC_001_W4_R1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not start PlatformProbe.
Do not start another Wishlist wave.
Do not start another module.
Wait for Architect review.

END_TOOBA_TASK