PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-FE-BASELINE

Parent-Task:
TB-TMAR-CONTRACTS-W6

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
TMAR Frontend Architecture Baseline — Structure, Size, Client/Server Boundaries, Libraries, Tests and SEO-Safe Recovery Plan

Task Type:
AUDIT + GUARDS + RECOVERY PLAN
NO BROAD FRONTEND REFACTOR IN THIS TASK

0. Architect Intent

CONTRACTS-W6 is accepted.

Verified state:

Backend-Structural-Readiness = STABLE_FOR_PARALLEL_RECOVERY

Frontend-Recovery-Readiness = READY

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

App→App debt remains known/frozen but no longer blocks parallel frontend recovery

Checkout shared-ACID remains deferred and frozen

user explicitly wants frontend architecture, folder organization, and long-term maintainability corrected

user explicitly does not want another wrong architectural move

This task establishes the frontend source of truth BEFORE implementation.

Primary objective:
create an evidence-backed frontend architecture baseline and enforce no-growth guards before any large folder move, component split, library replacement, or client/server conversion.

No cosmetic cleanup.
No Big Bang frontend rewrite.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Frontend:
D:\Users\User\source\repos\SarvNewVer\src\Web

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CONTRACTS-W6/recovery-sot.md

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
c8730b2069ec5b607b83f10de97602c136c2f01f

If tracked user work conflicts:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/recovery-start.md

2. Frontend Repository Inventory

Inventory the full src/Web tree.

Report at minimum:

total TS/TSX/JS/JSX files

routes/pages/layouts

app-router groups

admin files

storefront files

shared UI/components

hooks

services/data-access

state management

feature folders

test files

CSS/style files

generated/vendor-like files that should be excluded from source-size analysis

public/static assets only as structure counts, not repository-bloat remediation

Produce machine-readable inventory:
docs/evidence/TB-TMAR-FE-BASELINE/frontend-inventory.json

Human summary:
docs/evidence/TB-TMAR-FE-BASELINE/frontend-inventory.md

Do not move files.

3. Current Folder / Ownership Analysis

Audit whether frontend organization reflects capabilities/features or has flat/global accumulation.

Pay special attention to:

app/admin

storefront route organization

shared components

product workspace

category admin

builder/appearance/editor areas

checkout/cart

catalog/product/category

campaigns/promotions

content/articles

store pages

reusable design-system primitives

Classify folders/files as:

GOOD_BOUNDARY

ACCEPTABLE_LEGACY

FLAT_FEATURE_ACCUMULATION

WRONG_OWNERSHIP

SHARED_DUMPING_GROUND

NEEDS_DESIGN

Do not decide ownership solely from filename.
Use imports, route responsibility, data source, and behavioral ownership.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/folder-ownership.md

4. Target Frontend Folder Architecture

Design a target structure that preserves Next.js routing while moving implementation ownership toward capability/feature boundaries.

Target principles:

App Router remains route composition layer

route files should stay thin

business/feature UI should not accumulate directly under route folders

reusable primitives separate from business feature components

admin and storefront may share domain-facing client libraries without sharing inappropriate presentation state

SEO-critical storefront rendering must remain compatible with server rendering

folders should reflect capability ownership, not arbitrary technical buckets alone

A valid target may resemble conceptually:

src/Web/
app/
...
features/
catalog/
pricing/
cart/
checkout/
promotions/
appearance/
content/
...
components/
ui/
lib/
api/
auth/
locale/
...
hooks/
tests/

BUT:
do not blindly impose this exact tree.
Derive the final target from repository evidence.

Define:

route layer

feature layer

shared UI layer

shared technical lib layer

API/data-access layer

test organization

boundary/import rules

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/target-folder-architecture.md

5. Source-Size / God-Component Inventory

Repository-wide frontend source-size audit for hand-written TS/TSX/JS/JSX.

Thresholds:

WATCH >500 LOC

OVERSIZED >800 LOC

CRITICAL >1200 LOC

Inventory:

path

LOC

category

route/feature ownership

imported dependency fan-out if reasonably obtainable

suggested decomposition seams

Known examples from external review such as:

category-admin-screen.tsx

product-workspace-screen.tsx
are examples only, NOT the scope boundary.

Create:
docs/evidence/TB-TMAR-FE-BASELINE/frontend-source-size-baseline.json
docs/evidence/TB-TMAR-FE-BASELINE/god-component-plan.md

Add shrink-only guard:

new hand-written frontend source >800 LOC fails

existing oversized files may not grow above baseline

shrink is allowed

baseline may only shrink

Do NOT split giant files yet.

6. use client / Server-Client Boundary Audit

Audit all use client files.

Do NOT treat count alone as a defect.

Classify client components:

REQUIRED_INTERACTIVE

CLIENT_BY_DEPENDENCY

CLIENT_BY_PARENT_CONTAGION

LIKELY_SERVER_CANDIDATE

NEEDS_REVIEW

Focus on SEO-sensitive storefront paths:

Home

PLP/category

PDP

search

landing pages

campaign/amazing pages

article/content pages

store pages

For each critical route, document:

server entry/layout/page status

main content render path

metadata generation

client boundary placement

whether product/category/article textual content is server-renderable/indexable

hydration-sensitive widgets

whether client contagion unnecessarily pulls large route subtrees client-side

No component conversion in this task.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/client-server-boundaries.md
Machine-readable:
docs/evidence/TB-TMAR-FE-BASELINE/client-server-boundaries.json

7. SEO Rendering Safety Baseline

Create explicit frontend SEO invariants.

At minimum:

canonical product/category/article/store content must not depend solely on post-hydration client fetch

route metadata must remain server-capable

JSON-LD/schema output must remain SSR-safe where used

route navigation and canonical URL generation must remain locale/store aware

visual effects/sliders/rails must not become content authority

loading optimizations must not remove crawlable primary content

multi-language/RTL architecture must remain unlimited-locale safe, not FA/EN-only

Add FE-SEO-001 architecture lock:
New storefront implementation must not make primary indexable content client-only without explicit architectural justification.

If mechanically testable, add a narrow guard/characterization test.
Do not create brittle HTML snapshot spam.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/seo-rendering-baseline.md

Update:
docs/architecture/TMAR-architecture-locks.md

8. Import / Layer Boundary Audit

Build an import graph or equivalent evidence for frontend.

Identify:

route → feature

feature → shared UI

feature → API/lib

feature → feature

shared UI → feature (usually suspicious)

technical lib → feature (suspicious)

circular dependencies

deep relative imports

cross-feature internal imports

Define target frontend dependency direction.

Suggested intent:

route layer may compose features

features may use shared UI/lib

shared UI/lib must not depend on business features

feature internals should not be imported across features except via explicit public boundary/index where appropriate

Do not implement a huge barrel-file architecture.

Create exact baseline/guard if safe:

new shared→feature reverse dependency should fail

new prohibited cross-feature internal imports should fail where detectable

existing debt baselined and shrink-only

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/frontend-import-boundaries.md

9. Data Fetching / API Access Audit

Audit actual frontend data access.

Determine:

server-side fetch usage

client fetch usage

custom API wrappers

duplicated fetch clients

caching/revalidation patterns

mutation handling

error handling

auth/session propagation

store/tenant/locale propagation

retry behavior

query state duplication

IMPORTANT:
Do NOT assume TanStack Query/SWR is required.

Return one of:

CURRENT_APPROACH_ACCEPTABLE

CENTRALIZE_EXISTING_WRAPPER

NEEDS_DEDICATED_CLIENT_DATA_LAYER

NEEDS_SERVER_FETCH_CONSOLIDATION

MIXED_STRATEGY_RECOMMENDED

If a future library is justified, document why and where.
No library installation in this task.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/data-access-audit.md

10. Library Overlap / Dependency Audit

Audit package.json and real imports.

Known concerns to verify:

CKEditor vs TipTap

Swiper vs Embla

antd usage

duplicate utility/component libraries

unused dependencies

heavy browser-only dependencies affecting server/client boundaries

For each:

package

actual import count/files

capability owner

bundle/client implication

keep/consolidate/remove-later recommendation

Do not uninstall anything yet.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/frontend-dependencies.md

11. Test Discovery / Coverage Architecture

Audit frontend test setup.

Verify:

test scripts

discovery globs

manually enumerated test files

unit/component/integration/e2e separation

whether important admin/storefront paths are characterized

If test discovery is manually hardcoded and demonstrably incomplete:
fix ONLY test discovery/configuration if low-risk and no behavior change.

Otherwise document exact repair task.

Create a minimal frontend architecture test layer if none exists, limited to:

source-size guard

import-boundary guard

test discovery sanity

possibly SEO/server-boundary characterization if robust

Do NOT create broad snapshot tests.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/frontend-tests.md

12. Folder Migration Strategy

Do NOT move folders now.

Create phased migration plan:

Phase FE-F1:

guards/baselines

small low-risk shared structure fixes

Phase FE-F2:

admin feature extraction from flat accumulation

Phase FE-F3:

storefront feature ownership cleanup

Phase FE-F4:

giant component decomposition with characterization tests

Phase FE-F5:

server/client boundary optimization for SEO/performance

Phase FE-F6:

dependency consolidation/removal

For each phase define:

candidate files

dependencies

risk

required tests

rollback/recovery point

expected benefit

Important:
physical folder moves happen only AFTER ownership and import boundary are understood.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/frontend-recovery-roadmap.md

13. Frontend Architecture Locks

Add canonical locks to docs/architecture/TMAR-architecture-locks.md.

At minimum propose/record:

FE-ARCH-001
New route files should remain thin composition/transport layers; business feature UI must not accumulate directly in route files.

FE-SIZE-001
New hand-written frontend source >800 LOC is prohibited.

FE-SIZE-002
Existing oversized frontend files are shrink-only against baseline.

FE-SEO-001
Primary indexable storefront content must not become client-only without explicit architecture approval.

FE-BOUNDARY-001
Shared UI/technical libraries must not depend on business feature modules.

FE-BOUNDARY-002
New cross-feature imports must use an approved public feature boundary rather than deep internal imports.

Do not create unenforceable locks without evidence/guard strategy.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/frontend-locks.md

14. Characterization Before Refactor

For top critical files planned for later decomposition, identify required characterization tests FIRST.

At minimum for top 5 highest-risk frontend giants:

user-visible behavior

route/query behavior

mutation behavior

locale/RTL behavior where relevant

SEO/server behavior where relevant

Do not perform the decomposition now.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/characterization-plan.md

15. Backend Isolation

Do NOT modify backend production code in this task.

Backend changes allowed only if:

necessary test/architecture metadata update

documentation/recovery state

absolutely necessary compile fix caused by frontend architecture-test tooling (should normally be none)

Do NOT resume Contracts/Host cleanup in this task.

16. Product Behavior

No product feature behavior change.

No visual redesign.

Do not:

alter Shopeiva fidelity

change Mega Menu behavior

change Product Rail designs

change slider behavior

change PDP/PLP content

change admin workflows

change localization strings

change API contracts

This is architecture baseline work.

17. Build / Tests

Run appropriate frontend validation:

package-manager install state verification without unnecessary dependency churn

TypeScript check

lint if canonical and currently healthy

frontend tests using canonical test runner

architecture guards added in this task

Next build if reasonably bounded and canonical for repo

Do not "fix" unrelated legacy lint warnings by broad edits.

Record exact commands and results.

Evidence:
docs/evidence/TB-TMAR-FE-BASELINE/tests.md

18. Recovery State

Update:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-CAPABILITY-MAP.md only if actual capability ownership knowledge materially changed

docs/evidence/TB-TMAR-FE-BASELINE/recovery-sot.md

Do NOT churn Capability Map merely because task ran.
Only update ownership entries supported by new evidence.

Record:

frontend baseline accepted state

source-size baseline location

architecture locks

top frontend recovery priorities

next task

19. Next Task Decision

Choose automatically after evidence.

A. TB-TMAR-FE-F1
if guards are in place and a low-risk first frontend structural slice is clear.

B. TB-TMAR-FE-ADMIN-W1
if flat admin organization is clearly the highest-value first migration slice.

C. TB-TMAR-FE-GODFILE-W1
if one critical giant file blocks architecture and characterization coverage is sufficient.

D. TB-TMAR-HOST-W3
if frontend baseline reveals no urgent implementation and backend Host cleanup is higher value.

E. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if frontend baseline is complete and checkout design should be formalized before more structural implementation.

Do not ask the user.

20. Acceptance Criteria

PASS only if:

full frontend inventory exists

folder/ownership problems are evidence-backed

target frontend folder architecture is documented

source-size baseline/guard exists

server/client boundary audit exists

SEO rendering baseline/lock exists

import boundary baseline/guard exists where safely automatable

data access strategy is audited without blindly installing libraries

dependency overlap is verified from real imports

test discovery is audited/fixed only if low risk

phased folder migration roadmap exists

top giant components have characterization plans

no broad refactor occurred

no product behavior changed

user work preserved

next task automatically selected

canonical Result delivered

Worker stops completely

21. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Frontend-Inventory
Folder-Ownership
Target-Folder-Architecture
Source-Size-Baseline
God-Component-Plan
Client-Server-Boundaries
SEO-Rendering-Baseline
Import-Boundaries
Data-Access-Audit
Frontend-Dependencies
Frontend-Tests
Frontend-Recovery-Roadmap
Frontend-Locks
Characterization-Plan
Build-Validation
Capability-Map
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Next-Recommended-Task

Product-Resume-Safety is expected to remain:
SAFE_WITH_TMAR_PARALLEL

After canonical Result through Bridge:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK