PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-OFFER-REFERENCE-W1-R2

Parent-Task:
TB-TMAR-OFFER-REFERENCE-W1-R1

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
REPAIR

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Track:
OFFER_REFERENCE_MODULE

Title:
Offer Golden Module Residual Repair — Semantic Errors, Determinism, Boundary Purity, Full Residual Scan

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Architect Reopen Decision

Offer was previously marked COMPLETE_REFERENCE_PATTERN, but user inspection found a concrete production Domain violation in SellerOffer.Activate(...):

throw new InvalidOperationException("Offer بایگانی‌شده دوباره فعال نمی‌شود؛ listing جدید بسازید.");

The same aggregate also uses string-based InvalidOperationException codes and directly calls UuidV7.New().

Therefore the prior COMPLETE state is REOPENED.

This repair MUST NOT fix only that one visible line.
Perform a full residual scan of the entire Offer module and eliminate every remaining violation that contradicts the Golden Module standard.

Do not switch modules.
Do not touch frontend.
Do not resume Checkout.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Verify branch/main, HEAD==origin/main, no unexpected tracked changes, staged=0, stashes untouched, user work preserved, protected ancestor 18ca10c9 still ancestor.

If unexpected divergence exists: STOP with RECOVERY_CONFLICT.

Never use git reset/clean/destructive checkout/restore/unsafe rebase/blind stash/broad git add ..

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/recovery-start.md

2. Known SellerOffer defects

Inspect Tooba.Offer.Domain.Aggregates.SellerOffer.

A. Remove hardcoded localized Domain exception text.
Domain/Application must not own localized user-facing prose.

B. Replace arbitrary string-based business InvalidOperationException signaling:

offer.min_quantity.invalid

offer.max_quantity.invalid

offer.min_quantity.exceeds_max

archived-offer reactivation failure

Use the already accepted canonical semantic/domain error mechanism from the repo.
Do NOT invent a second framework.

C. Audit UuidV7.New() inside Domain.
If accepted TMAR rules require IIdGenerator, move ID generation to Application and pass ID into Domain creation.
If current accepted architecture explicitly allows Domain UUID generation, prove that from durable rules before keeping it.

D. Preserve deterministic time handling; no direct system clock calls in Domain/Application.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/seller-offer-repair.md

3. FULL OFFER RESIDUAL SCAN — MANDATORY

Scan ALL Offer production code:

Tooba.Offer.Domain

Tooba.Offer.Application

Tooba.Offer.Contracts

Tooba.Offer.Infrastructure

Tooba.Offer.Endpoints

Offer-owned Host integration/mapping

Offer tests/architecture guards

Search for:

Error/localization violations

Persian literals in Domain/Application exceptions/errors

English user-facing prose in Domain/Application exceptions/errors

InvalidOperationException("...")

Exception("...")

ArgumentException("...")

localized validation messages below boundary

magic string error codes bypassing canonical semantic error type

Determinism violations

Guid.NewGuid()

UuidV7.New()

direct random IDs

DateTime.Now

DateTime.UtcNow

DateTimeOffset.Now

DateTimeOffset.UtcNow

direct clock bypasses in Domain/Application

Architecture violations

Offer.Domain -> foreign module implementation refs

Offer.Application -> foreign Application refs

direct DbContext use in Application

Host business logic for Offer

Endpoints making business decisions

Infrastructure containing business rules

Contracts leaking Domain/Application implementation types

cross-module SQL joins/FKs

shared mega-DbContext coupling

folder ownership violations

root dumping

god files >800 LOC

temporary workarounds, polling, sleeps, suppressions, hardcoded shortcuts, catch-ignore, test-only branches

Offer-specific conceptual violations

Offer owning Price truth

Offer owning Stock truth

Seller/User identity confusion

duplicated return-policy authority

inconsistent SalesChannel authority

Catalog Variant modeled as cross-module EF relation rather than stable ID boundary

Produce:

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/full-residual-scan.md

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/full-residual-scan.json

Required table:
Rule | File | Line/Type | Severity | Action | Result

No violation contradicting COMPLETE_REFERENCE_PATTERN may be silently deferred.

4. Canonical Semantic Error Pattern

Locate and document the accepted repo pattern:

canonical type(s)

namespace/project

error code structure

Domain/Application signaling

Endpoint/Host -> localized ProblemDetails mapping

localization location

Reuse it exactly.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/semantic-error-pattern.md

5. Repair Domain/Application semantics

Repair every Offer Domain/Application violation found.

Requirements:

stable machine-readable codes

no localized prose in Domain/Application

no HTTP concepts below boundary

expected business failures do not use arbitrary InvalidOperationException strings

invariants remain in Domain

Application validators/use cases expose stable codes

localization only at boundary

For SellerOffer preserve behavior:

invalid minimum rejected

invalid maximum rejected

min > max rejected

archived offer cannot reactivate

Add/update focused tests for code stability and unchanged valid transitions.

6. ID / Clock determinism

If TMAR requires generator usage:

Application obtains ID via existing IIdGenerator

Domain receives ID explicitly

tests provide deterministic ID

no direct UuidV7.New() remains in Offer Domain/Application

For time:

Domain receives time as arguments

Application uses IClock

no direct system clock reads

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/determinism.md

7. Endpoint / localization boundary

Verify Offer endpoint boundary maps semantic errors correctly:

stable code

localized message at boundary

FA/EN and unlimited-locale compatible

no translation text in Domain/Application

Do not redesign endpoints.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/error-boundary.md

8. Physical/folder verification

Verify the Golden Offer module still contains:

Tooba.Offer.Domain

Tooba.Offer.Application

Tooba.Offer.Contracts

Tooba.Offer.Infrastructure

Tooba.Offer.Endpoints

Tooba.Offer.Tests

Verify responsibility folders contain real files and root dumping has not returned.
No empty ceremonial folders.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/physical-structure.md

9. Source size gate

Record all Offer production files >300, >500, >800 LOC.

Rules:

new handwritten production file >800 => FAIL

unresolved existing >800 contradicting Golden completion must be repaired now

no baseline widening

preserve cohesion when splitting

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/source-size.md

10. Dependency gate

Verify:

Domain has no foreign implementation dependency

Application has no foreign Application implementation dependency unless explicitly grandfathered AND compatible with Golden completion

cross-module calls use Contracts

Infrastructure adapters explicit

Endpoints depend inward

Host contains no Offer business truth

Any remaining grandfathered edge must be listed and justified.
If it violates Golden Module criteria, Offer cannot be COMPLETE.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/dependency-gate.md

11. Tests / validation

Run narrowest tests first, then broader Offer/backend validation:

SellerOffer domain tests

Offer Application tests

Offer architecture tests

endpoint/error mapping tests where present

build all Offer projects

backend/solution build as appropriate

No workaround or fake/skipped assertions.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/validation.md

12. Anti-pattern gate

Final scan for:

localized Domain/Application messages

InvalidOperationException business signaling

direct clock/id bypass

polling

magic sleeps/timeouts

catch-ignore

hardcode

first-item/first-seller shortcuts

test-only production branches

baseline widening

suppressions

Expected:
AntiPattern-Gate: CLEAN

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/antipattern-scan.md

13. Recovery

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-CAPABILITY-MAP.md only if facts changed

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/recovery-sot.md

Do not preserve a false COMPLETE status.

Final state must be exactly one:

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

Module-Recovery-State: INCOMPLETE

COMPLETE only if every mandatory gate passes.

14. Acceptance

PASS only if ALL:

visible SellerOffer Persian exception removed

expected business failures no longer use arbitrary InvalidOperationException strings

entire Offer Domain/Application scanned

canonical semantic error mechanism reused

ID/clock determinism verified/repaired

endpoint localization boundary verified

no unresolved Golden-pattern dependency violation

no unresolved >800 LOC Golden-module violation

focused tests green

Offer architecture tests green

anti-pattern gate CLEAN

frontend untouched

user work preserved

recovery docs truthful

15. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
TMAR-Execution-Mode
Frontend-Production-Changes
Known-Defect-Repair
Full-Residual-Scan
Semantic-Error-Pattern
Domain-Error-Repair
Application-Error-Repair
Determinism
Error-Boundary
Physical-Structure
Source-Size
Dependency-Gate
Focused-Validation
AntiPattern-Gate
Capability-Map
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Module-Recovery-State
Next-Recommended-Task

Expected if fully clean:
Module-Recovery-State: COMPLETE_REFERENCE_PATTERN
Next-Recommended-Task: USER_REVIEW_OFFER

If any mandatory gap remains:
Module-Recovery-State: INCOMPLETE
Next-Recommended-Task: TB-TMAR-OFFER-REFERENCE-W1-R3

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK
