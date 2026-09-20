PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-FE-ADMIN-W5

Parent-Task:
TB-TMAR-FE-ADMIN-W4

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
TMAR Frontend Admin Wave 5 — Continue Proven Capability Extraction and Establish Admin Flat-Debt Exit Criteria

Task Type:
IMPLEMENTATION — CONTROLLED FEATURE MIGRATION + RECOVERY CHECKPOINT

0. Architect Intent

FE-ADMIN-W4 is accepted.

Verified state:

Admin-Migration-Pattern = PROVEN

Flat-Admin-Recovery-State = CONTINUE_FEATURE_MIGRATION

God-Component-Recovery-Readiness = DEFER

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

admin-api LOC = 1069

admin-api export freeze = 41

admin-screens LOC = 1036

architecture guards are green

canonical test discovery is active

NEW_FAILURES = 0

remaining known capability debt includes receipts / dashboard / orders / others

Orders is potentially higher-risk because of checkout/payment/fulfillment adjacency and must NOT be chosen casually

Primary objectives:

migrate exactly ONE additional low-risk admin capability

shrink admin-api capability debt again

shrink flat admin/admin-screens again

define objective exit criteria for repeated flat-admin migrations

decide whether to continue ADMIN waves or pivot to Host/Checkout/Godfile work

No Big Bang move.
No visual redesign.
No giant-file split.
No Orders migration unless repository evidence proves it is truly low-risk and isolated.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Frontend root:
D:\Users\User\source\repos\SarvNewVer\src\frontend

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/evidence/TB-TMAR-FE-ADMIN-W4/recovery-sot.md

docs/evidence/TB-TMAR-FE-ADMIN-W4/admin-api-shrink.md

docs/evidence/TB-TMAR-FE-ADMIN-W4/flat-admin-shrink.md

docs/evidence/TB-TMAR-FE-ADMIN-W4/flat-recovery-state.md

docs/evidence/TB-TMAR-FE-BASELINE/god-component-plan.md

docs/evidence/TB-TMAR-FE-BASELINE/characterization-plan.md

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD == origin/main

18ca10c9 ancestor

known/clean worktree state

user work preserved

Expected previous accepted tip:
6b1fff49747e2bcc960fea1ded3e9b1638be96ff

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

Never use destructive Git operations.
Never use broad git add ..

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/recovery-start.md

2. Select Exactly ONE Additional Low-Risk Admin Capability

Use current repository evidence.

Preferred selection order ONLY if supported by actual coupling/risk:

receipts

another isolated CRUD/list capability

dashboard subsection only if it has clear ownership and no global orchestration

orders ONLY if proven isolated and low-risk

Selection criteria:

clear capability ownership

capability-specific API debt currently inside admin-api

meaningful flat-screen shrink

no giant decomposition needed

low mutation/workflow risk

no checkout/payment/fulfillment consistency semantics

no storefront SEO impact

characterization tests available or cheap to add

Do not choose solely for LOC reduction.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/slice-selection.md

3. Characterization Before Migration

Before relocation, confirm/add focused characterization tests.

Cover where relevant:

route composition

screen render

query/filter/sort/search/pagination

API request/response shape

mutations/status changes

permissions

loading/error/empty states

locale/RTL

deep links/navigation

No broad snapshots.
No fake behavior.
No timeout/polling/workaround code to satisfy tests.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/characterization-tests.md

4. Perform Exactly One Feature Migration

Move selected capability to:

src/frontend/features/<capability>/

Create only folders actually required.

Rules:

App Router remains thin

capability implementation leaves flat admin

capability-specific API leaves generic admin-api

no stale duplicate implementation

no deep external imports

no import cycles

no alias swamp

shared transport/auth remains shared

shared design-system primitives remain shared

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/migration.md

5. Shrink admin-api

Move capability-owned data/API functions out of generic admin-api.

Preserve:

auth propagation

store/tenant propagation

locale propagation

request/response semantics

retry/error behavior

Do NOT:

duplicate fetch wrappers

introduce TanStack Query/SWR

move truly shared formatters merely to reduce LOC

Record exact before/after:

admin-api LOC

exported capability symbols

remaining CAPABILITY_SPECIFIC_DEBT

SHARED_TECHNICAL

NEEDS_REVIEW

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/admin-api-shrink.md

6. Shrink Flat Admin Screen Debt

Remove only selected capability-owned implementation from admin-screens or equivalent flat aggregator.

Record exact before/after LOC.

If admin-screens naturally crosses below 800 LOC:

remove/shrink its oversized baseline allowance immediately

do not preserve obsolete allowance

Do not manipulate formatting just to cross threshold.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/flat-admin-shrink.md

7. Define Flat-Admin Exit Criteria

This task must establish durable, objective criteria for when repeated ADMIN-Wx migration waves should STOP.

Create:
docs/evidence/TB-TMAR-FE-ADMIN-W5/flat-admin-exit-criteria.md

At minimum evaluate these dimensions:

A. Folder structure:

no new flat feature growth

remaining flat files are route/compat/shared-only or explicitly baselined debt

