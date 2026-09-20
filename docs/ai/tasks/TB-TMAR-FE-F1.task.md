PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-FE-F1

Parent-Task:
TB-TMAR-FE-BASELINE

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
TMAR Frontend Foundation Wave 1 — Freeze Flat Growth, Repair Test Discovery, and Migrate One Low-Risk Admin Feature Slice

Task Type:
IMPLEMENTATION — SMALL STRUCTURAL SLICE + GUARDS

0. Architect Intent

FE-BASELINE is accepted.

Verified frontend facts:

canonical frontend root is src/frontend

710 TS/JS sources

167 tests

253 use client

26 oversized/critical frontend files

flat admin accumulation and admin-api dumping-ground behavior are primary structural debt

frontend source-size, import-boundary, and SEO guards now exist

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

This task is the FIRST controlled frontend structural migration.

Goals:

freeze NEW flat-admin/admin-api dumping-ground growth

make test discovery canonical and maintainable if current manual enumeration is confirmed

migrate exactly ONE low-risk admin capability slice into the target feature structure

keep route behavior, UI, API contracts, locale, RTL, SEO and product semantics unchanged

prove the migration pattern before broader folder reorganization

No Big Bang move.
No giant component split.
No visual redesign.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Frontend root:
D:\Users\User\source\repos\SarvNewVer\src\frontend

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/evidence/TB-TMAR-FE-BASELINE/recovery-sot.md

docs/evidence/TB-TMAR-FE-BASELINE/folder-ownership.md

docs/evidence/TB-TMAR-FE-BASELINE/target-folder-architecture.md

docs/evidence/TB-TMAR-FE-BASELINE/frontend-import-boundaries.md

docs/evidence/TB-TMAR-FE-BASELINE/frontend-tests.md

docs/evidence/TB-TMAR-FE-BASELINE/frontend-recovery-roadmap.md

docs/evidence/TB-TMAR-FE-BASELINE/characterization-plan.md

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD==origin/main

git status --short

git diff --name-only

git diff --cached --name-only

18ca10c9 ancestor

user work preserved

Expected previous accepted tip:
aa393175729cf524679eff240f504bac006bc747

If tracked user work conflicts:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.

Evidence:
docs/evidence/TB-TMAR-FE-F1/recovery-start.md

2. Freeze Flat Admin Growth

Using FE-BASELINE evidence, identify the exact flat admin locations and the admin-api accumulation path(s).

Create a machine-readable baseline of:

direct files in the flat admin accumulation area

direct files in admin-api dumping-ground area

approved route files that must remain in App Router

approved temporary compatibility files

Add shrink-only architecture guards:

FE-FOLDER-001
No NEW business-feature implementation file may be added directly to a baselined flat admin accumulation directory.

FE-FOLDER-002
No NEW capability-specific API client/service may be added to the generic admin-api dumping-ground path.

Existing debt:

is baselined

may remain temporarily

may only shrink

no wildcard suppression

Do not block legitimate Next.js route convention files such as:

page

layout

loading

error

not-found

route
when they belong in App Router.

Evidence:
docs/evidence/TB-TMAR-FE-F1/flat-growth-guard.md

Baseline:
docs/evidence/TB-TMAR-FE-F1/frontend-flat-folder-baseline.json

3. Repair Test Discovery If Confirmed Manual/Incomplete

FE-BASELINE reported test discovery remains npm-script enumerated.

Verify exact current mechanism first.

If test execution depends on a manually maintained file list:
replace ONLY the discovery mechanism with a canonical glob/config-based discovery strategy supported by the repository's existing test runner.

Requirements:

do not change test framework unless absolutely necessary

do not rewrite tests

do not broaden into unsupported test categories accidentally

exclude generated/vendor/build output

preserve intentional architecture-test commands if they are a separate suite

document before/after discovered test counts

fail if known test fixtures unintentionally disappear

Add a discovery sanity test or script if appropriate.

If repository evidence shows enumeration is intentional and safer than glob discovery, do not replace it; document that and instead add a guard that prevents silent omission of test files.

Evidence:
docs/evidence/TB-TMAR-FE-F1/test-discovery.md

4. Select Exactly ONE Low-Risk Admin Capability Slice

From FE-BASELINE folder ownership evidence, automatically choose ONE capability with:

small/moderate file count

clear ownership

low visual risk

no giant file decomposition required

no route semantics change

no SEO-critical storefront impact

bounded API/data-access surface

adequate characterization coverage or easy-to-add focused characterization tests

Do NOT choose:

category-admin giant if decomposition is required

product-workspace giant if decomposition is required

checkout

payment

highly stateful builder/appearance area
unless evidence proves the slice is truly low risk.

Before moving anything, document:

current files

current imports

current route entry points

API/data-access files

shared dependencies

tests

target feature path

rollback point

expected import-boundary improvements

Evidence:
docs/evidence/TB-TMAR-FE-F1/slice-selection.md

5. Target Feature Shape

For the selected slice, use the FE-BASELINE target architecture.

A typical shape may be:

src/frontend/features/<capability>/

components/

api/ or data/

hooks/

model/ / types/ where justified

index.ts as a deliberate public boundary only if needed

BUT:
derive the exact structure from the selected capability.
Do not create empty ceremonial folders.
Do not create barrel exports for everything.

Rules:

App Router files remain route composition only

feature internals belong under the selected feature

generic design-system primitives stay shared

capability-specific API access leaves generic admin-api dumping ground

shared technical helpers must not be moved into a feature unless ownership is clear

Evidence:
docs/evidence/TB-TMAR-FE-F1/target-slice-structure.md

6. Characterization Before Move

Before structural relocation, add or confirm focused characterization tests for the selected slice.

Cover relevant behavior:

route renders/entry composition

primary user actions

API call shape

query/mutation semantics

error behavior

locale/RTL behavior if applicable

permissions/authorization visibility if applicable

Do not add broad snapshots.

Evidence:
docs/evidence/TB-TMAR-FE-F1/characterization-tests.md

7. Perform the Narrow Structural Migration

Move only the selected slice.

Required:

update imports cleanly

no temporary duplicate implementation

no copy-and-leave stale source unless a compatibility wrapper is justified

compatibility wrapper must be minimal, documented, and temporary

preserve route paths

preserve component public props

preserve API request/response behavior

preserve styling/classes

preserve localization keys

preserve user-visible behavior

Avoid alias churn unrelated to the slice.

Do not rename product concepts casually.

Evidence:
docs/evidence/TB-TMAR-FE-F1/migration.md

8. Public Feature Boundary

If other areas consume the selected feature:

Prefer:
features/<capability> public boundary

Avoid:
deep imports into:

components/internal

hooks/internal

api/internal

Only add a public index.ts when there are real cross-boundary consumers.

Do not create circular dependencies.

Update import-boundary baseline/guard so new deep external imports into the migrated feature are prohibited.

Evidence:
docs/evidence/TB-TMAR-FE-F1/feature-boundary.md

9. Admin API De-Dumping Rule

For the selected capability:

move capability-specific API/data access out of generic admin-api

keep truly generic transport/auth/request infrastructure in shared technical lib

do not duplicate fetch wrappers

do not introduce TanStack Query/SWR in this task

do not alter retry/auth/store/locale propagation semantics

Document which remaining admin-api files are:

CAPABILITY_SPECIFIC_DEBT

SHARED_TECHNICAL

NEEDS_REVIEW

Evidence:
docs/evidence/TB-TMAR-FE-F1/admin-api-classification.md

10. Server / Client Boundary Preservation

This task is admin-focused.

Do not convert components between Server/Client unless the selected move mechanically requires a directive to remain where it already semantically belongs.

No opportunistic use client removal.

Verify:

no new unnecessary use client

no client-boundary expansion

no storefront server-rendering regression

Evidence:
docs/evidence/TB-TMAR-FE-F1/client-boundary.md

11. Source-Size Guard Compliance

Existing FE source-size guards stay active.

