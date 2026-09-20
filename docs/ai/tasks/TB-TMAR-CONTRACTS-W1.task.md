PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CONTRACTS-W1

Parent-Task:
TB-TMAR-BOUNDARY-V1-R1

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
TMAR Contracts Wave 1 — Establish First Public Module Contracts and Remove Confirmed Structural Leakage

Task Type:
IMPLEMENTATION — NARROW BOUNDARY REPAIR

0. Architect Intent

Boundary verification is complete and accepted.

Confirmed structural debt:

Cart.Domain → Offer.Domain (dead ProjectReference)

Order.Domain → Offer.Domain (dead ProjectReference)

Pricing.Domain → Offer.Domain (dead ProjectReference)

Payment.Infrastructure → Wallet.Domain (live type usage)

Order.Application has multiple foreign Application references

33 Infrastructure → foreign Application edges are now frozen

55 oversized source files are frozen against growth

Product development remains NOT_YET.

This task starts Contracts extraction with the smallest high-value slice.

Primary objectives:

remove the three dead Domain→Offer.Domain ProjectReferences

create the first proper Tooba.Offer.Contracts boundary if required by verified consumers

create the first proper Tooba.Wallet.Contracts boundary for the live Payment→Wallet dependency

migrate ONLY the exact verified boundary types needed for these edges

shrink architecture debt baselines

do not redesign Order orchestration yet

No Big Bang Contracts rollout.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/evidence/TB-TMAR-BOUNDARY-V1/dependency-verification.md

docs/evidence/TB-TMAR-BOUNDARY-V1/source-coupling.md

docs/evidence/TB-TMAR-BOUNDARY-V1/contracts-sequence.md

docs/evidence/TB-TMAR-BOUNDARY-V1-R1/recovery-sot.md

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD==origin/main

git status --short

git diff --name-only

git diff --cached --name-only

confirm 18ca10c9 ancestor

Expected previous accepted tip:
2056f42081fe440167ec8b5d7d5d07f98f7d5003

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W1/recovery-start.md

2. Remove Dead Domain → Offer.Domain References First

The independent review and BOUNDARY-V1 confirmed these project references are structurally present but source-level unused:

Cart.Domain → Offer.Domain

Order.Domain → Offer.Domain

Pricing.Domain → Offer.Domain

Verify again before editing.

If still unused:

remove the <ProjectReference> entries

do NOT add replacement references

do NOT move types unnecessarily

ensure compile/test proves they were truly dead

Update/shrink:
tmar-domain-to-foreign-domain.json

Expected result:
these three Domain→Offer.Domain baseline entries disappear.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W1/dead-domain-offer-removal.md

3. Create Tooba.Offer.Contracts Only If Verified Necessary

Use the BOUNDARY-V1 Contracts sequence and current consumers.

Create:
Tooba.Offer.Contracts

ONLY if there are verified cross-module Offer boundary types/interfaces that currently force foreign Application/Domain coupling.

Contracts project rules:

no EF

no Infrastructure

no Host

no domain entities leaking across module boundary

no implementation classes

minimal stable DTOs/value contracts/interfaces only

Do NOT clone the entire Offer.Application API.

If no Offer contract extraction is actually needed after dead Domain refs are removed, document that and do not create unnecessary project.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W1/offer-contracts.md

4. Create Tooba.Wallet.Contracts for Live Payment → Wallet Coupling

Confirmed live coupling:
Payment.Infrastructure → Wallet.Domain
using:
WalletAccount.NormalizeCurrency

This is not acceptable as a future microservice boundary.

Design the smallest stable boundary.

Important:
Do NOT expose WalletAccount through Contracts.

Preferred direction:

move/share a semantic currency normalization contract/value primitive at the correct boundary
OR

expose a Wallet-owned contract/service if the behavior truly belongs to Wallet
OR

move the normalization primitive to BuildingBlocks only if it is genuinely domain-neutral and used across multiple bounded contexts

Do not choose placement by convenience.

Before implementation document:

who owns currency normalization invariant?

is it Wallet-specific or shared financial primitive?

why the selected location is correct for future service extraction?

Then remove:
Payment.Infrastructure → Wallet.Domain

Update/shrink:
tmar-infra-to-foreign-domain.json

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W1/wallet-contracts.md

5. Dependency Direction After This Task

Target for touched edges:

Domain:

no foreign Domain dependency

Application:

may depend on foreign *.Contracts

must not gain new foreign Application dependency

Infrastructure:

may implement local Application/Contracts abstractions

may consume foreign *.Contracts where necessary

must not depend on foreign Domain/Infrastructure

must not add new foreign Application edges

No new App→App edges.
No new Infra→foreign Application edges.

6. Do NOT Refactor Order.Application Yet

Order.Application hub analysis is already available.

This task MUST NOT:

redesign checkout orchestration

introduce a Saga

convert all six foreign dependencies

change consistency semantics

replace sync calls with events broadly

Only prepare the Contracts foundation required by this Wave.

Order hub decomposition is a later task.

7. MediatR / CQRS Compliance

Any NEW use-case introduced in this task must follow:

MediatR 12.5.0

Handler in Application

FluentValidation where request validation is needed

IClock/IIdGenerator where applicable

Do not introduce Directories as new top-level use-case pattern.

Existing untouched legacy remains for later migration.

8. Error / Locale Compliance

All NEW code:

semantic errors only

no hardcoded localized user-facing messages in ANY language inside Domain

unlimited-locale safe

no FA/EN-only assumptions

Do not mass-migrate unrelated errors.

9. Source-Size Guard Compliance

Do not create new hand-written source files >800 LOC.

Existing oversized files must not grow above baseline.

If touching an oversized legacy file:

keep diff minimal

do not perform unrelated cleanup

if decomposition is necessary, add characterization tests first

Expected:
no giant-file decomposition needed in this task.

10. Architecture Guards

Update exact baselines so they SHRINK:

Expected candidates:

remove the 3 Domain→Offer.Domain entries

remove Payment.Infrastructure→Wallet.Domain entry

Do not add wildcard exceptions.

Add/adjust architecture tests to ensure:

no foreign Domain ProjectReference can be reintroduced

Payment.Infrastructure no longer references Wallet.Domain

touched Contracts projects remain dependency-clean

Contracts do not reference Domain/Infrastructure/Host

no new App→App edge

no new Infra→foreign Application edge

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W1/architecture-guards.md

11. Tests

Required:

For dead-reference removal:

affected Cart.Domain/Application compile

affected Order.Domain/Application compile

affected Pricing.Domain/Application compile

For Wallet boundary:

tests covering currency normalization behavior

Payment affected tests

Wallet affected tests

For Contracts:

architecture dependency tests

ArchitectureBoundaryTests

TMAR foundation tests

source-size guard tests

If current behavior has no focused test:
add focused characterization test BEFORE moving the behavior.

Do not create full unit-test projects for all modules.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W1/tests.md

12. Product Resume Safety

At completion return exactly:

Product-Resume-Safety: NOT_YET

or

Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

SAFE_WITH_TMAR_PARALLEL requires:

critical dependency expansion is frozen

confirmed Domain→Domain leaks addressed or isolated

live Payment→Wallet Domain coupling removed

new product code can use Contracts/CQRS without expanding old coupling

no remaining unknown critical boundary contradiction

If Order.Application hub is known but frozen and can be avoided by new features, SAFE may be justified.
If not, return NOT_YET.

Do not ask the user.

13. Next Task Decision

Choose automatically:

A. TB-TMAR-CONTRACTS-W2
if the next highest-value App→App/Infra→App contract extraction is required before product resume.

B. TB-TMAR-HOST-W3
if boundaries are sufficiently safe and Host write debt is now the limiting issue.

C. TB-TMAR-ORDER-BOUNDARY-W1
if Order.Application hub must be addressed before safe product resume.

Do not ask the user.

14. Capability Map / Bootstrap / Recovery SoT

Update narrowly:

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/evidence/TB-TMAR-CONTRACTS-W1/recovery-sot.md

Record:

exact removed project-reference debt

created Contracts projects

baseline shrink

Last Verified Task = TB-TMAR-CONTRACTS-W1

15. No Scope Creep

Do NOT:

create Contracts projects for every module

refactor all 16 App→App edges

refactor all 33 Infra→App edges

rewrite Order orchestration

move Domain ownership

reorganize folders

split God files

install Redis

rewrite Git history

alter frontend

change product behavior

16. Acceptance Criteria

PASS only if:

all 3 dead Domain→Offer refs removed if still unused

no replacement foreign Domain dependency introduced

Payment.Infrastructure→Wallet.Domain live coupling removed

any created Contracts project is minimal and clean

no foreign Domain entity leaks through Contracts

architecture baselines shrink

no new App→App or Infra→foreign Application edge

focused characterization/tests pass

source-size guards remain green

no broad refactor

Product-Resume-Safety assessed

next task chosen automatically

user work preserved

canonical Result delivered

Worker stops completely

17. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Dead-Domain-Offer-Refs
Offer-Contracts
Wallet-Contracts
Currency-Ownership
Dependency-Changes
Architecture-Guards
Characterization-Tests
Tests
Source-Size-Compliance
Capability-Map
Bootstrap
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Next-Recommended-Task

After canonical Result through Bridge:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK