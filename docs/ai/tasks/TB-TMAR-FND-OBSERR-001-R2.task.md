PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-FND-OBSERR-001-R2

Parent-Task:
TB-TMAR-FND-OBSERR-001-R1

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
FOUNDATION_REPAIR

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Track:
FOUNDATION_OBSERVABILITY_ERROR_PRESENTATION

Title:
Runtime Correlation + Request Log Scope + MediatR/Module Trace Topology + Messaging Propagation

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect review of R1

R1 is accepted as FOUNDATION_PHASE1_COMPLETE, not as final foundation completion.

Architect directly verified current repository state after R1:

CorrelationIdContext exists and is AsyncLocal-based.

CorrelationIdMiddleware exists.

SafeErrorMapper exists.

RequestLocaleResolver exists.

ProblemDetails foundation exists.

ObservabilityLogScope exists.

ToobaTracingPolicy / ToobaTraceEnricher exist.

Host registers AddToobaObservabilityFoundation().

Offer now routes semantic/platform errors through central presentation.

OfferEndpointLocalizer remains a bilingual contributor residual.

Important verified residuals:

request runtime correlation/log-scope wiring is incomplete;

module/use-case trace topology is not yet visible;

MediatR has LoggingBehavior but no tracing behavior;

MassTransit/outbox correlation + W3C trace propagation is not yet proven;

request log scope currently contains only correlation/request basics and does not reliably carry commerce/actor context;

current ToobaTraceEnricher only enriches existing HTTP activity and does not yet provide internal module-call topology.

R2 closes runtime wiring and proves a visible request path.

1. Git/recovery safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected lineage includes R1 implementation:
4dee9c8214478c8668e01490f8bd4950faa333af
plus any canonical result/tip-alignment commits.

Verify:

branch main

HEAD == origin/main

staged = 0

no unexpected tracked modifications

protected 18ca10c9 remains ancestor

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
NONE.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R2/recovery-start.md

2. Characterize actual runtime pipeline before edits

Locate and document:

current app.Use... middleware ordering in Host Program.cs

forwarded headers ordering

auth/authz ordering

commerce/tenant/store context assignment ordering

exception handler ordering

CorrelationIdMiddleware registration/use

MassTransit publisher implementation

MassTransit consumer implementation

outbox dispatcher publish path

in-process testing publisher

current message headers

current Activity/traceparent behavior

Produce:
docs/evidence/TB-TMAR-FND-OBSERR-001-R2/runtime-pipeline-before.md

Do not guess middleware order.

3. Wire CorrelationIdMiddleware correctly

Ensure UseToobaCorrelationId() is actually present in runtime pipeline.

Required ordering principles:

forwarded headers before IP-dependent context

correlation early enough that all later logs/errors have it

exception/error pipeline can access correlation

auth/tenant/store context may enrich later scope without replacing correlation

no duplicate middleware registration

HTTP contract:

incoming valid X-Correlation-ID accepted and normalized

invalid/missing input => generated canonical ID

response always contains final correlation ID

Activity gets tooba.correlation_id

same value available through ICorrelationContext / ICorrelationIdProvider

same value appears in ProblemDetails

Do not use HttpContext.Items as a competing SSOT unless only as compatibility mirror.

4. Add request observability scope with platform context

Current scope is too thin.

Create/adapt one runtime request scope that enriches structured logs with:

CorrelationId

TraceId

SpanId

RequestId

TenantId when resolved

StoreId when resolved

ActorId when authenticated

HTTP method

route/path

optional client IP only via trusted-forwarded-header semantics

Important:

no DB query solely for logging

missing context is safe

no tokens/claims dump

no email/phone/name PII

do not mutate business context

do not create a second correlation ID

If tenant/store/actor become available after initial correlation middleware, use a second nested enrichment scope or a dedicated middleware after context/auth resolution.
Do NOT delay basic correlation until after authentication.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R2/request-log-scope.md

5. MediatR tracing behavior

Implement a central:
TracingBehavior<TRequest,TResponse>

Register it in canonical CQRS foundation.

For every MediatR request:

create one internal Activity child span using existing ToobaTelemetry.ActivitySource

do not create a span if tracing is disabled/no listener (normal ActivitySource semantics)

operation span must be deterministic and low-cardinality

Recommended naming:
mediatr.<module>.<request>

Required tags:

tooba.module

tooba.operation

tooba.request_kind = command/query/request

tooba.correlation_id

request type name

Module should be derived centrally from namespace/assembly convention where reliable.
No hardcoded Offer-only mapping.

On success:

Activity status OK where appropriate

On exception:

Activity status Error

add exception type only, not exception.Message as a tag

do not duplicate log exception at every layer

Do not serialize request payload into tags.

6. LoggingBehavior relationship

Current LoggingBehavior logs:

handling

handled

failed

Review for duplicate/noisy behavior after TracingBehavior.

Target:

structured logs should correlate with Activity and scope

expected SemanticException/ValidationException should not produce duplicate Error logs

unexpected failures should be logged once at the correct boundary

Debug lifecycle logs are acceptable if low-noise

If LoggingBehavior currently logs expected business failures as Warning and the global exception pipeline also logs them, normalize behavior to avoid double logging.

Document decision:
docs/evidence/TB-TMAR-FND-OBSERR-001-R2/mediatr-logging-policy.md

7. Standard module boundary tracing API

Implement a reusable internal-module call tracing abstraction/facade.

Goal:
make synchronous Modular Monolith calls visible like:

HTTP
└─ Offer.ListSellerOffersQuery
├─ Catalog.Lookup
├─ Pricing.Lookup
├─ Inventory.Lookup
└─ Offer.Persistence/Query

Do NOT scatter raw ActivitySource.StartActivity() across handlers.

Provide one canonical helper/service, e.g. responsibility equivalent to:

IModuleCallTracer

ModuleCallTrace

ToobaModuleTrace

Required API supports:

source module

target module

operation

correlation automatically

child Activity

success/error status

no payload/PII

Tags:

tooba.module.source

tooba.module.target

tooba.operation

tooba.request_kind=module_call

tooba.correlation_id

Keep cardinality bounded:
operation names are static names, not IDs/SKUs/routes with raw values.

8. Prove topology on Offer Golden path

Instrument ONLY enough real Offer cross-module boundaries to prove the pattern.

At minimum current Offer read/create flows that call:

Catalog contract/gateway

Party contract/gateway where applicable

Pricing contract/gateway

Inventory contract/gateway

Do not instrument every module repository-wide in R2.

Preferred placement:
at the cross-module adapter/gateway/decorator boundary,
not scattered throughout handlers.

Expected trace shape for list/get:
HTTP
→ MediatR Offer Query
→ Offer.Application
→ Catalog module call
→ Pricing module call
→ Inventory module call

Expected create:
HTTP
→ MediatR CreateOfferCommand
→ Catalog validation call
→ Party seller validation call
→ Offer persistence

No duplicate spans for the same logical call.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R2/offer-trace-topology.md

9. MassTransit correlation propagation

Inspect actual publisher/consumer APIs and implement correlation propagation compatible with MassTransit 8.5.10 + PostgreSQL SQL Transport.

Producer/publish path:

obtain correlation from ICorrelationContext/Provider

set MassTransit CorrelationId when possible

propagate W3C trace context through transport using standard MassTransit/OpenTelemetry semantics first

only add custom traceparent/tracestate headers if required by actual transport/instrumentation gaps

do not fight MassTransit-owned Activity

Consumer:

reuse transport CorrelationId if present/valid

establish CorrelationIdContext scope for consume lifetime

response/logs use same correlation

enrich existing MassTransit Activity

create fallback child Activity only if MassTransit Activity genuinely absent

dispose scope and restore prior AsyncLocal

No new random correlation per consumer when valid incoming exists.

10. Outbox correlation durability

This is critical.

Current outbox can introduce time separation between HTTP request and actual publish.

Determine what metadata is persisted today.

Ensure outbox record preserves enough observability context for later dispatcher publish:

CorrelationId

causation/message identity where existing model supports it

traceparent/tracestate only if semantically safe/useful and persistence schema allows additive change

Important:

do not persist Activity object

do not depend on AsyncLocal surviving background dispatch

no cross-request static state

If schema change is required:

additive migration only

no destructive migration

backward-compatible null handling for existing outbox rows

Dispatcher:

restores correlation scope from persisted metadata

publish receives stable correlation

links/parents tracing according to OpenTelemetry semantics

no fake parent if original trace context unavailable

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R2/outbox-correlation.md

11. In-process test double parity

InProcessIntegrationEventPublisher must preserve the same correlation contract as production publisher where meaningful.

Tests must not pass only because production transport is bypassed.

12. OpenTelemetry source registration

Verify the existing Host OpenTelemetry tracing registers all custom sources actually used.

At minimum:

ToobaTelemetry.ActivitySourceName

MassTransit source

Do not add duplicate AddOpenTelemetry() pipelines.
Do not add another tracer provider.

If EF instrumentation is not currently enabled, DO NOT add it just to satisfy R2 unless required by the explicit trace topology objective.
R2 focus is request/module/messaging path.

13. ProblemDetails/log join

Prove that one failed request can be joined across:

API ProblemDetails correlationId

API ProblemDetails traceId

structured log CorrelationId

structured log TraceId/SpanId

Activity tags

For one SemanticException and one unexpected exception:
all identifiers must be coherent.

No response contains stack trace or raw exception.Message in Production.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R2/problem-log-trace-join.md

14. Tests — runtime topology

Add focused tests with ActivityListener / in-memory logger/test sink where appropriate.

Required:

HTTP correlation:

valid incoming preserved/normalized

invalid input replaced

response header equals provider context

ProblemDetails identifiers match request

MediatR:

command span created

query span created

module/request tags correct

exception marks Activity error

no request payload tags

Module boundary:

child span source/target/operation

correlation inherited

no duplicate span for one call

Offer proof:

List/Get trace includes Catalog/Pricing/Inventory module-call spans

Create trace includes Catalog/Party call spans

Messaging:

producer correlation propagated

consumer restores correlation

nested consume scope restores previous value

existing MassTransit Activity is enriched, not duplicated

Outbox:

persisted correlation survives dispatcher delay

legacy row without correlation remains processable

Logging:

scope contains correlation/trace/request

tenant/store/actor only when available

no sensitive claim/header dump

15. Architecture guards

Add/strengthen guards:

TracingBehavior<,> registered exactly once

no raw StartActivity in Offer handlers/endpoints

no Guid.NewGuid correlation in endpoint/module code

no module-specific custom correlation context

no local manual traceparent parsing in business modules

Offer uses canonical module tracing helper at cross-module boundary

no request payload serialized into trace tags

no exception.Message tag

no duplicate OpenTelemetry provider registration

no new endpoint-local error/localization mapper

16. Anti-pattern scan

Reject:

polling

magic sleeps

AsyncLocal without scope restore

ThreadStatic

static current HttpContext

service locator

raw RequestServices resolution in foundation

custom background timer for telemetry

duplicate spans

unbounded tag cardinality

user PII in tags/log scopes

exception message in tags/API

blanket catch-ignore

hardcoded module IDs in generic foundation

correlation generated separately by HTTP/MassTransit/outbox

Expected:
AntiPattern-Gate: CLEAN

17. Validation

Run:

BuildingBlocks tests

Offer.Tests

Host error/correlation tests

messaging tests

outbox tests

focused Offer trace topology tests

Host build

backend solution build

Frontend:
NONE.

18. Recovery truth

Update:

Master Recovery

Architect Bootstrap

R2 recovery-sot

Offer remains:
REOPENED_WAITING_CENTRAL_FOUNDATION

Foundation success state:
FOUNDATION_RUNTIME_TRACING_COMPLETE

Do NOT declare Offer complete in R2.

Residual expected after R2:

remove bilingual OfferEndpointLocalizer and move to central localization catalog/resource model

normalize SafeErrorMapper classification model (no fragile code-name heuristics if still present)

migrate ToobaExceptionHandler / endpoint presentation fully onto central factory

final repo-wide guard/verification

Those belong to R3/R4.

19. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Runtime-Pipeline-Before
Correlation-Middleware
Request-Log-Scope
MediatR-Tracing
MediatR-Logging-Policy
Module-Trace-API
Offer-Trace-Topology
MassTransit-Correlation
Outbox-Correlation
TestDouble-Parity
OpenTelemetry-Registration
Problem-Log-Trace-Join
Architecture-Guards
Focused-Validation
Full-Validation
AntiPattern-Gate
Residual-Foundation-Work
Offer-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Module-Recovery-State
Next-Recommended-Task

Expected:
Module-Recovery-State: FOUNDATION_RUNTIME_TRACING_COMPLETE
Next-Recommended-Task: TB-TMAR-FND-OBSERR-001-R3

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK