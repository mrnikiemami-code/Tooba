PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-FE-ADMIN-W4

Parent-Task:
TB-TMAR-FE-ADMIN-W3

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
TMAR Frontend Admin Wave 4 — Continue Feature Migration, Drive Flat Admin Toward <800 LOC, and Reassess God-Component Readiness

Task Type:
IMPLEMENTATION — CONTROLLED FEATURE-BASED MIGRATION

0. Architect Intent

FE-ADMIN-W3 is accepted.

Verified state:

Admin-Migration-Pattern = PROVEN

Flat-Admin-Recovery-State = CONTINUE_FEATURE_MIGRATION

God-Component-Recovery-Readiness = DEFER

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

admin-api LOC = 1107

admin-api export freeze = 45

admin-screens LOC = 1057

FE architecture guards remain green

canonical test discovery is active

no new failure signatures were introduced

Primary objectives:

migrate exactly ONE more low-risk admin capability using the proven pattern

reduce admin-api capability debt again

reduce flat admin/admin-screens debt again

prefer a slice that materially moves admin-screens toward <800 LOC without touching a giant decomposition seam

preserve all UI/API/route/product behavior

reassess whether the next move should still be feature migration or characterization-first god-component recovery

No broad refactor.
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

docs/evidence/TB-TMAR-FE-ADMIN-W3/recovery-sot.md

docs/evidence/TB-TMAR-FE-ADMIN-W3/admin-api-shrink.md

docs/evidence/TB-TMAR-FE-ADMIN-W3/flat-admin-shrink.md

docs/evidence/TB-TMAR-FE-ADMIN-W3/flat-recovery-state.md

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
982e461bc1ccdc6ff340fe4d0f006f6c771b5e0c

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.
No broad git add ..

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/recovery-start.md

2. Select Exactly ONE Additional Low-Risk Admin Capability

Use repository evidence to choose one remaining capability.

Known remaining examples may include:

customers

receipts

orders

other capability-specific admin areas

Selection priority:

clear capability ownership

measurable admin-api shrink

measurable flat-screen shrink

low workflow risk

no critical giant dependency

no checkout/payment consistency semantics

characterization tests available or cheap to add

Prefer customers/receipts or another clearly bounded low-risk slice over Orders if Orders touches checkout/payment/fulfillment complexity.

Do NOT select by LOC reduction alone.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/slice-selection.md

3. Characterization Before Migration

Before moving implementation, add/confirm focused characterization tests.

Cover as applicable:

route composition

screen render

query/filter/sort/search/pagination

API request/response shape

mutations/status changes

permissions/visibility

loading/error/empty states

locale/RTL

navigation/deep links

No broad snapshots.
No fake behavior.
No workaround code merely to make tests pass.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/characterization-tests.md

4. Perform Exactly One Feature Migration

Move exactly the selected capability to:

src/frontend/features/<capability>/

Only create folders actually needed:

components

api/data

hooks

model/types

public boundary

Rules:

App Router route files stay thin

owned implementation leaves flat admin area

capability-specific API code leaves generic admin-api

no duplicate stale implementation

no alias swamp

no deep external imports

no feature cycles

shared transport/auth remains shared

shared design-system primitives remain shared

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/migration.md

5. Shrink admin-api Again

For selected capability:

move capability-specific data access out of generic admin-api

preserve generic request/auth/store/locale propagation

preserve error/retry semantics

do not duplicate fetch wrappers

do not introduce TanStack Query/SWR

Record before/after:

admin-api LOC

exported capability-specific symbols

remaining capability debt if measurable

No baseline widening.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/admin-api-shrink.md

6. Shrink Flat Admin Aggregation Again

For the selected capability, remove its owned implementation from admin-screens or equivalent flat aggregator.

Record exact before/after LOC.

Important:

prefer a slice that naturally reduces admin-screens significantly

do not manipulate formatting just to cross a threshold

do not extract unrelated code

if admin-screens crosses below 800 LOC naturally, immediately shrink/remove its oversized baseline allowance

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/flat-admin-shrink.md

7. Public Feature Boundary

Require:

external imports through deliberate feature public boundary

no deep imports into feature internals

no shared lib/UI → feature reverse dependency

no feature cycles

no catch-all barrel explosion

Temporary compatibility re-export only if:

strictly necessary

minimal

documented

clearly temporary

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/feature-boundary.md

8. Architecture Guard Compliance

Keep green:

FE-FOLDER-001

FE-FOLDER-002

FE-SIZE-001

FE-SIZE-002

FE-SEO-001

FE-BOUNDARY-001

FE-BOUNDARY-002

migrated-feature deep-import guards

canonical test-discovery guard

No wildcard suppression.
No baseline widening.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/architecture-guards.md

9. Client / Server Boundary Safety

Admin-only migration.

Do NOT:

expand use client unnecessarily

opportunistically convert components

touch storefront SSR/SEO routes

Record before/after client directives for touched files only.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/client-boundary.md

10. Source-Size Safety

No new handwritten source >800 LOC.

Existing oversized files:

may only shrink

baseline must shrink when they improve

must not grow

Do NOT split a god-component in this task.

If selected slice requires such a split:
abandon that slice and choose another low-risk capability.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/source-size-compliance.md

11. Test Discovery and Validation

Use canonical discovery.

Required:

selected capability characterization tests

npm run test:architecture

canonical discovered frontend tests

TypeScript check against known legacy baseline

lint if canonical

build if canonical and reasonably bounded

Record discovered test-file count.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/tests.md

12. Legacy Failure Control

Known pre-existing failures include:

critical-storefront bg-white

PAGE_SIZES

admin-api actor ESM

inventory count drift

legacy typecheck failures

Verify current signatures first.

Do NOT:

suppress them

rewrite unrelated areas

add new allowed failures

Required:
NEW_FAILURES = 0

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/legacy-failures.md

13. God-Component Recovery Readiness Reassessment

Do NOT split a giant yet.

Return:
God-Component-Recovery-Readiness: READY
or
God-Component-Recovery-Readiness: DEFER

READY requires:

one exact candidate identified

ownership understood

behavior characterized

API/data dependencies isolated

decomposition seams documented

no visual redesign needed

enough tests exist to support incremental extraction

If READY:
document the first safe extraction seam only.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/god-component-readiness.md

14. Flat Admin Recovery State

Return exactly one:

Flat-Admin-Recovery-State: CONTINUE_FEATURE_MIGRATION

Flat-Admin-Recovery-State: READY_FOR_GODFILE

Flat-Admin-Recovery-State: SUFFICIENTLY_STABILIZED

Use evidence from:

admin-api LOC/debt

admin-screens LOC

remaining ownership ambiguity

oversized thresholds

test/characterization readiness

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W4/flat-recovery-state.md

15. Next Task Decision

Choose automatically:

A. TB-TMAR-FE-ADMIN-W5
if another low-risk feature migration remains highest value.

B. TB-TMAR-FE-GODFILE-W1
only if God-Component-Recovery-Readiness = READY.

C. TB-TMAR-HOST-W3
if frontend flat structure is sufficiently stabilized and backend Host debt is now higher value.

D. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if frontend can safely pause and checkout consistency design is now higher value.

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

permissions semantics

SEO behavior

17. Recovery State

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if durable rules change

docs/architecture/TOOBA-CAPABILITY-MAP.md only if ownership evidence materially changes

docs/evidence/TB-TMAR-FE-ADMIN-W4/recovery-sot.md

Record:

migrated capability

admin-api before/after

flat admin before/after

God-Component-Recovery-Readiness

Flat-Admin-Recovery-State

next task

18. Acceptance Criteria

PASS only if:

exactly one additional low-risk capability migrated

admin-api debt shrinks

flat admin debt shrinks

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