B. admin-api:

capability-specific API debt reduced to a small, explicit residual set

generic admin-api contains only truly shared technical helpers or documented residual debt

C. source size:

major flat aggregators below 800 LOC OR remaining oversized files require separate characterization-first godfile tasks

D. boundaries:

migrated features expose public boundaries

deep-import guards are active

E. tests:

canonical discovery active

characterization coverage exists for next risky areas

Return one:
Flat-Admin-Exit-State: NOT_READY
Flat-Admin-Exit-State: READY_TO_PIVOT

Do not set READY merely because this is W5.

8. Public Feature Boundary

External consumers must use deliberate public feature surface.

Prohibit:

deep imports into internals

shared lib/UI → feature reverse dependency

cycles

catch-all global barrels

Compatibility re-export:

only if required

minimal

documented

temporary

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/feature-boundary.md

9. Architecture Guards

Keep active and green:

FE-FOLDER-001

FE-FOLDER-002

FE-SIZE-001

FE-SIZE-002

FE-SEO-001

FE-BOUNDARY-001

FE-BOUNDARY-002

migrated-feature boundary guards

canonical test-discovery guard

No baseline widening.
No wildcard suppression.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/architecture-guards.md

10. Client / Server Boundary Safety

Admin-only migration.

Do NOT:

expand use client unnecessarily

opportunistically change server/client composition

touch storefront SSR/SEO paths

Record client-directive changes for touched files only.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/client-boundary.md

11. Source-Size Safety

No new handwritten source >800 LOC.

Existing oversized files:

shrink only

no baseline growth

remove stale allowances when thresholds improve

Do NOT split a god-component.

If selected slice unexpectedly requires a god-file decomposition:
abandon the slice and choose another safe capability.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/source-size-compliance.md

12. Legacy Failure Control

Verify known pre-existing failures before/after.

Known prior signatures include:

critical-storefront bg-white

PAGE_SIZES

admin-api actor ESM

inventory count drift

legacy typecheck failures

Do NOT suppress, whitelist broadly, or repair unrelated areas.

Required:
NEW_FAILURES = 0

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/legacy-failures.md

13. Validation

Required:

selected characterization tests

npm run test:architecture

canonical discovered frontend tests

TypeScript check against legacy baseline

lint if canonical

build if canonical and reasonably bounded

Record discovered test count.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/tests.md

14. God-Component Readiness Reassessment

Do NOT split a giant yet.

Return:
God-Component-Recovery-Readiness: READY
or
God-Component-Recovery-Readiness: DEFER

READY requires:

exact candidate

ownership known

behavior characterized

data/API dependencies isolated

first extraction seam documented

no visual redesign needed

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/god-component-readiness.md

15. Architecture Priority Checkpoint

Now that backend contracts are stable and frontend foundation is protected, compare next-step architectural value among:

further admin flat migration

Host direct-write cleanup

Checkout consistency design

God-component recovery

Do not rank casually.
Use concrete remaining debt/risk.

Return:
Architecture-Priority: FE_ADMIN
or
Architecture-Priority: HOST
or
Architecture-Priority: CHECKOUT_DESIGN
or
Architecture-Priority: FE_GODFILE

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W5/architecture-priority.md

16. Next Task Decision

Choose automatically based on the checkpoint:

A. TB-TMAR-FE-ADMIN-W6
only if Flat-Admin-Exit-State = NOT_READY and FE_ADMIN remains highest value.

B. TB-TMAR-FE-GODFILE-W1
only if God-Component-Recovery-Readiness = READY and FE_GODFILE is highest value.

C. TB-TMAR-HOST-W3
if HOST is highest value.

D. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if CHECKOUT_DESIGN is highest value.

Do not ask the user.

17. Product / Visual Safety

No visual redesign.
No behavior changes.

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

18. Recovery State

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if durable guards change

docs/architecture/TOOBA-CAPABILITY-MAP.md only if ownership knowledge materially changes

docs/evidence/TB-TMAR-FE-ADMIN-W5/recovery-sot.md

Record:

migrated capability

admin-api before/after

flat admin before/after

Flat-Admin-Exit-State

God-Component-Recovery-Readiness

Architecture-Priority

next task

19. Acceptance Criteria

PASS only if:

exactly one low-risk admin capability migrated

admin-api capability debt shrinks

flat admin debt shrinks

no baseline widening

no god-file split

no product/visual/API behavior change

public boundary clean

architecture guards green

canonical discovery preserved

NEW_FAILURES = 0

objective flat-admin exit criteria documented

Flat-Admin-Exit-State returned

God-Component readiness reassessed

Architecture-Priority returned

user work preserved

next task selected automatically

canonical Result delivered

Worker stops completely

20. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Slice-Selection
Characterization-Tests
Migration
Admin-Api-Shrink
Flat-Admin-Shrink
Flat-Admin-Exit-State
Feature-Boundary
Architecture-Guards
Client-Boundary
Source-Size-Compliance
Legacy-Failures
Tests
God-Component-Recovery-Readiness
Architecture-Priority
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