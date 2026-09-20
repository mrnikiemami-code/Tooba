PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-FE-ADMIN-W2

Parent-Task:
TB-TMAR-FE-ADMIN-W1

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
TMAR Frontend Admin Wave 2 — Migrate One More Capability, Continue admin-api Shrink, and Prepare for God-Component Work

Task Type:
IMPLEMENTATION — CONTROLLED FEATURE-BASED MIGRATION

0. Architect Intent

FE-ADMIN-W1 is accepted.

Verified state:

admin-promotions migrated to features/admin-promotions

admin-api LOC shrank 1321→1234

admin-api export baseline shrank 59→55

admin-screens shrank 1230→1120 and is no longer CRITICAL, but remains OVERSIZED

FE-FOLDER-001/002 remain green

architecture tests 11/11 PASS

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

migration pattern is safe to continue

giant files remain deferred pending characterization-first work

Primary objectives:

migrate exactly ONE additional low-risk admin capability

shrink admin-api capability debt again

further reduce flat/oversized admin accumulation where naturally achieved

identify whether the next step should remain feature migration or move to characterization-first god-component decomposition

keep product/UI/API behavior unchanged

No Big Bang move.
No visual redesign.
No giant-file split unless only characterization/preparation is needed.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Frontend root:
D:\Users\User\source\repos\SarvNewVer\src\frontend

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/evidence/TB-TMAR-FE-ADMIN-W1/recovery-sot.md

docs/evidence/TB-TMAR-FE-ADMIN-W1/admin-api-shrink.md

docs/evidence/TB-TMAR-FE-ADMIN-W1/admin-migration-pattern.md

docs/evidence/TB-TMAR-FE-ADMIN-W1/roadmap-update.md if present

docs/evidence/TB-TMAR-FE-BASELINE/god-component-plan.md

docs/evidence/TB-TMAR-FE-BASELINE/characterization-plan.md

Verify:

branch main

HEAD == origin/main

exact HEAD SHA

exact origin/main SHA

18ca10c9 ancestor

known/clean worktree state

user work preserved

Expected previous accepted tip:
329ef855ab73e1d99e192d0fc6b9e289cb84c88b

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.
No broad git add ..

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/recovery-start.md

2. Select Exactly ONE Additional Admin Capability

Use current:

admin-api classification

flat-folder baseline

import graph

route ownership

existing tests

source-size baseline

Selection criteria:

clear bounded frontend capability ownership

meaningful admin-api debt reduction

no dependency on critical giant decomposition

low visual and workflow risk

no SEO-critical storefront impact

no heavily stateful builder/editor workflow

no checkout/payment consistency involvement

characterization coverage available or cheap to add

Known remaining capability debt may include orders/reviews/sellers/etc., but selection MUST be repository-evidence-based.

Do not choose a slice merely to maximize LOC reduction.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/slice-selection.md

3. Characterization Before Migration

Before moving implementation, capture behavior.

Cover as applicable:

route composition

screen rendering

query/filter/sort/search behavior

API request/response shape

mutations

permissions/visibility

loading/error/empty states

locale/RTL

navigation/deep links

No broad snapshots.
No fake API behavior added just for tests.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/characterization-tests.md

4. Perform Feature-Based Migration

Move exactly the selected capability to:

src/frontend/features/<capability>/

Use only folders that are genuinely needed:

components

api/data

hooks

model/types

public boundary

Rules:

App Router route files remain thin composition

no duplicate implementation left behind

no alias swamp

no deep external imports into feature internals

no circular dependencies

generic design-system primitives stay shared

generic technical request/auth transport stays shared

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/migration.md

5. Shrink admin-api Debt Again

For the selected capability:

move capability-specific API/data-access methods out of generic admin-api

keep generic transport/auth/request infrastructure shared

preserve store/tenant/locale/auth propagation

preserve retry/error semantics

no TanStack Query/SWR introduction

no duplicated fetch wrappers

Record exact before/after:

admin-api LOC

capability-specific exports count

remaining CAPABILITY_SPECIFIC_DEBT count if measurable

Update classification:

CAPABILITY_SPECIFIC_DEBT

SHARED_TECHNICAL

NEEDS_REVIEW

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/admin-api-shrink.md

6. Reduce Flat Admin Accumulation Where Natural

If selected slice currently contributes to an oversized flat aggregator or screen file:

move ONLY the selected capability's owned implementation out

preserve the aggregator's public surface

do not decompose unrelated capabilities

Record before/after LOC of any affected oversized file.

If an oversized file drops below a threshold:

shrink source-size baseline accordingly

never widen it again

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/flat-admin-shrink.md

7. Feature Boundary Guard

Ensure:

external consumers import through feature public boundary

no new deep imports

shared UI/lib does not depend on business feature

feature-to-feature dependency uses approved public surface

no global barrel explosion

If current architecture guard can safely enforce migrated-feature boundaries generically, extend it.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/feature-boundary.md

8. Folder / Source-Size Guard Compliance

Keep active:

FE-FOLDER-001

FE-FOLDER-002

FE-SIZE-001

FE-SIZE-002

FE-BOUNDARY-001

FE-BOUNDARY-002

FE-SEO-001

No baseline widening.

No new handwritten file >800 LOC.
Existing oversized files may only shrink.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/architecture-guards.md

9. Client / Server Boundary Safety

Admin-focused migration only.

Do not:

expand use client outside selected slice

opportunistically convert components

alter storefront SSR/SEO behavior

Record before/after use client count for touched files only and verify no accidental client-boundary spread.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/client-boundary.md

10. Test Discovery

Use canonical discovered-test runner from FE-F1.

Verify:

selected feature tests are automatically discovered

no manual list added

no test silently disappears

discovered count recorded

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/test-discovery.md

11. Legacy Failure Preservation

Known pre-existing frontend failures include:

critical-storefront bg-white failure

legacy typecheck failures

Do not suppress them.
Do not broaden scope to repair them.

Record:

before signature/count

after signature/count

prove no new failure introduced

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/legacy-failures.md

12. Validation

Required:

selected feature characterization tests

npm run test:architecture

canonical discovered frontend test command

TypeScript check against known legacy baseline

lint if canonical

build if canonical and reasonably bounded

backend TMAR source-size guard only if touched architecture baseline files require it

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/tests.md

13. God-Component Readiness Assessment

Do NOT split a giant component in this task.

After this migration, inspect top critical/oversized frontend files and return:

God-Component-Recovery-Readiness: READY
or
God-Component-Recovery-Readiness: DEFER

READY requires:

characterization seams identified

route/public behavior understood

API/data boundaries known

selected candidate has sufficient tests to support incremental extraction

no need for visual redesign

If READY, identify exactly ONE best candidate for TB-TMAR-FE-GODFILE-W1.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/god-component-readiness.md

14. Admin Migration Pattern Status

Return exactly:

Admin-Migration-Pattern: PROVEN
or
Admin-Migration-Pattern: NEEDS_ADJUSTMENT

Use exact values in canonical Result.

PROVEN requires:

two+ successful feature migrations

feature boundaries stable

admin-api debt measurably shrinking

folder guards working

no behavior regressions

no alias/import mess accumulating

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W2/pattern-review.md

15. Next Task Decision

Choose automatically:

A. TB-TMAR-FE-ADMIN-W3
if admin migration pattern is PROVEN and another low-risk capability slice is clearly best.

B. TB-TMAR-FE-GODFILE-W1
if God-Component-Recovery-Readiness = READY and giant-file debt is now the highest-value FE risk.

C. TB-TMAR-HOST-W3
if frontend structure is sufficiently protected and Host write debt is higher-value.

D. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if frontend can pause safely and checkout distributed-consistency design is the best next architectural investment.

Do not ask the user.

16. Product / Visual Safety

No visual redesign.
No feature behavior changes.

Do NOT alter:

Shopeiva fidelity

menus

sliders

rails

PDP/PLP behavior

route URLs

localization copy

API contracts

permission semantics

SEO behavior

17. Recovery State

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if durable guard changes

docs/architecture/TOOBA-CAPABILITY-MAP.md only when material ownership knowledge changes

docs/evidence/TB-TMAR-FE-ADMIN-W2/recovery-sot.md

Record:

migrated capability

admin-api before/after

oversized flat-file before/after if touched

exact Admin-Migration-Pattern value

God-Component-Recovery-Readiness

next task

18. Acceptance Criteria

PASS only if:

exactly one capability migrated

admin-api debt shrinks measurably

flat/admin debt does not increase

public feature boundary is clean

no deep-import debt introduced

no giant component split performed

no visual/product/API behavior changed

architecture guards remain green

tests discovered automatically

no new test/type/build failure signatures

Admin-Migration-Pattern returned with exact required value

God-Component-Recovery-Readiness assessed

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
Test-Discovery
Legacy-Failures
Tests
God-Component-Recovery-Readiness
Admin-Migration-Pattern
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