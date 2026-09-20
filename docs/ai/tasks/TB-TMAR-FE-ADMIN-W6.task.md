PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-FE-ADMIN-W6

Parent-Task:
TB-TMAR-FE-ADMIN-W5

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
TMAR Frontend Admin Wave 6 — Cross the Flat-Admin Size Threshold and Close Low-Risk Capability Debt

Task Type:
IMPLEMENTATION — CONTROLLED FEATURE MIGRATION + EXIT CHECKPOINT

0. Architect Intent

FE-ADMIN-W5 is accepted.

Verified state:

Admin-Migration-Pattern = PROVEN

Flat-Admin-Exit-State = NOT_READY

Flat-Admin-Recovery-State = CONTINUE_FEATURE_MIGRATION

Architecture-Priority = FE_ADMIN

God-Component-Recovery-Readiness = DEFER

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

admin-api LOC = 1022

admin-api export freeze = 38

admin-screens LOC = 894

residual admin-api debt is reported as mostly Orders + Dashboard

architecture guards are green

canonical test discovery is active

NEW_FAILURES = 0

Primary objectives:

migrate exactly ONE additional LOW-RISK admin capability

prefer Dashboard or another isolated slice if repository evidence confirms low risk

avoid Orders unless it is provably isolated and does not touch checkout/payment/fulfillment semantics

drive admin-screens below 800 LOC if this can happen naturally through owned-feature extraction

shrink admin-api capability debt again

re-evaluate whether flat-admin recovery is now ready to pivot away from repetitive migration waves

No Big Bang move.
No visual redesign.
No god-file split.
No Orders migration by default.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Frontend root:
D:\Users\User\source\repos\SarvNewVer\src\frontend

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/evidence/TB-TMAR-FE-ADMIN-W5/recovery-sot.md

docs/evidence/TB-TMAR-FE-ADMIN-W5/admin-api-shrink.md

docs/evidence/TB-TMAR-FE-ADMIN-W5/flat-admin-shrink.md

docs/evidence/TB-TMAR-FE-ADMIN-W5/flat-admin-exit-criteria.md

docs/evidence/TB-TMAR-FE-ADMIN-W5/architecture-priority.md

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
6f5689418ccd5ab8e76f25a2bd2861e483fe6e0a

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

Never use destructive Git operations.
Never use broad git add ..

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/recovery-start.md

2. Select Exactly ONE Low-Risk Residual Admin Capability

Current evidence says residual capability debt is mostly Dashboard + Orders.

Selection rule:

Prefer:

Dashboard capability/sub-capability if it has clear ownership and is composition/read-oriented

another isolated residual capability if repository evidence shows one

Orders ONLY if all of the following are true:

selected slice is read-only or presentation-only

no checkout/payment/fulfillment consistency semantics

no mutation workflow coupled to order lifecycle transitions

no critical giant decomposition required

characterization tests prove behavior

If Orders does not meet these conditions:
DO NOT select it.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/slice-selection.md

3. Characterization Before Migration

Before moving code, add/confirm focused tests for the selected slice.

Cover as applicable:

route composition

render behavior

dashboard cards/metrics/list data shape

filters/search/sort/pagination

API request/response shape

permissions/visibility

loading/error/empty states

locale/RTL

navigation/deep links

No broad snapshots.
No workaround logic just to pass tests.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/characterization-tests.md

4. Perform Exactly One Feature Migration

Move selected capability to:

src/frontend/features/<capability>/

Rules:

App Router remains thin

capability-owned implementation leaves flat admin

capability-specific API/data access leaves generic admin-api

no duplicate stale implementation

no deep external imports

no cycles

no alias swamp

shared transport/auth remains shared

shared UI/design-system remains shared

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/migration.md

5. Shrink admin-api Again

Move only capability-specific data/API functions.

Preserve:

auth propagation

store/tenant context

locale propagation

request/response semantics

error/retry behavior

Do NOT:

duplicate fetch wrappers

introduce TanStack Query/SWR

relocate genuinely shared formatters to fake LOC reduction

Record before/after:

admin-api LOC

export count

remaining CAPABILITY_SPECIFIC_DEBT

SHARED_TECHNICAL

NEEDS_REVIEW

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/admin-api-shrink.md

6. Drive admin-screens Below 800 If Naturally Possible

Current verified admin-screens LOC = 894.

For selected capability:

remove only its owned implementation from the flat aggregator

keep unrelated code untouched

preserve public behavior/imports

Goal:
if the selected owned slice is sufficient, naturally reduce admin-screens below 800 LOC.

Do NOT:

reformat large blocks just to reduce line count

split unrelated code

alter logic to hit the threshold

If admin-screens falls below 800:

immediately remove/shrink its oversized baseline allowance

record it as no longer oversized

do not retain stale exemption

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/flat-admin-shrink.md

7. Public Feature Boundary

External consumers must use deliberate public boundary.

Prohibit:

deep imports into internals

shared lib/UI → feature reverse dependency

feature cycles

global catch-all barrels

Temporary compatibility re-export allowed only if:

strictly necessary

minimal

documented

temporary

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/feature-boundary.md

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
docs/evidence/TB-TMAR-FE-ADMIN-W6/architecture-guards.md

9. Client / Server Boundary Safety

Admin-only migration.

Do NOT:

expand use client unnecessarily

opportunistically convert server/client composition

touch storefront SSR/SEO routes

Record touched client directives before/after.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/client-boundary.md

10. Source-Size Safety

No new hand-written source >800 LOC.

Existing oversized files:

shrink only

baseline must shrink when thresholds improve

must not grow

Do NOT split a god-component.

If selected slice unexpectedly requires god-file decomposition:
abandon the slice and choose another safe capability, or return BLOCKED if none exists.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/source-size-compliance.md

11. Legacy Failure Control

Verify known pre-existing failures before/after.

Known prior signatures include:

critical-storefront bg-white

PAGE_SIZES

admin-api actor ESM

inventory count drift

legacy typecheck failures

Do NOT suppress or widen allowed failures.

Required:
NEW_FAILURES = 0

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/legacy-failures.md

12. Validation

Required:

selected capability characterization tests

npm run test:architecture

canonical discovered frontend tests

TypeScript check against legacy baseline

lint if canonical

build if canonical and bounded

Record discovered test count.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/tests.md

13. Flat-Admin Exit Decision

Re-run W5 exit criteria.

Return exactly:
Flat-Admin-Exit-State: NOT_READY
or
Flat-Admin-Exit-State: READY_TO_PIVOT

READY_TO_PIVOT should require evidence that:

flat feature growth is frozen

remaining flat files are route/shared/compat or explicitly risky debt

admin-screens is below 800 OR remaining oversized work belongs to separate godfile recovery

admin-api capability debt is now small and explicit

next risky residual (especially Orders) should not be migrated casually

boundaries/tests/guards are sufficient to stop repetitive low-risk waves

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/flat-admin-exit.md

14. Orders Boundary Risk Assessment

Because Orders is likely to become the remaining major admin capability debt, perform a READ-ONLY assessment only.

Do NOT migrate Orders in this section.

Document:

exact admin Order files

API methods

mutations/workflows

checkout/payment/fulfillment coupling

source-size/god-file dependencies

characterization gaps

whether Orders should be:

LOW_RISK_FEATURE_MIGRATION

NEEDS_CHARACTERIZATION_FIRST

NEEDS_BACKEND/WORKFLOW_BOUNDARY_FIRST

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/orders-admin-risk.md

15. God-Component Readiness Reassessment

Do NOT split any giant.

Return:
God-Component-Recovery-Readiness: READY
or
God-Component-Recovery-Readiness: DEFER

If READY:
identify exact candidate and first safe seam.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/god-component-readiness.md

16. Architecture Priority Checkpoint

Choose highest-value next area using concrete risk:

Return exactly one:
Architecture-Priority: FE_ADMIN
Architecture-Priority: FE_GODFILE
Architecture-Priority: HOST
Architecture-Priority: CHECKOUT_DESIGN

Guidance:

if flat admin is READY_TO_PIVOT, do not keep issuing ADMIN waves automatically

if Orders is risky and flat low-risk debt is exhausted, pivot

if Host direct-write debt remains significant, HOST may outrank further FE cleanup

if Checkout shared-ACID design risk now dominates, CHECKOUT_DESIGN may outrank

FE_GODFILE only if readiness is READY

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W6/architecture-priority.md

17. Next Task Decision

Choose automatically:

A. TB-TMAR-FE-ADMIN-W7
ONLY if Flat-Admin-Exit-State = NOT_READY AND another low-risk capability clearly remains.

B. TB-TMAR-FE-GODFILE-W1
ONLY if God-Component-Recovery-Readiness = READY AND FE_GODFILE is highest value.

C. TB-TMAR-HOST-W3
if HOST is highest value.

D. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if CHECKOUT_DESIGN is highest value.

Do not ask the user.

18. Product / Visual Safety

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

19. Recovery State

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if durable rules change

docs/architecture/TOOBA-CAPABILITY-MAP.md only if ownership evidence materially changes

docs/evidence/TB-TMAR-FE-ADMIN-W6/recovery-sot.md

Record:

migrated capability

admin-api before/after

admin-screens before/after

Flat-Admin-Exit-State

Orders risk classification

God-Component-Recovery-Readiness

Architecture-Priority

next task

20. Acceptance Criteria

PASS only if:

exactly one low-risk capability migrated

admin-api capability debt shrinks

flat admin debt shrinks

no baseline widening

admin-screens threshold handled correctly if crossed

no god-file split

no product/visual/API behavior change

architecture guards green

canonical discovery preserved

NEW_FAILURES = 0

Flat-Admin-Exit-State returned

Orders risk assessed without migration

God readiness reassessed

Architecture-Priority returned

user work preserved

next task selected automatically

canonical Result delivered

Worker stops completely

21. Result Contract

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
Legacy-Failures
Tests
Flat-Admin-Exit-State
Orders-Admin-Risk
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