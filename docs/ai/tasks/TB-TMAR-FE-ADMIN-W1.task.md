PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-FE-ADMIN-W1

Parent-Task:
TB-TMAR-FE-F1

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
TMAR Frontend Admin Wave 1 — Migrate One Additional Admin Capability and Shrink admin-api Debt

Task Type:
IMPLEMENTATION — CONTROLLED FEATURE-BASED MIGRATION

0. Architect Intent

FE-F1 is accepted.

Verified state:

FE-FOLDER-001/002 guards active

test discovery is canonical via scripts/run-discovered-tests.mjs

one proven migration exists: admin-languages → features/admin-languages

admin-api debt shrank 1326→1321

architecture tests 11/11 PASS

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

no giant-component decomposition has begun

This task proves the migration pattern on a SECOND admin capability.

Primary objectives:

migrate exactly ONE additional low-risk admin capability from flat/admin-api debt into feature ownership

shrink admin-api capability-specific debt measurably

strengthen feature-boundary rules if necessary

keep UI/API/route behavior unchanged

do not touch giant components or SEO-critical storefront paths

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Frontend root:
D:\Users\User\source\repos\SarvNewVer\src\frontend

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/evidence/TB-TMAR-FE-F1/recovery-sot.md

docs/evidence/TB-TMAR-FE-F1/roadmap-update.md

docs/evidence/TB-TMAR-FE-F1/admin-api-classification.md

docs/evidence/TB-TMAR-FE-BASELINE/folder-ownership.md

docs/evidence/TB-TMAR-FE-BASELINE/target-folder-architecture.md

Verify:

branch main

HEAD == origin/main

exact SHA

18ca10c9 ancestor

clean/known worktree state

user work preserved

Expected previous accepted tip:
062eaead6037a42d0da7a1059b1a429eee553a62

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/recovery-start.md

2. Select Exactly ONE Additional Low-Risk Admin Capability

Use current evidence from:

flat admin inventory

admin-api classification

roadmap update

import graph

test availability

Selection criteria:

clear capability ownership

moderate/small file set

capability-specific API methods exist in admin-api

no critical giant file dependency

no SEO-critical storefront impact

no heavy builder/editor state

characterization tests can be added or already exist

low regression radius

Do NOT choose merely by smallest file count.
Choose the highest-value SAFE structural slice.

Avoid for this wave if decomposition is needed:

category-admin giant

product-workspace giant

appearance/builder giant

checkout/payment admin

large article/editor workspace

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/slice-selection.md

3. Characterization Before Migration

Before moving files, capture behavior.

Required where relevant:

route composition

primary screen render

API calls and payload shape

mutation semantics

filtering/sorting/search behavior

permissions/visibility

locale/RTL

loading/error states

No broad snapshots.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/characterization-tests.md

4. Migrate Feature Ownership

Move exactly the selected capability into:

src/frontend/features/<capability>/

Use only folders actually needed:

components

api/data

hooks

types/model

public boundary

Do not create ceremonial empty folders.

Route layer:

keep Next.js route files thin

route imports selected feature through its public boundary where appropriate

Public boundary:

expose only externally consumed feature surface

no deep external imports into feature internals

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/migration.md

5. Shrink admin-api Debt

For the selected capability:

move capability-specific API/data-access methods out of generic admin-api

keep generic request/auth/transport infrastructure shared

do not duplicate fetch wrappers

preserve auth/store/locale propagation

preserve error/retry behavior

no TanStack Query/SWR installation

Update classification of remaining admin-api files/methods:

CAPABILITY_SPECIFIC_DEBT

SHARED_TECHNICAL

NEEDS_REVIEW

Record before/after measurable debt count.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/admin-api-shrink.md

6. Feature Boundary Enforcement

Strengthen guards only where evidence supports it.

For migrated features:

outside code must not deep-import feature internals

shared UI/lib must not import business features

feature-to-feature imports must go through approved public boundary

avoid global barrel-file explosion

If a compatibility re-export is required:

minimal

documented

marked temporary

not a permanent alias swamp

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/feature-boundary.md

7. Folder Growth Guard Compliance

Keep FE-FOLDER-001/002 active.

After migration:

flat admin debt must not increase

admin-api capability-specific debt must shrink

no new direct feature implementation under protected flat admin paths

No baseline widening.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/folder-guards.md

8. Server/Client Boundary Safety

This is admin-focused.

Do not opportunistically convert server/client components.

Verify:

no unnecessary new use client

no client-boundary expansion outside selected slice

no storefront SSR/SEO regression

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/client-boundary.md

9. Source-Size / God-File Safety

No new handwritten source >800 LOC.
No oversized file may grow beyond baseline.

If selected capability unexpectedly requires structural changes in a critical giant:
ABORT that capability selection and choose another safe slice.

Do NOT split god-components here.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/source-size-compliance.md

10. Test Discovery Guard

Use the canonical discovery mechanism from FE-F1.

Verify:

selected capability tests are discovered automatically

architecture tests remain separate/intentional if designed that way

no test file silently omitted

discovered count is recorded

Do not reintroduce manual enumeration.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/test-discovery.md

11. Validation

Required:

selected capability characterization tests

npm run test:architecture

canonical discovered frontend test command

TypeScript check with comparison to known legacy baseline

lint/build if canonical and bounded

Pre-existing FE failures:

known bg-white critical-storefront issue

known legacy typecheck failures

Do not suppress or broadly repair unrelated legacy failures in this task.
Prove no NEW failure signature/count was introduced.

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/tests.md

12. Migration Pattern Review

After the second feature migration, assess whether the pattern is now proven enough for repeated admin migration.

Return:
Admin-Migration-Pattern: PROVEN
or
Admin-Migration-Pattern: NEEDS_ADJUSTMENT

Evaluate:

route thinness

feature public boundary

API ownership

test discovery

import stability

folder guard usefulness

migration friction

compatibility aliases

Evidence:
docs/evidence/TB-TMAR-FE-ADMIN-W1/pattern-review.md

13. Decide Next Frontend Strategy

If Admin-Migration-Pattern: PROVEN, choose one:

A. TB-TMAR-FE-ADMIN-W2
for another admin capability migration if high-value debt remains and pattern is stable.

B. TB-TMAR-FE-GODFILE-W1
if flat migration now reaches a point where a critical giant blocks further admin cleanup AND characterization coverage exists.

C. TB-TMAR-HOST-W3
if frontend can safely pause and Host debt now has higher architectural value.

D. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if frontend structure is sufficiently protected and checkout design now has higher value.

Do not ask the user.

14. Product / Visual Safety

No visual redesign.
No product behavior change.

Do NOT alter:

Shopeiva fidelity

menus

sliders

rails

PDP/PLP

localization copy

routes

API contracts

permissions semantics

15. Recovery State

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if a new durable lock is added

docs/architecture/TOOBA-CAPABILITY-MAP.md only if ownership knowledge materially changes

docs/evidence/TB-TMAR-FE-ADMIN-W1/recovery-sot.md

Record:

selected migrated capability

admin-api debt before/after

migration-pattern status

next recommended task

16. Acceptance Criteria

PASS only if:

exactly one additional admin capability migrated

admin-api capability debt measurably shrinks

feature public boundary is clean

no deep external imports to migrated feature internals

flat-folder guards remain green

no god-file decomposition

no product/visual behavior change

no client-boundary expansion

tests discovered automatically

no NEW type/test/build regressions

migration pattern assessed

user work preserved

next task selected automatically

canonical Result delivered

Worker stops completely

17. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Slice-Selection
Characterization-Tests
Migration
Admin-Api-Shrink
Feature-Boundary
Folder-Guards
Client-Boundary
Source-Size-Compliance
Test-Discovery
Tests
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