No new hand-written source >800 LOC.
No existing oversized file may grow above baseline.

Do not split critical giant files in this task.

If selected slice unexpectedly requires touching a critical giant:
STOP that slice and choose another low-risk slice.

Evidence:
docs/evidence/TB-TMAR-FE-F1/source-size-compliance.md

12. Architecture Tests

Extend npm run test:architecture as needed to cover:

FE-FOLDER-001

FE-FOLDER-002

selected feature public-boundary rule

existing FE-SIZE locks

existing FE-BOUNDARY locks

existing FE-SEO lock

Architecture tests must be deterministic and path-based/import-based, not fragile text snapshots.

Evidence:
docs/evidence/TB-TMAR-FE-F1/architecture-guards.md

13. Existing Pre-Existing Failures

FE-BASELINE recorded:

one pre-existing critical-storefront failure involving bg-white

legacy typecheck failures

Do NOT broadly "fix" these just to make this task green.

However:

verify this task introduces no NEW failures

record exact before/after failure counts/signatures

if this task touches the exact failing area, preserve or improve it without widening scope

never suppress failures

Evidence:
docs/evidence/TB-TMAR-FE-F1/legacy-failures.md

14. Validation

Run appropriate validation:

Required:

architecture suite

selected slice characterization tests

full relevant frontend test command using repaired/current discovery

TypeScript check, comparing against known baseline failures

lint if canonical, comparing against known baseline

build if canonical and reasonably bounded

Pass criteria:

no new failures

architecture guards pass

selected slice tests pass

test discovery has no silent omission

Evidence:
docs/evidence/TB-TMAR-FE-F1/tests.md

15. Folder Migration Roadmap Update

Update FE roadmap with actual evidence from this first migration.

Record:

migration pattern that worked

compatibility issues found

next 3 candidate slices by evidence

whether admin feature migration can continue safely

which giant files still require separate characterization-first decomposition

Do not schedule all moves at once.

Evidence:
docs/evidence/TB-TMAR-FE-F1/roadmap-update.md

16. Product / Visual Safety

No product behavior change.

Do NOT:

redesign UI

change Shopeiva fidelity

change product rails

change sliders

change mega menu

change PDP/PLP behavior

change API contracts

change localization copy

change SEO semantics

alter storefront rendering

17. Recovery State

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md only if the selected frontend ownership evidence materially changes capability knowledge

docs/evidence/TB-TMAR-FE-F1/recovery-sot.md

Record:

selected migrated feature

flat-growth baselines

test discovery state

no-growth guards

next task

18. Next Task Decision

Choose automatically:

A. TB-TMAR-FE-ADMIN-W1
if the first migration pattern is proven and another admin capability slice is clearly next.

B. TB-TMAR-FE-GODFILE-W1
only if characterization is sufficient and a critical giant now blocks progress.

C. TB-TMAR-HOST-W3
if FE-F1 is complete and backend Host debt is higher-value next.

D. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if frontend foundation is now safe to pause and distributed consistency design is higher value.

Do not ask the user.

19. Acceptance Criteria

PASS only if:

flat admin/admin-api growth baselines exist

no-growth guards exist and pass

test discovery is repaired or safely guarded based on evidence

exactly one low-risk admin capability slice is migrated

App Router remains thin

capability-specific API access is moved out of dumping ground for selected slice

no giant-file decomposition occurred

no product/visual behavior changed

no new client-boundary expansion

no new test/type/build regressions

user work preserved

recovery docs updated

next task selected automatically

canonical Result delivered

Worker stops completely

20. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Flat-Growth-Baseline
Test-Discovery
Slice-Selection
Target-Slice-Structure
Characterization-Tests
Migration
Feature-Boundary
Admin-Api-Classification
Client-Boundary
Source-Size-Compliance
Architecture-Guards
Legacy-Failures
Tests
Roadmap-Update
Capability-Map
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Next-Recommended-Task

Product-Resume-Safety expected:
SAFE_WITH_TMAR_PARALLEL

After canonical Result through Bridge:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK
