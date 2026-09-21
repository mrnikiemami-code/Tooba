PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-REFBATCH-TP-001

Parent-Task:
TB-TMAR-FND-OBSERR-001-R4

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
FAST_SAFE_REFERENCE_REVALIDATION

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Mode:
FAST-SAFE

Track:
REFERENCE_MODULE_REVALIDATION

Title:
Fast-Safe Revalidation + Repair Batch — Tax + Pricing against Golden Offer/Foundation

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Architect Decision

Offer and central Observability/Error/Localization foundation are accepted as Golden reference.

Verified final states:

FOUNDATION_COMPLETE

Offer-State: COMPLETE_REFERENCE_PATTERN

Historical state:

Tax was previously marked COMPLETE_REFERENCE_PATTERN.

Pricing was previously marked COMPLETE_REFERENCE_PATTERN.
Those states predate the final Foundation/Offer Golden contract and therefore need one fast revalidation pass.

Speed rule:
Do NOT split Tax and Pricing into separate waves unless a real blocker requires it.
Audit both, repair all bounded defects in this one task, add guards, run tests, report residual blockers only.

No frontend work.

Git / Recovery Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Verify:

branch main

HEAD == origin/main

staged = 0

protected 18ca10c9 remains ancestor

stashes untouched

user .rar files untouched

Forbidden:

git reset

git clean

destructive checkout/restore

unsafe rebase

stash pop/drop

broad git add .

Evidence:
docs/evidence/TB-TMAR-REFBATCH-TP-001/recovery-start.md

Golden Contract to Apply

Use current Offer + Foundation as canonical reference.

Each module must end with:

Domain/Application/Contracts/Infrastructure/Endpoints/Tests ownership

MediatR CQRS for module-owned HTTP use cases

thin Endpoints

Contracts as cross-module boundary

no cross-module DbContext/SQL/FK

no foreign Application references

no Host business orchestration for module-owned concerns

SemanticError/explicit descriptor path

global ProblemDetails/error pipeline

central locale resolver/resource localization

central correlation/tracing/logging

IClock/IIdGenerator where time/id generation occurs

no local exception mapper/localizer

no raw ex.Message response

no source >800 LOC without explicit justified exception

no workaround patterns

Fast Pre-Audit — Tax

Audit all production Tax projects and Host composition.

Required checks:

canonical six-project structure or justified equivalent

namespaces match ownership

no stale duplicate folders/types

Domain has no HTTP/persistence/Host/localized prose/PlatformHttpException

no foreign Domain implementation refs

no direct time/id bypass where shared abstractions are required

Application uses MediatR for module-owned use cases

Application has no DbContext/Host/foreign Application/HTTP shaping

Contracts expose stable public ports/DTOs without implementation leakage

Infrastructure owns TaxDbContext/config/migrations/adapters only

no cross-module SQL/FK

Endpoints are thin and use ISender for Tax-owned use cases

no local try/catch mapping

no local Accept-Language parsing

no local ProblemDetails factory

no bilingual hardcoded error switch

Host has no TaxDbContext business/read-model leak or Tax aggregate mutation

user-facing semantic errors reachable from HTTP have explicit descriptors/resources if applicable

tracing/correlation inherited centrally

Produce:
docs/evidence/TB-TMAR-REFBATCH-TP-001/tax-audit.md

Repair every bounded Tax defect.

Fast Pre-Audit — Pricing

Apply the same checks to Pricing.

Additionally verify:

authored price ownership remains Pricing

Offer aliases call Pricing owner contract, not Pricing persistence

cross-module reads use Contracts/Gates

no Offer Domain/Application dependency

no direct OfferDbContext

no duplicate pricing authority in Host

currency/value-object ownership is clear

batch lookup APIs avoid N+1

write paths preserve seller/offer authorization boundary where applicable

Pricing endpoints use global error/localization pipeline

Produce:
docs/evidence/TB-TMAR-REFBATCH-TP-001/pricing-audit.md

Repair every bounded Pricing defect.

Central Foundation Adoption Sweep

For BOTH Tax and Pricing scan:

PlatformHttpException

SemanticException

AcceptLanguage

StartsWith("en")

StartsWith("fa")

ProblemDetails

Results.Json

ex.Message

exception.Message

DateTime.UtcNow

DateTimeOffset.UtcNow

Guid.NewGuid

UuidV7.New

ActivitySource

StartActivity

direct logger scope creation

local correlation IDs

Classify:
CANONICAL
LEGACY_SAFE
VIOLATION

Any VIOLATION in Tax/Pricing production must be fixed unless it requires risky data migration.

Evidence:
docs/evidence/TB-TMAR-REFBATCH-TP-001/foundation-adoption-scan.md

Error Descriptor / Localization Coverage

For each module:

every SemanticError reachable from HTTP must have explicit ErrorDescriptor

no status inference from code name

English default resource for current localized errors

Persian resource where current product supports Persian

unknown locale follows central fallback and never implies Persian

no module-local language switch

If a module has no user-facing localized error surface, document that fact; do not add ceremonial resources.

Tracing / Module Path

Do not over-instrument.

Verify:

MediatR spans inherited automatically

cross-module calls use canonical IModuleCallTracer/decorator only when useful

no raw StartActivity in handlers/endpoints

no duplicate spans

bounded low-cardinality operation names

For Pricing, prove at least one real cross-module boundary if one exists.
For Tax, add module-call tracing only if there is an actual cross-module call.

Host Leak Scan

Search Host production for:

TaxDbContext

PricingDbContext

Tax/Pricing aggregate/entity mutation

direct SQL to tax/pricing schemas

module-owned decisions

duplicate tax/pricing calculations in Host

Repair bounded leaks through existing Contracts/Application seams.
Do not refactor unrelated Host debt.

Evidence:
docs/evidence/TB-TMAR-REFBATCH-TP-001/host-leak-scan.md

Architecture Guards

Add/strengthen canonical guards:

no foreign Application refs

no DbContext outside module Infrastructure production

no Host module persistence references

no local error/localization mapper

no direct time/id bypass

no raw StartActivity in endpoints/handlers

no source >800 LOC

no TypeForwardedTo

no namespace ownership leak

no Domain→Contracts if Golden layering disallows it for that module

Do not duplicate equivalent guards.

Tests / Validation

For speed run:

Tax.Tests + Pricing.Tests

architecture guards

focused Host tests

backend solution build

Required:

Tax.Tests green

Pricing.Tests green

BuildingBlocks tests only if foundation changed

Offer.Tests only if shared contracts/foundation changed

Host build green

backend solution build green

No frontend tests.
No sleep/retry workaround.
Fix deterministic test isolation issues rather than hiding them.

Anti-Pattern Gate

Reject:

polling

magic delays

catch-ignore

suppressions to force green

service locator

hardcoded seller/first-item shortcuts

cross-module DbContext

bilingual hardcoded error switches

exception.Message response

duplicate telemetry spans

direct DateTime/Guid bypass where shared abstraction required

baseline widening

Expected:
AntiPattern-Gate: CLEAN

Completion Criteria

Success only if BOTH modules satisfy Golden contract.

Expected success:
Tax-State: COMPLETE_REFERENCE_PATTERN
Pricing-State: COMPLETE_REFERENCE_PATTERN
Batch-State: COMPLETE
Module-Recovery-State: REFERENCE_BATCH_COMPLETE
Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-001

If one module fails:

mark only that module INCOMPLETE

keep the clean module accepted

next task must target only the incomplete module

Recovery Docs

Update:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-REFBATCH-TP-001/recovery-sot.md

Remove stale text saying Pricing waits for USER_REVIEW_OFFER; user explicitly authorized immediate continuation after Offer acceptance.

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend remains frozen.

Git Discipline

Stage task-owned files only.
No git add .

Before commit:
git diff --cached --name-only

End:

push origin/main

HEAD == origin/main

staged = 0

no unexpected tracked changes

stashes untouched

user work preserved

Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Golden-Baseline
Tax-Audit
Tax-Repairs
Tax-Foundation-Adoption
Tax-Guards
Tax-Validation
Tax-State
Pricing-Audit
Pricing-Repairs
Pricing-Foundation-Adoption
Pricing-Guards
Pricing-Validation
Pricing-State
Host-Leak-Scan
Foundation-Adoption-Scan
Focused-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Batch-State
Frontend-Production-Changes
Checkout-State
Capability-Map
Master-Recovery-State
Recovery-SoT
Git
Blockers
User-Work-Preserved
Module-Recovery-State
Next-Recommended-Task

Expected:
Module-Recovery-State: REFERENCE_BATCH_COMPLETE
Tax-State: COMPLETE_REFERENCE_PATTERN
Pricing-State: COMPLETE_REFERENCE_PATTERN
Batch-State: COMPLETE
Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-001

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK