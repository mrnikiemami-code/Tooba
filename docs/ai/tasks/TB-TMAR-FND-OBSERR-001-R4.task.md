PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-FND-OBSERR-001-R4

Parent-Task:
TB-TMAR-FND-OBSERR-001-R3

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
FINAL_FOUNDATION_VERIFICATION_AND_OFFER_REVERIFY

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Track:
FOUNDATION_OBSERVABILITY_ERROR_PRESENTATION

Title:
Final Integrated Verification — Observability/Error/Localization Foundation + Offer Golden Reverification

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Architect Review

R3 is accepted as FOUNDATION_ERROR_LOCALIZATION_COMPLETE.

Architect directly re-read current main.

Architect-verified baseline:
1623b33886f22aa42c1622766e67cae6ce194321

Verified:

SafeErrorMapper naming heuristics removed.

explicit ErrorDescriptor catalog exists.

OfferEndpointLocalizer deleted.

Offer localization moved to .resx.

unknown non-English locale no longer falls back to Persian.

ApiResponseFactory is context-based.

ToobaExceptionHandler is thin.

Offer seller endpoints no longer perform local SemanticException/ProblemDetails mapping.

R4 is final integrated verification plus any required repair. Do not pass ceremonially.

Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Verify:

main

HEAD == origin/main

staged = 0

protected 18ca10c9 is ancestor

no unexpected tracked changes

stashes untouched

user .rar archives untouched

Forbidden:
git reset
git clean
destructive checkout/restore
unsafe rebase
stash pop/drop
broad git add .

Frontend:
NONE

1. Foundation Structure Final

Verify coherent folders:

Observability/Correlation

Observability/Tracing

Observability/Logging

Presentation/Errors

Presentation/ProblemDetails

Localization

DependencyInjection

Rules:

one major public responsibility per file

no handwritten file >800 LOC

namespace matches folder

no duplicate correlation context/provider

no duplicate trace policy

no duplicate API response mapper

no duplicate locale parser

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/foundation-structure-final.md

2. Correlation Final

Verify end-to-end HTTP, MassTransit, in-process publisher, outbox.

HTTP:

valid incoming X-Correlation-ID normalized/preserved

invalid/missing generated once

response header equals final ID

provider/context same ID

ProblemDetails same ID

nested scopes restore previous value

Messaging:

producer propagates correlation

consumer restores AsyncLocal scope

MassTransit Activity enriched, not duplicated

fallback span only if transport Activity absent

Outbox:

persisted CorrelationId survives delayed dispatch

legacy null rows safe

no fake parent trace

No competing SSOT.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/correlation-final.md

3. Log Scope Final

Verify structured scope where available:
CorrelationId
TraceId
SpanId
RequestId
TenantId
StoreId
ActorId
Method
Path
safe ClientIp only under trusted proxy rules

No token/claims dump.
No email/phone/name PII.
No DB query solely for logging.
Scope must remain active through exception presentation.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/log-scope-final.md

4. Tracing Final

Verify exactly one OpenTelemetry provider.

ASP.NET owns server spans.
HttpClient owns client spans.
MassTransit owns transport spans.
Tooba internal spans are child spans only.
health/ready excluded.

MediatR TracingBehavior:

registered once

deterministic low-cardinality names

module/operation/request_kind/correlation tags

exception.type only

no exception.Message tag

no request payload tags

No raw StartActivity in Offer endpoints/handlers.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/tracing-final.md

5. Offer Trace Topology Final

Prove:

List/Get:
HTTP
→ MediatR Offer Query
→ Catalog
→ Pricing
→ Inventory

Create:
HTTP
→ MediatR CreateOfferCommand
→ Catalog validation
→ Party validation
→ Offer persistence

Price alias:
HTTP
→ Offer route alias
→ Pricing owner boundary
→ Offer Get query

Inventory alias:
HTTP
→ Offer route alias
→ Inventory owner boundary
→ Offer Get query

No duplicate spans.
No N+1 span explosion.
No IDs/SKUs in span names.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/offer-topology-final.md

6. Error Catalog Final

Verify:

foundation descriptors for validation.failed, platform.error, platform.unexpected

every Offer SemanticError reachable from seller API has explicit descriptor

descriptor includes status/classification/localization key/severity

no substring/name heuristic classification

no ClassifySemanticCode

duplicate code registration fails

unknown semantic code uses safe generic behavior without guessed status

registry deterministic and immutable after startup

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/error-catalog-final.md

7. Localization Final

Verify:

only IRequestLocaleResolver parses Accept-Language for error presentation

q-values respected

malformed header safe

exact culture → parent → configured fallback

unknown locale never means Persian

no FA-first behavior

no module-local language switch

no StartsWith("en")/StartsWith("fa") in Offer error presentation

English Offer resources complete

Persian Offer resources complete

Test:
en
en-US
fa
fa-IR
tr-TR missing resource => configured fallback
malformed header
wildcard edge cases

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/localization-final.md

8. ProblemDetails Final

Expected pipeline:
Exception
→ SafeErrorMapper
→ ErrorDescriptor catalog
→ ProblemDetails context
→ locale resolver
→ resource localizer
→ ApiResponseFactory
→ IExceptionPresentationService
→ IProblemDetailsService

Verify:

mapper invoked once

no endpoint duplicate mapping

errorCode/correlationId/traceId present

requestId when available

validation errors structured

no stack trace in Production

no raw exception.Message in Production

no exception type in Production body

Development detail controlled

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/problem-details-final.md

9. Exception Logging Final

Business/validation/not-found/conflict/forbidden:
not Error-level.

Unexpected/system:
Error-level.

Requirements:

one exception event per failure

no duplicate Error from MediatR + global handler

structured ErrorCode/Classification/StatusCode

correlation/trace join

no exception.Message copied to public response

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/exception-logging-final.md

10. PlatformHttpException Containment

Repo-wide scan.

Classify:
LEGACY_HOST
LEGACY_MODULE_ENDPOINT
FOUNDATION_TRANSITIONAL
BLOCKS_OFFER_GOLDEN
NEW_VIOLATION

Rules:

none in Offer Domain/Application

no new Golden Offer usage

generic foundation supports only as compatibility input

do not migrate unrelated modules in R4 unless shared foundation/Offer blocker

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/platform-http-final.md

11. Offer Golden Reverification

Re-run full Offer checklist.

Physical:

canonical six projects

VS grouping

no stale duplicate

no root dumping

Domain:

no foreign Domain/implementation refs

no Contracts dependency

semantic errors only

no localized text

no persistence/HTTP

Application:

real MediatR CQRS

per-use-case folders

no DbContext/HTTP/Host

no foreign Application refs

seller scope enforced

Contracts:

correct namespace

no Domain namespace leak

no implementation leakage

Infrastructure:

Offer persistence only here

no foreign Application refs

no use-case orchestration

no cross-module SQL/FK

Endpoints:

thin

ISender for Offer-owned use cases

Pricing/Inventory aliases only call owner contracts

no local error/localization

no Host BFF

Host:

zero OfferDbContext business/read-model leak

zero Offer aggregate mutation

zero Offer seller response shaping

Source:

no handwritten Offer production >800 LOC

Type identity:

no TypeForwardedTo

no ambiguous duplicate OfferStatus/SalesChannel ownership

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/offer-golden-reverify.md

12. Architecture Guards Final

Guards must prevent:

Domain→Contracts

TypeForwardedTo

Contracts namespace leak

foreign Application refs

OfferDbContext outside Offer.Infrastructure production

Host Offer business read/write

OfferEndpointLocalizer

local Offer Accept-Language parser

local Offer SemanticException mapping

error-code substring HTTP classification

missing Offer descriptor/resource

manual en/fa switch

raw exception.Message response

duplicate TracingBehavior registration

raw StartActivity in Offer endpoints/handlers

duplicate OpenTelemetry provider

local correlation context

source >800 LOC

direct time/id bypass in Offer

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/golden-guards-final.md

13. Repo-wide Cross-cutting Scan

Search:
AcceptLanguage
StartsWith("fa")
StartsWith("en")
PlatformHttpException
SemanticException
ProblemDetails
Results.Json
ex.Message
exception.Message
Guid.NewGuid
ActivitySource
StartActivity
CorrelationIdContext
X-Correlation-ID
custom error mappers/localizers

Classify:
FOUNDATION_CANONICAL
OFFER_CANONICAL
LEGACY_OTHER_MODULE
VIOLATION
TEST_ONLY
GENERATED

Do not migrate unrelated legacy modules.
Repair every FOUNDATION or OFFER violation.

Produce:
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/repo-crosscutting-scan.md
docs/evidence/TB-TMAR-FND-OBSERR-001-R4/repo-crosscutting-scan.json

14. Tests

Run:

BuildingBlocks.Tests

full Offer.Tests

Host error contract tests

Host correlation runtime tests

Offer trace topology tests

localization tests

error catalog tests

messaging correlation tests

outbox correlation tests

architecture guards

Host build

all Offer projects build

backend solution build

If parallel WebApplicationFactory tests still flake due MassTransit SQL migrator:

characterize root cause

no sleeps/retries

no hiding/skipping

fix test isolation or serialize only justified fixture

No fake assertions.

15. Anti-pattern Gate

Reject:

polling

magic delays/retries

ThreadStatic

static HttpContext

service locator

duplicate correlation SSOT

duplicate spans

unbounded telemetry cardinality

PII/tokens in telemetry

exception.Message in public response

bilingual hardcoded error switch

unknown locale→Persian

heuristic status mapping

duplicate exception logging

endpoint semantic try/catch boilerplate

cross-module DbContext

baseline widening

suppressions to force green

Expected:
AntiPattern-Gate: CLEAN

16. Completion Decision

Success only if objectively clean:

Module-Recovery-State:
FOUNDATION_COMPLETE

Offer-State:
COMPLETE_REFERENCE_PATTERN

Next-Recommended-Task:
USER_REVIEW_OFFER

If ANY mandatory Foundation or Offer defect remains:

Module-Recovery-State:
INCOMPLETE

Next-Recommended-Task:
TB-TMAR-FND-OBSERR-001-R5

Do not complete because R4 is called final.

17. Recovery Docs

Update truthfully:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-CAPABILITY-MAP.md only if facts changed

docs/evidence/TB-TMAR-FND-OBSERR-001-R4/recovery-sot.md

If success:
record central Observability/Error/Localization foundation as canonical and Offer as first Golden consumer.

18. Git Discipline

Stage task-owned files only.
No git add .

Before commit:
git diff --cached --name-only

Do not stage/delete .rar.

End:

push origin/main

HEAD == origin/main

staged = 0

no unexpected tracked changes

stashes untouched

user work preserved

19. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Architect-Verified-R3-State
Foundation-Structure
Correlation-Final
Log-Scope-Final
Tracing-Final
Offer-Topology-Final
Error-Catalog-Final
Localization-Final
ProblemDetails-Final
Exception-Logging-Final
PlatformHttpException-Final
Offer-Golden-Reverify
Architecture-Guards
Repo-Crosscutting-Scan
Focused-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Foundation-State
Offer-State
Frontend-Production-Changes
Capability-Map
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Blockers
User-Work-Preserved
Module-Recovery-State
Next-Recommended-Task

Expected only if objectively clean:
Module-Recovery-State: FOUNDATION_COMPLETE
Offer-State: COMPLETE_REFERENCE_PATTERN
Next-Recommended-Task: USER_REVIEW_OFFER

Otherwise:
Module-Recovery-State: INCOMPLETE
Next-Recommended-Task: TB-TMAR-FND-OBSERR-001-R5

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK