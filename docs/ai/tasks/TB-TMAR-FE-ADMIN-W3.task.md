PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-FE-ADMIN-W3

Parent-Task:
TB-TMAR-FE-ADMIN-W2

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
ISSUED

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Title:
TMAR Frontend Admin Wave 3 — Continue Proven Feature Migration and Push Flat Admin Debt Below Critical Thresholds

Task Type:
IMPLEMENTATION — CONTROLLED FEATURE-BASED MIGRATION

0. Architect Intent

FE-ADMIN-W2 is accepted.

Verified state:

Admin-Migration-Pattern = PROVEN

God-Component-Recovery-Readiness = DEFER

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

admin-api LOC: 1145

admin-api export freeze: 49

admin-screens LOC: 1078

architecture guards green

canonical test discovery active

flat admin/admin-api growth frozen

giant-component decomposition is not yet ready and must remain deferred

Primary objectives:

migrate exactly ONE additional low-risk admin capability using the proven pattern

shrink admin-api capability debt again

shrink admin-screens or equivalent flat aggregator where naturally owned by the selected feature

prefer a slice that moves one or both legacy oversized files materially toward <800 LOC without forcing decomposition

preserve product/UI/API behavior

reassess whether another feature migration or god-component characterization should come next

No Big Bang move.
No visual redesign.
No giant split.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Frontend root:
D:\Users\User\source\repos\SarvNewVer\src\frontend

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/evidence/TB-TMAR-FE-ADMIN-W2/recovery-sot.md

docs/evidence/TB-TMAR-FE-ADMIN-W2/admin-api-shrink.md

docs/evidence/TB-TMAR-FE-ADMIN-W2/flat-admin-shrink.md

docs/evidence/TB-TMAR-FE-ADMIN-W2/pattern-review.md

docs/evidence/TB-TMAR-FE-BASELINE/god-component-plan.md

docs/evidence/TB-TMAR-FE-BASELINE/characterization-plan.md

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD==origin/main

18ca10c9 ancestor

known/clean worktree state

user work preserved

Expected previous accepted tip:
89574d54ac06e161e871e5e78cbda76c0e2c5523

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/recovery-start.md

2. Select Exactly ONE Additional Low-Risk Capability

Use repository evidence to select one capability from remaining admin debt.

Known examples may include:

sellers

customers

receipts

orders

other remaining capability-specific admin areas

Selection priority:

clear capability ownership

meaningful admin-api shrink

meaningful flat-screen/aggregator shrink

existing or easy characterization coverage

low visual/workflow risk

no critical giant dependency

no checkout/payment/order consistency semantics

Prefer sellers/customers/receipts over orders if evidence supports lower risk.

Do NOT select Orders if its workflow is materially coupled to checkout/payment/fulfillment or requires large-state/god-file changes.

Do not choose solely for LOC reduction.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/slice-selection.md

3. Characterization Before Move

Before relocation, confirm or add focused characterization tests.

Cover as applicable:

route entry/composition

screen render

list/query/search/filter/sort/pagination

API request and response shape

create/update/status mutations

permissions/visibility

loading/error/empty states

locale/RTL

navigation/deep links

No broad snapshots.
No fake behavior or workaround code just to pass tests.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/characterization-tests.md

4. Perform One Feature Migration

Move exactly the selected capability to:

src/frontend/features/<capability>/

Use only required folders:

components

api/data

hooks

model/types

public boundary

Rules:

App Router route files stay thin

remove owned implementation from flat admin accumulation

remove capability-specific admin-api code from generic dumping ground

no stale duplicate implementation

no alias swamp

no deep external imports into internals

no circular dependencies

generic transport remains shared

generic design-system primitives remain shared

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/migration.md

5. Shrink admin-api

For selected capability:

move capability-specific request/data access out of generic admin-api

preserve generic request/auth/store/locale propagation

preserve existing error/retry semantics

no new HTTP client abstraction unless strictly necessary

no TanStack Query/SWR introduction

Record exact before/after:

admin-api LOC

admin-api exported capability symbols

CAPABILITY_SPECIFIC_DEBT count if tracked

Target:
shrink only.
No baseline widening.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/admin-api-shrink.md

6. Shrink Flat Admin Screen Debt

Where the selected capability is currently embedded in an oversized shared/flat screen file:

extract only the capability-owned portion

leave unrelated behavior untouched

preserve current public imports/route behavior

Record exact before/after LOC for:

admin-screens or actual relevant flat aggregator

any other touched oversized file

Important target:
If a file can naturally fall below 800 LOC solely through owned-feature extraction, do so.
Do NOT manipulate formatting or split unrelated code just to cross the threshold.

If threshold crossed:

shrink source-size baseline immediately

do not keep stale oversized allowance

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/flat-admin-shrink.md

7. Public Feature Boundary

External consumers must use the feature's deliberate public surface.

Prohibit:

deep imports into feature internals

shared lib/UI → business feature reverse dependency

feature cycles

global catch-all barrels

A temporary compatibility re-export is allowed only if:

required to preserve current route/import compatibility

minimal

documented

marked for removal in a future narrow task

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/feature-boundary.md

8. Architecture Guard Compliance

Keep green:

FE-FOLDER-001

FE-FOLDER-002

FE-SIZE-001

FE-SIZE-002

FE-SEO-001

FE-BOUNDARY-001

FE-BOUNDARY-002

migrated-feature deep-import rules

canonical test-discovery guard

No wildcard suppression.
No baseline widening.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/architecture-guards.md

9. Client / Server Boundary Safety

Admin-only structural migration.

Do NOT:

expand use client unnecessarily

convert server/client components opportunistically

touch storefront SSR/SEO paths

Record:

touched files containing use client before/after

any directive move caused purely by file relocation

proof no client contagion expanded

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/client-boundary.md

10. Source-Size Safety

No new handwritten source >800 LOC.

Existing oversized files:

may shrink

must not grow

baseline must shrink when threshold/LOC improves

Do NOT split a god-component in this task.

If selected slice unexpectedly requires such a split:
abandon the slice and choose a safer capability.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/source-size-compliance.md

11. Test Discovery and Validation

Use canonical discovery.

Required:

selected capability characterization tests

npm run test:architecture

canonical discovered frontend test command

TypeScript check compared with known legacy failures

lint if canonical

build if canonical and reasonably bounded

Do not reintroduce manual enumeration.

Record discovered test file count.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/tests.md

12. Legacy Failure Control

Known pre-existing failures from W2 include:

critical-storefront bg-white

PAGE_SIZES issue

admin-api actor ESM issue

inventory count drift

legacy typecheck failures

First VERIFY these are still pre-existing and not worsened.

Do NOT:

suppress them

rewrite unrelated systems to make this task green

silently add more allowed failures

Return:

exact before signatures

exact after signatures

NEW_FAILURES = 0 required for PASS

If any new failure appears due to this task:
repair within scope or FAIL.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/legacy-failures.md

13. God-Component Recovery Readiness Reassessment

Do NOT split a giant yet.

Reassess:
God-Component-Recovery-Readiness: READY
or
God-Component-Recovery-Readiness: DEFER

READY requires:

candidate ownership understood

characterization coverage sufficient

API/data dependencies isolated

route behavior understood

decomposition seams documented

no visual redesign needed

If READY:
identify exactly ONE candidate and the first extraction seam.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/god-component-readiness.md

14. Flat-Debt Readiness

Return:
Flat-Admin-Recovery-State: CONTINUE_FEATURE_MIGRATION
or
Flat-Admin-Recovery-State: READY_FOR_GODFILE
or
Flat-Admin-Recovery-State: SUFFICIENTLY_STABILIZED

Use evidence:

admin-api remaining capability debt

flat admin LOC

remaining feature ownership ambiguity

source-size thresholds

characterization readiness

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W3/flat-recovery-state.md

15. Next Task Decision

Choose automatically:

A. TB-TMAR-FE-ADMIN-W4
if another low-risk feature migration remains highest value.

B. TB-TMAR-FE-GODFILE-W1
only if God-Component-Recovery-Readiness = READY.

C. TB-TMAR-HOST-W3
if frontend flat structure is sufficiently stabilized and Host debt is higher value.

D. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if frontend can safely pause and checkout design is now higher architectural value.

Do not ask the user.

16. Product / Visual Safety

No visual redesign.
No behavior change.

Do NOT alter:

Shopeiva fidelity

Mega Menu

product rails

sliders

PDP/PLP

URLs

localization copy

API contracts

permission semantics

SEO behavior

17. Recovery State

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if durable rules change

docs/architecture/TOOBA-CAPABILITY-MAP.md only if ownership evidence materially changes

docs/evidence/TB-TMAR-FE-ADMIN-W3/recovery-sot.md

Record:

migrated capability

admin-api before/after

flat file before/after

God-Component-Recovery-Readiness

Flat-Admin-Recovery-State

next task

18. Acceptance Criteria

PASS only if:

exactly one additional capability migrated

admin-api debt shrinks

flat admin debt shrinks or remains neutral for a justified reason

no baseline widening

no god-file split

no product/visual/API behavior change

public feature boundary remains clean

architecture guards pass

canonical discovery remains active

NEW_FAILURES = 0

God-Component readiness reassessed

Flat-Admin-Recovery-State returned

user work preserved

next task selected automatically

canonical Result delivered

Worker stops completely

19. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Slice-Selection
Characterization-Tests
Migration
Admin-Api-Shrink
Flat-Admin-Shrink
Feature-Boundary
Architecture-Guards
Client-Boundary
Source-Size-Compliance
Test-Discovery
Legacy-Failures
Tests
God-Component-Recovery-Readiness
Flat-Admin-Recovery-State
Capability-Map
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Next-Recommended-Task

Expected:
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

After canonical Result through Bridge:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK