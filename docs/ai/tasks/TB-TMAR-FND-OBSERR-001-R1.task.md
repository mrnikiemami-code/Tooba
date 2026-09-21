PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-FND-OBSERR-001-R1

Parent-Task:
TB-TMAR-OFFER-REFERENCE-W1-R6

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
Central Observability + Error/ProblemDetails Foundation — Phase 1 Core

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect Decision

Offer COMPLETE_REFERENCE_PATTERN is REOPENED.

Reason: user manual review exposed a cross-cutting gap:

local OfferEndpointLocalizer

local SemanticException → HTTP mapping

ad-hoc Accept-Language parsing

no central SafeErrorMapper

no central ApiResponseFactory

no central IProblemDetailsContextProvider

no central CorrelationId lifecycle/log scope

OpenTelemetry exists, but internal application/module path tracing is incomplete

External reference repository:
mrnikiemami-code/BuildingBlocks

PRIMARY REFERENCE BRANCH:
mastertest

Architect directly verified mastertest contains mature reference implementations for:

CorrelationIdContext / CorrelationIdProvider / CorrelationContextAdapter

ICorrelationIdProvider / ICorrelationContext

CorrelationIdMiddleware

BuildingBlocksActivitySource

BuildingBlocksTraceEnricher

BuildingBlocksTracingPolicy

ObservabilityLogScope / ObservabilityLogScopeKeys / TracingTagNames

IProblemDetailsContextProvider / ProblemDetailsContext / ProblemDetailsContextProvider

ProblemDetailsFactory / ValidationProblemFactory

ApiResults / DefaultResultHttpMapper / ResultHttpMapper

Presentation DI

Do NOT blindly copy the external package layout.
Adapt the responsibilities into Tooba's existing BuildingBlocks + Host architecture.

1. Current Tooba truth

Already exists:

ToobaTelemetry.ActivitySource / Meter

AddOpenTelemetry()

ASP.NET Core instrumentation

HttpClient instrumentation

Runtime metrics

OTLP exporter

JSON structured logging

ActivityTrackingOptions TraceId/SpanId/ParentId

AddProblemDetails()

ToobaExceptionHandler

SemanticError / SemanticException

ValidationBehavior / LoggingBehavior

Do NOT duplicate working OpenTelemetry.
The gap is central context/policy/presentation.

2. Git safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Verify before mutation:

main

HEAD == origin/main

staged = 0

protected 18ca10c9 is ancestor

stashes untouched

user .rar archives untouched

Forbidden:

git reset

git clean

destructive restore/checkout

unsafe rebase

stash pop/drop

broad git add .

Frontend:
NONE.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R1/recovery-start.md

3. Characterize first

Read current Tooba:

BuildingBlocks/TmarFoundation.cs

ToobaTelemetry.cs

PlatformHttpException.cs

BuildingBlocks csproj

Host Program.cs

Host Errors/ToobaExceptionHandler.cs

Host localization

OfferEndpointLocalizer

Offer endpoints

MassTransit correlation/header flow

Read external BuildingBlocks mastertest reference files listed above.

Produce:
docs/evidence/TB-TMAR-FND-OBSERR-001-R1/reference-gap-analysis.md

For each reference capability classify:
ADOPT / ADAPT / ALREADY_EXISTS / REJECT
with reason.

4. Central Correlation foundation

Implement one SSOT correlation lifecycle.

Required:

ICorrelationContext read-only Current CorrelationId

ICorrelationIdProvider

GetCorrelationId

GetCorrelationGuid

EnsureCorrelationId

SetCorrelationId

CorrelationIdContext

AsyncLocal-backed

Current / CurrentGuid

Set / Ensure / BeginScope

normalization

nested scope restoration

Rules:

valid incoming GUID normalized

generated fallback stable for one async flow

nested scope restores previous value

no mutable global string outside AsyncLocal

no new GUID on every read

Tests:

normalize

invalid input

generated id

nested restore

async-flow isolation

5. Central ProblemDetails context

Implement:
IProblemDetailsContextProvider

and immutable Tooba context containing at minimum:

CorrelationId

TraceId

SpanId

RequestId / TraceIdentifier

TenantId if available

StoreId if available

Actor/User id if safely available

Path

Method

HideExceptionDetails

Rules:

no DB query

must not throw if auth/tenant/store unavailable

no token/header dump

no secrets

Production HideExceptionDetails=true

controlled Development details only; never stack trace in API body

The SAME context is for:

structured logs

API ProblemDetails metadata

6. SafeErrorMapper

Implement central error classification for:

SemanticError / SemanticException

FluentValidation ValidationException

PlatformHttpException as transitional legacy input

unknown Exception

Output must include:

HTTP status

stable error code

localization key

structured args

classification/severity

safe exposure policy

Rules:

no exception.Message to client

unknown => generic stable 500

expected business errors not Error-level by default

unexpected errors Error-level

explicit validation/not-found/conflict/forbidden mapping

NO Offer-specific switch inside generic mapper

module mapping extensible, not a giant central module switch

avoid double logging

7. Central locale resolver

Implement one request-language resolver.

Forbidden:

local AcceptLanguage.Contains("en")

Persian-first fallback

FA/EN-only design

Requirements:

reuse existing Tooba localization SSOT where possible

unlimited locale

canonical normalization

centralized configurable fallback chain

malformed headers safe

consumable by ApiResponseFactory

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R1/locale-foundation.md

8. Central ApiResponseFactory / ProblemDetails factory

Create central presentation boundary equivalent in responsibility to reference:

ApiResponseFactory

ProblemDetails factory/context

semantic/current result → IResult mapping seam

Do NOT force repository-wide Result<T> migration in R1.

Must support current SemanticException flow.

Response requirements:

ProblemDetails style

stable errorCode

localized title/detail from central localization

traceId

correlationId

safe request metadata only

structured validation errors

no stack trace in Production

no exception class leak in Production

consistent status/content type

Do NOT migrate all endpoints yet.

9. Structured ObservabilityLogScope

Implement central log scope adapted from mastertest.

At minimum:

CorrelationId

TraceId

SpanId

RequestId

TenantId if known

StoreId if known

ActorId if known

ClientIp optional only if trusted-proxy safe.

No repeated manual correlation properties on every log call.

10. Tracing policy/facade

Keep ToobaTelemetry; do not replace it.

Add centralized responsibilities equivalent to:

BuildingBlocksTracingPolicy

BuildingBlocksTraceEnricher

stable tag constants

Rules:

ASP.NET owns server span when instrumentation active

HttpClient owns client spans

MassTransit owns transport spans

enrich existing spans, no duplicates

fallback only when owner instrumentation absent

health/ready excluded centrally

no scattered StartActivity in endpoints/handlers

Stable tags should support:

tooba.correlation_id

module

operation

request kind

R1 establishes APIs/policy only.
Do NOT instrument every module in R1.

11. Canonical DI

Add one canonical registration path, e.g.:
AddToobaObservabilityFoundation(...)

Register:

correlation provider/context adapter

ProblemDetails context provider

SafeErrorMapper

locale resolver

ApiResponseFactory

non-static tracing services if any

Avoid long per-service Program.cs registration.
No conflicting lifetimes.

12. Source organization

Do NOT dump into TmarFoundation.cs.

Use responsibility folders/files conceptually:

Observability/Correlation

Observability/Tracing

Observability/Logging

Presentation/ProblemDetails

Presentation/Errors

Localization

Exact layout may adapt to existing project.

Rules:

one major public responsibility per file

namespace matches ownership

no file >800 LOC

13. Tests

Add focused tests for:

Correlation:

normalization

generated fallback

nested scope

async isolation

Problem context:

trace/span/correlation

no HttpContext fallback

Production hides details

missing tenant/user safe

SafeErrorMapper:

SemanticException

ValidationException

PlatformHttpException

unknown Exception

no Message leak

Locale:

culture selection

fallback

non-FA/EN locale

malformed header

ApiResponse:

status

errorCode

correlationId

traceId

safe Production body

Tracing policy:

enrich existing Activity

no duplicate HTTP span policy

health/ready exclusion

14. Architecture guards

Add guards preventing new local cross-cutting variants in Offer:

no new endpoint-local language parser

no new local SemanticException → Results.Json switch

no ex.Message API exposure

no raw exception detail in Production

no ad-hoc correlation GUID generation in endpoints

Do NOT remove OfferEndpointLocalizer in R1 unless central replacement is fully usable with low risk.
If retained, record as explicit residual for R2/R3.

15. Validation

Required:

BuildingBlocks build

Host build

Offer build

new focused tests

Offer.Tests

affected Host tests

Anti-pattern scan:

no polling

no magic sleep

no service locator

no static IHttpContextAccessor holder

no endpoint global state

no exception.Message leak

no duplicate Activity ownership

no hardcoded module-specific localized error map in foundation

no FA-first locale fallback

Expected:
AntiPattern-Gate: CLEAN

16. Recovery truth

Update:

Master Recovery

Architect Bootstrap

R1 recovery-sot

Offer state:
REOPENED_WAITING_CENTRAL_FOUNDATION

Do NOT mark Offer complete.

17. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Reference-Repository
Reference-Branch
Reference-Gap-Analysis
Correlation-Foundation
ProblemDetails-Context
SafeErrorMapper
Locale-Resolver
ApiResponseFactory
Observability-Log-Scope
Tracing-Policy
DI-Registration
Source-Organization
Architecture-Guards
Focused-Validation
Offer-Regression-Validation
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
Module-Recovery-State: FOUNDATION_PHASE1_COMPLETE
Next-Recommended-Task: TB-TMAR-FND-OBSERR-001-R2

R2 will wire request/messaging correlation + log scope + MediatR/module-path tracing into the running Host and validate trace topology.

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK