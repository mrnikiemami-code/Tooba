PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-FND-001

Parent-Task:
TB-TMAR-ARCH-BASELINE

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
TMAR Foundation — CQRS/MediatR, Deterministic Abstractions, Error Foundation, and Architecture Freeze Guards

Task Type:
IMPLEMENTATION — FOUNDATION ONLY

Architect Decision

TB-TMAR-ARCH-BASELINE is accepted as the architecture recovery baseline.

This task establishes the NEW architectural foundation and freezes further drift.

This task MUST NOT perform the large migration yet.

After this task:

new use-cases must follow CQRS + MediatR

new validation must use FluentValidation pipeline

new direct Host business writes/decisions must not expand

new cross-module Application→Application coupling must not expand

new direct system-clock / UUID implementation usage in protected layers must not expand

new localized Domain error strings must not expand

new direct IMemoryCache bypasses must not expand

Existing legacy debt remains explicitly baselined for later TMAR waves.

0. Recovery / Git Safety — MANDATORY

Repository:
D:\Users\User\source\repos\SarvNewVer

Before modifying anything, record:

current branch

exact HEAD SHA

exact origin/main SHA

HEAD==origin/main YES/NO

git status --short

git diff --name-only

git diff --cached --name-only

confirm 18ca10c9 is ancestor of HEAD

confirm user work is preserved

The previous planning result reported tip undefined.
That ambiguity MUST be eliminated here.

If HEAD != origin/main unexpectedly, or tracked user changes conflict with this task:
STOP and return RECOVERY_CONFLICT.

Forbidden:

git reset

git clean

checkout/restore over user work

unsafe rebase

blind stash manipulation

deleting .tmp-*

broad git add .

Evidence:
docs/evidence/TB-TMAR-FND-001/recovery-start.md

1. Read the Approved TMAR Baseline

Read only the architecture evidence needed for this task:

docs/evidence/TB-TMAR-ARCH-BASELINE/current-state.md

docs/evidence/TB-TMAR-ARCH-BASELINE/cqrs-mediatr-plan.md

docs/evidence/TB-TMAR-ARCH-BASELINE/host-freeze.md

docs/evidence/TB-TMAR-ARCH-BASELINE/contracts-plan.md

docs/evidence/TB-TMAR-ARCH-BASELINE/clock-plan.md

docs/evidence/TB-TMAR-ARCH-BASELINE/id-generation-plan.md

docs/evidence/TB-TMAR-ARCH-BASELINE/error-localization-plan.md

docs/evidence/TB-TMAR-ARCH-BASELINE/locale-policy.md

docs/evidence/TB-TMAR-ARCH-BASELINE/cache-plan.md

docs/evidence/TB-TMAR-ARCH-BASELINE/architecture-tests-plan.md

docs/evidence/TB-TMAR-ARCH-BASELINE/proposed-locks.md

docs/evidence/TB-TMAR-ARCH-BASELINE/priority-matrix.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

Do NOT repeat the full repository audit.

2. Canonicalize TMAR Architecture Locks

Create/update a durable architecture lock document under:

docs/architecture/

Use a clear name such as:
TMAR-architecture-locks.md

Canonicalize the approved rules from the baseline, including at minimum:

ARCH-OWN-001
Domain ownership is determined by bounded-context invariant/lifecycle, never persistence convenience.

ARCH-CONTRACT-001
Cross-module business boundaries move through Tooba.<Module>.Contracts; foreign Application/Infrastructure is not the intended public boundary.

ARCH-DOMAIN-001
Domain errors are semantic/error-code based; localized FA/EN user-facing text is forbidden in Domain.

ARCH-TIME-001
Application/Infrastructure orchestration uses IClock; pure Domain methods may receive explicit now.

ARCH-ID-001
ID generation uses an approved abstraction at orchestration boundaries; direct implementation calls are forbidden in protected Domain/Application paths.

ARCH-HOST-001
Host is transport/composition root only; no NEW business write, transaction, pricing/inventory/seller/campaign decision, or Domain ownership.

ARCH-READ-001
NEW cross-module read composition uses declared read contracts/gateways; direct foreign DbContext composition is legacy-only and must not expand.

ARCH-DB-001
Each module owns schema/DbContext/migrations; no cross-schema FK/JOIN or foreign-module business-write DbContext access.

ARCH-CQRS-001
All NEW application use-cases use CQRS + MediatR Handler.

ARCH-CQRS-002
Approved MediatR version is EXACTLY 12.5.0.

ARCH-VAL-001
NEW request validation uses FluentValidation through MediatR pipeline.

ARCH-LOCALE-001
Locale normalization/fallback is centralized and unlimited-locale safe.

ARCH-CACHE-001
NEW cache consumption uses ICache; direct IMemoryCache use must not expand.

ARCH-FOLDER-001
Physical folder moves happen only after ownership/dependency repair.

LOCK-SF-374 and all existing Storefront locks remain intact.

Evidence:
docs/evidence/TB-TMAR-FND-001/canonical-locks.md

3. Install MediatR 12.5.0 — Exact Version

Add MediatR EXACTLY:

MediatR version 12.5.0

Do not use 13.x or any later commercial/dual-license line.

Respect the repository's existing package-management style:

if central package management exists, use it

do not scatter versions across projects unnecessarily

Add only the minimum project references/package references needed to establish the Application foundation.

Do not mass-convert existing Directories.

Evidence:
docs/evidence/TB-TMAR-FND-001/mediatr-install.md

4. Add FluentValidation Foundation

Add FluentValidation using the repository's package-management conventions.

Implement MediatR validation pipeline support.

Required behavior:

validators run before handler execution

validation failures are deterministic

no localized hardcoded Domain messages

validation result can later map cleanly to localized ProblemDetails

no duplicate endpoint-only validation pattern for NEW MediatR use-cases

Do NOT migrate every current validator in this task.

Evidence:
docs/evidence/TB-TMAR-FND-001/validation-foundation.md

5. Add IClock

Create one canonical time abstraction in the appropriate shared/BuildingBlocks layer.

Requirements:

Application code can depend on it without depending on Infrastructure

Infrastructure provides the system implementation

tests can provide fixed/fake implementation

canonical UTC time type must be documented

Domain entities do NOT receive IClock injection

pure Domain methods may continue accepting explicit now

Do not rewrite all existing clock calls now.

Add architecture guard so NEW direct system-time use in protected layers cannot expand.

Protected patterns include, as applicable:

DateTime.UtcNow

DateTimeOffset.UtcNow

DateTime.Now

concrete/system clock calls

Existing violations must be baselined explicitly, not silently suppressed.

Evidence:
docs/evidence/TB-TMAR-FND-001/clock-foundation.md

6. Add IIdGenerator / UUID Abstraction

Create one canonical ID-generation abstraction.

Requirements:

supports Tooba's UUIDv7 requirement

Application orchestration can request a new ID

Infrastructure/shared implementation may internally use UuidV7.New()

Domain should receive generated IDs explicitly when appropriate

deterministic/fake generator possible in tests

Do NOT mass-replace existing ID calls now.

Add architecture guard so NEW direct Guid.NewGuid() / UuidV7.New() usage in protected Domain/Application paths cannot expand.

Existing occurrences must be explicitly baselined.

Evidence:
docs/evidence/TB-TMAR-FND-001/id-foundation.md

7. Minimal Semantic Error Foundation

Establish the smallest reusable semantic error primitive required by TMAR.

The exact design must fit current BuildingBlocks conventions, but it MUST support:

stable error code, e.g. catalog.category.invalid_slug

optional structured metadata/arguments

no requirement for localized user-facing message in Domain

future mapping to ProblemDetails at HTTP boundary

future localization by locale

Do NOT migrate the entire repository's exceptions in this task.

Do NOT hardcode only FA/EN.
The design must be locale-agnostic.

Add a guard/baseline plan preventing NEW obviously localized user-facing exception strings in Domain from expanding.

Do not overengineer:

no giant error framework

no translation database redesign

no full exception migration

Evidence:
docs/evidence/TB-TMAR-FND-001/error-foundation.md

8. MediatR Pipeline Foundation

Provide reusable MediatR pipeline behaviors in the correct shared/Application foundation location.

Minimum:

Validation behavior

Logging/telemetry behavior

Design/implement transaction behavior only if it can be added safely without changing current transaction semantics.
If not safe, document and defer it; do NOT introduce a fake/global transaction behavior.

Authorization and idempotency:

define extension points/interfaces only if current architecture already supports them cleanly

do not invent a generic system that changes behavior

Pipeline must not bypass existing SpiceDB/authorization locks.

Evidence:
docs/evidence/TB-TMAR-FND-001/pipeline-foundation.md

9. Prove the Foundation Without Mass Migration

Do NOT convert dozens of existing use-cases.

Prove registration and execution with one of these, in priority order:

A. test-only Command/Query + Handler exercising the pipeline, OR
B. one genuinely low-risk existing read-only use-case if a test-only proof is insufficient

Preference:
use tests rather than production migration.

Proof must demonstrate:

ISender resolves

handler executes

validator executes

validation short-circuits invalid request

logging/telemetry behavior is wired

no business behavior regression

Evidence:
docs/evidence/TB-TMAR-FND-001/mediatr-proof.md

10. Architecture Freeze Guards — NEW DEBT MUST FAIL

This is a critical part of the task.

Implement architecture tests/guards that allow CURRENT known legacy debt but reject NEW violations.

DO NOT use broad suppressions.

Use an explicit legacy-debt baseline/manifest at stable granularity such as:

rule key

project/file

member/symbol where practical

normalized violation identity

Avoid line-number-only baselines.

At minimum freeze:

A. Host debt

Reject NEW:

business DbContext writes

SaveChanges

BeginTransaction

business pricing/inventory/seller/campaign decisions

direct foreign-module DbContext composition beyond existing baseline

B. Cross-module references

Current App→App debt was reported as 16 edges.

Freeze the current graph:

no NEW App→App project-reference edge

later Contracts tasks will reduce the baseline

C. Domain purity

Reject NEW protected-layer:

direct system time

direct Guid.NewGuid/UuidV7.New

obvious localized Domain user-facing exception text

D. Cache

Reject NEW direct IMemoryCache consumption outside approved provider/legacy baseline.

E. Existing core boundaries

Keep existing tests for:

Domain ↛ Infrastructure/Host

Application ↛ foreign Infrastructure

Infra A ↛ Infra B

no global business DbContext

no cross-schema FK/JOIN rules already present

A baseline file is acceptable only if it is explicit, reviewable, and intended to shrink during TMAR.
It must not become a permanent wildcard exemption.

Evidence:
docs/evidence/TB-TMAR-FND-001/architecture-freeze-guards.md

11. Do NOT Create Per-Module Contracts Yet

This task must NOT create 31 Contracts projects.

Only:

canonicalize the rule

freeze NEW App→App edges

prepare the foundation for later extraction

Contracts extraction is a separate TMAR task.

12. Do NOT Move Domain Ownership Yet

Do not move:

StoreAppearance*

StoreLandingPage*

StoreMenu*

StoreCheckout*

any other Domain types

The repository-wide ownership map from the baseline remains the source for later ownership tasks.

No namespace/folder churn here.

13. Do NOT Refactor Host Yet

No broad Host cleanup in this task.

Only:

freeze NEW debt

add architecture guards

Actual Host write removal is a later wave.

14. Cache — No Redis Yet

Do not install Redis.
Do not rewrite all cache consumers.

Only enforce:

NEW direct IMemoryCache bypass cannot expand

existing ICache foundation remains canonical

Redis provider remains deferred.

15. Tests

Required:

solution/build for affected projects

MediatR registration test

handler execution test

FluentValidation behavior test

IClock fixed/fake test

IIdGenerator deterministic/fake test

semantic error primitive test

architecture freeze tests

existing ArchitectureBoundaryTests

focused existing tests around any touched shared infrastructure

No unrelated broad test suite unless needed.

All failures must be classified:

caused by task

pre-existing

environment

Do not suppress failures.

Evidence:
docs/evidence/TB-TMAR-FND-001/tests.md

16. Capability Map

This task materially changes architecture foundation.

Update ONLY relevant sections of:

docs/architecture/TOOBA-CAPABILITY-MAP.md

Record:

MediatR 12.5.0 foundation established

FluentValidation pipeline established

IClock established

IIdGenerator established

semantic error foundation established

architecture freeze guards active

legacy debt remains baselined for TMAR migration

Last Verified Task = TB-TMAR-FND-001

Do not rewrite unrelated capability sections.

17. Recovery SoT

At completion record:

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Phase:
Foundation

Last Accepted Architecture Baseline:
TB-TMAR-ARCH-BASELINE

Active Implementation:
TB-TMAR-FND-001

Next Expected Wave:
Host dangerous-write removal OR exact next task justified by current evidence

Branch:
main

HEAD SHA:
<exact>

origin/main SHA:
<exact>

HEAD==origin/main:
YES/NO

18ca10c9 ancestor:
YES/NO

User work preserved:
YES/NO

Evidence:
docs/evidence/TB-TMAR-FND-001/recovery-sot.md

18. Anti-Pattern Gate

FAIL the task if implementation introduces:

MediatR 13+

Big Bang CQRS migration

mass Directory deletion

31 Contracts projects

Host refactor wave

Domain ownership moves

folder reorganization

Redis

IClock injected into every Domain entity

interface-per-pure-function

localized Domain user-facing text

blanket architecture-test suppression

wildcard legacy allowlists

changing business behavior to make tests pass

direct foreign-module write DbContext access

cross-schema FK/JOIN

destructive git recovery

Evidence:
docs/evidence/TB-TMAR-FND-001/antipattern-scan.md

19. Acceptance Criteria

PASS only if ALL are true:

exact Git SHAs recorded; tip undefined eliminated

MediatR EXACTLY 12.5.0 installed

FluentValidation pipeline foundation works

IClock exists with system + deterministic test implementation

ID abstraction exists with UUIDv7 implementation + deterministic test support

minimal semantic error-code foundation exists

MediatR pipeline is proven by tests

NEW Host debt is frozen by architecture guard

NEW App→App edges are frozen

NEW protected direct clock/ID usage is frozen

NEW direct IMemoryCache bypass is frozen

existing legacy violations are explicitly baselined, not hidden

no mass CQRS migration

no Host cleanup wave

no Contracts extraction wave

no Domain ownership moves

no folder moves

no Redis

existing business behavior preserved

capability map updated narrowly

canonical TMAR locks persisted

user work preserved

result sent through Bridge

Worker stops completely

20. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Applicable-Locks
Recovery-Start
Git-Baseline
MediatR
FluentValidation
Clock-Foundation
Id-Foundation
Error-Foundation
Pipeline-Foundation
MediatR-Proof
Architecture-Freeze-Guards
Legacy-Debt-Baseline
Tests
Capability-Map
Recovery-SoT
AntiPattern-Scan
Changed-Files
Git
Architectural-Concerns
Blockers
Next-Recommended-Task

Do not include Worker-IDLE.

After canonical Result through Bridge:
STOP completely.

Do NOT:

poll

fetch next task

continue to Host cleanup

continue to Contracts extraction

continue to Domain moves

write Worker IDLE

END_TOOBA_TASK