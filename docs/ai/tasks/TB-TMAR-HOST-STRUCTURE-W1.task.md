PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-HOST-STRUCTURE-W1

Parent-Task:
TB-TMAR-CHECKOUT-IMPL-W3

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
TMAR Host Structure Wave 1 — Inventory, Classify, and Safely Reorganize Flat Host Source Structure

Task Type:
STRUCTURAL REFACTOR — MOVE-ONLY / OWNERSHIP-PRESERVING / NO BEHAVIOR CHANGE

0. Architect Intent

CHECKOUT-IMPL-W3 is accepted.

Verified state:

Cart conversion is behind Tooba.Cart.Contracts

Order.Application → Cart.Application was removed

shared TransactionScope remains intact

Checkout-Implementation-W4-Readiness = READY

W4 candidate = Order.Infrastructure → Inventory.Contracts cleanup for Cancel/Restore/PaymentBridge

Orders frontend still waits for backend W4

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

However, live visual evidence from the repository shows Tooba.Host has severe flat physical source organization debt:
many unrelated concerns are co-located at project root, including authentication, authorization, cache, messaging, outbox, hosted services, persistence helpers, tenancy, health, reservation coordination, exception handling, development bootstrap, and runtime log artifacts.

This task addresses that structural debt NOW, before resuming CHECKOUT-IMPL-W4.

Primary objectives:

inventory the entire current Tooba.Host root and subfolders

classify every root-level file by actual responsibility and ownership

define a conservative target folder map

move ONLY structurally safe files into coherent folders

preserve namespaces/behavior/public types unless a namespace change is mechanically required and fully safe

remove source-tree log artifacts from normal source layout if safe, with correct ignore/runtime handling

add guards preventing future root-level dumping

do NOT change business behavior

do NOT resume Checkout implementation in this task

This is not a cosmetic rename wave. It is a physical-architecture recovery task backed by ownership evidence.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected previous accepted tip:
c17a67d80ad3af59b4aee2b0a807814339394509

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/recovery-sot.md

latest Host evidence from HOST-W3 through HOST-W6

Verify:

branch main

exact HEAD

exact origin/main

HEAD == origin/main

18ca10c9 ancestor

git status --short

git diff --name-only

git diff --cached --name-only

user work preserved

If tracked user work conflicts:
STOP with RECOVERY_CONFLICT.

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/recovery-start.md

2. Full Host Physical Inventory

Inspect the complete Tooba.Host project.

Inventory:

every root-level .cs

every existing subfolder

Program.cs

configuration files

generated/runtime artifacts

.log, .err.log, temp/debug files

test project adjacency only for reference

For each root-level source file record:

filename

namespace

responsibility

architectural concern

runtime role

dependency direction

whether it is safe to move physically without semantic refactor

target folder candidate

confidence

Classify responsibilities at minimum into:

Composition

Authentication

Authorization

Security

Caching

Messaging

Outbox

Transport

Persistence

MultiTenancy

Health

Errors

Jobs

Reservations

Development

Configuration

Other / NeedsReview

Produce:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/host-file-inventory.md
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/host-file-inventory.json

3. Root-Level Log Artifact Audit

Audit files such as:

host-dev.log

host-dev.err.log

host-r1-dev.log

host-r2-dev.log

host-t*.log

host-t*.err.log
and any similar runtime artifacts.

For each:

tracked or untracked

producer/process

whether runtime still depends on exact path

whether it belongs in source control

whether .gitignore already covers it

safest disposition

Preferred outcome where safe:

source tree does not retain runtime log artifacts

runtime log location is outside source tree or under an intentionally ignored runtime/logs path

.gitignore prevents recurrence

Do NOT delete tracked/user-needed diagnostic artifacts blindly.
If uncertain, preserve and document.

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/log-artifact-audit.md

4. Target Host Folder Map

Design a conservative target map from evidence.

Expected shape MAY resemble:

Tooba.Host/
Composition/
Authentication/
Authorization/
Security/
Caching/
Messaging/
Outbox/
Transport/
Persistence/
MultiTenancy/
Health/
Errors/
Jobs/
Reservations/
Development/
Configuration/
Program.cs
appsettings.json

But do NOT force this exact map if live ownership evidence suggests better grouping.

Rules:

folders reflect responsibility, not arbitrary filename prefixes

avoid too-deep folder nesting

avoid Common, Misc, Helpers

avoid fake bounded-context ownership inside Host

Host remains transport/composition/platform edge

module business logic must not be moved into Host folders as a substitute for real module ownership

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/target-folder-map.md

5. Safe Move Set Selection

Do NOT move everything blindly in one wave.

Select a safe coherent subset of root-level files that:

have clear responsibility

have no ambiguous ownership

do not require behavioral refactor

do not require cross-module semantic changes

do not intersect risky active checkout logic

Preferred first-wave groups:

Authentication / Authorization / Security

Caching

Health / Errors

Configuration

Messaging / Outbox / Transport

clearly isolated Jobs

Avoid moving:

ambiguous business orchestration

files whose real ownership should leave Host entirely

active checkout/order workflow code

anything requiring semantic redesign

Produce exact move manifest:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/move-manifest.md

6. Physical Moves

Perform move-only refactor for selected safe subset.

Rules:

preserve behavior

preserve public type names

preserve DI registration behavior

preserve runtime discovery/reflection behavior

preserve file-scoped namespace if possible

namespace may remain unchanged when physical move does not require namespace change

if namespace change is necessary, update all references mechanically and test

Do NOT:

rename classes for style

change method signatures

change dependency injection semantics

rewrite logic

combine files

split files

alter business rules

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/moves.md

7. Program / Composition Safety

Ensure:

Program.cs remains thin

registration order unchanged unless proven irrelevant

hosted service registration order preserved

messaging/outbox registration preserved

auth/authz/security middleware order preserved

health/readiness behavior preserved

tenant resolution ordering preserved

Do not opportunistically refactor Program.cs beyond path/reference adjustments needed for moves.

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/composition-safety.md

8. Namespace Strategy

Document one of:

Host-Namespace-Strategy: PRESERVE_EXISTING
or
Host-Namespace-Strategy: ALIGN_WITH_FOLDERS

Default preference for W1:
PRESERVE_EXISTING, unless alignment is mechanically safe and clearly improves maintainability without churn.

Avoid broad namespace churn just because folders changed.

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/namespace-strategy.md

9. Root-Dumping Guard

Add an architecture guard preventing future uncontrolled root-level Host growth.

Guard should:

allow explicitly approved root files such as Program.cs

allow configuration files as appropriate

reject new arbitrary .cs files at Host root unless added to a small reviewed allowlist

not require updating a giant baseline for every valid file

Name durable lock:
HOST-FOLDER-001

Meaning:
New Host production source must live under an approved responsibility folder unless explicitly exempted by architecture.

Add to:
docs/architecture/TMAR-architecture-locks.md

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/folder-guard.md

10. Logging Artifact Guard

If log artifacts are confirmed disposable/runtime-generated:

update .gitignore narrowly

add test/guard if practical so *.log / *.err.log do not become tracked under Host source

Do NOT globally ignore potentially meaningful fixtures elsewhere without evidence.

Durable lock if justified:
HOST-HYGIENE-001

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/log-guard.md

11. Source-Size and Ownership Safety

No business logic changes.

No new file >800 LOC.
No god-file growth.
No module ownership reassignment.

If a file is discovered to be business logic incorrectly living in Host:

classify as SHOULD_LEAVE_HOST

do not merely hide it under a prettier folder

defer to a later semantic extraction task

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/ownership-findings.md

12. Tests / Validation

Required:

full Tooba.Host compile

Tooba.Host.Tests relevant suite

architecture tests

TMAR foundation tests

auth/authz tests if moved

messaging/outbox tests if moved

health/readiness tests if moved

source-size guard

new folder guard

no new warnings/errors attributable to moves

Compare behavior before/after.

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/tests.md

13. Host Structure Exit State

Return exactly:
Host-Structure-State: CONTINUE_STRUCTURE_WAVES
or
Host-Structure-State: READY_TO_PAUSE

CONTINUE only if:

significant safe root-level structural debt remains

another move-only wave has high value

no semantic ownership redesign is required

READY_TO_PAUSE if:

remaining root files are explicitly allowed, ambiguous, or require semantic extraction rather than folder movement

folder guard prevents regression

continuing move-only work has low marginal value

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/structure-state.md

14. Checkout Resume Readiness

Confirm the structural refactor did not disturb Checkout W3 state.

Return:
Checkout-W4-Resume-Readiness: READY
or
Checkout-W4-Resume-Readiness: BLOCKED

READY requires:

W3 tests remain green

no checkout/order/cart/inventory semantic files changed except reference/path mechanics if unavoidable

shared TransactionScope unchanged

Process Manager unchanged semantically

W4 candidate remains valid

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/checkout-resume-readiness.md

15. Next Task Decision

Choose automatically:

A. TB-TMAR-HOST-STRUCTURE-W2
only if Host-Structure-State = CONTINUE_STRUCTURE_WAVES and another safe move-only wave clearly remains high-value.

B. TB-TMAR-CHECKOUT-IMPL-W4
if Host-Structure-State = READY_TO_PAUSE and Checkout-W4-Resume-Readiness = READY.

C. TB-TMAR-CONTRACTS-W7
only if structural audit exposes a prerequisite boundary problem that blocks both.

Do not ask the user.

16. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md only if ownership facts materially change

docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/recovery-sot.md

Record:

root inventory

target folder map

files moved

files deliberately not moved

log artifact findings

new folder/hygiene guards

Host-Structure-State

Checkout-W4-Resume-Readiness

next task

17. Acceptance Criteria

PASS only if:

full Host root inventory exists

target folder map is evidence-based

only safe files are moved

no business behavior changes

ambiguous business ownership is not hidden by folders

runtime/log artifacts are audited

root-dumping guard exists

tests pass

no new failures

user work preserved

Checkout W3 state remains intact

next task selected automatically

canonical Result delivered

Worker stops completely

18. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Host-File-Inventory
Log-Artifact-Audit
Target-Folder-Map
Move-Manifest
Moves
Composition-Safety
Host-Namespace-Strategy
Folder-Guard
Log-Guard
Ownership-Findings
Tests
Host-Structure-State
Checkout-W4-Resume-Readiness
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