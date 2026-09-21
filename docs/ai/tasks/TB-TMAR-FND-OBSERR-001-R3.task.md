PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-FND-OBSERR-001-R3

Parent-Task:
TB-TMAR-FND-OBSERR-001-R2

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
Central Error Classification + Localization Catalog + Unified ProblemDetails Pipeline + Offer Final Adoption

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect review of R2

R2 is accepted as:
FOUNDATION_RUNTIME_TRACING_COMPLETE

Architect directly re-read current repository and confirmed:

request correlation runtime wiring exists

request log scope exists

MediatR TracingBehavior exists

module-call tracing exists

Offer trace topology is proven

MassTransit/outbox correlation propagation exists

ProblemDetails/log/trace join is proven

However Foundation is NOT complete.

Architect-confirmed residuals:

SafeErrorMapper still infers HTTP semantics from error-code naming:

.not_found

.missing

.duplicate

cannot_activate

.denied

.forbidden

This is fragile and not a Golden cross-cutting contract.

OfferEndpointLocalizer still contains a bilingual switch with Persian/English strings.

Any non-English non-FA locale falls back to Persian in Offer:
non-en => Persian
This violates unlimited locale.

ApiResponseFactory currently accepts raw Accept-Language strings.
The central resolver exists, but presentation should converge on one culture/context path, not endpoint-passed raw headers everywhere.

ToobaExceptionHandler still duplicates:

mapper invocation

severity decision

structured logging fields

ProblemDetails factory invocation

The final design should have one central exception-presentation/logging policy.

PlatformHttpException remains transitional and carries client-facing Title.
This must be bounded as legacy input, not the preferred future error model.

R3 must close these.

1. Git / recovery safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected lineage includes R2 tip:
c7af4d2f16810b2910ef650669c611b2a8716012
plus result/tip alignment if present.

Verify:

main

HEAD == origin/main

staged = 0

protected 18ca10c9 ancestor

no unexpected tracked changes

stashes untouched

user .rar untouched

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
docs/evidence/TB-TMAR-FND-OBSERR-001-R3/recovery-start.md

2. Replace naming-heuristic error classification

Current SafeErrorMapper.ClassifySemanticCode must be removed as the primary classification mechanism.

Do NOT infer HTTP status from substrings in error code names.

Introduce an explicit stable error metadata model.

Required concept:
ErrorDescriptor / ErrorDefinition / equivalent containing:

Code

Classification

HTTP status

LocalizationKey

Severity

optional default safe fallback

optional metadata flags

Module error catalogs must explicitly define descriptors.

For Offer, all OfferErrorCodes used by API paths must have explicit descriptors.

Generic foundation errors:

validation.failed

platform.unexpected

platform.error
must also have explicit definitions.

Design goals:

compile-time discoverable where practical

no giant central switch over every module code

module registers/contributes definitions

duplicate code registration fails fast

unknown semantic code maps to safe generic business fallback, not guessed status

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R3/error-descriptor-model.md

3. Error catalog registry

Implement central registry/lookup contract, e.g. responsibility equivalent to:

IErrorDefinitionCatalog

IErrorDescriptorProvider

IErrorCatalogContributor

Rules:

module contributor owns module codes

foundation composes contributors

duplicate code => startup/test failure

missing descriptor for a SemanticError in Golden Offer => architecture/test failure

no DB-backed lookup

no runtime network lookup

immutable after startup

Offer should register its error definitions via module presentation/DI boundary.

Do not put Offer codes inside generic BuildingBlocks.

4. Remove OfferEndpointLocalizer

Delete:
OfferEndpointLocalizer

Replace bilingual switch with central resource/catalog localization.

Offer may contribute:

localization resource files
or

module-local localization provider backed by standard .NET resources

But it must NOT perform:

manual en/fa switch

StartsWith("en")

non-en => fa fallback

Preferred standard:
.resx resource-based localization using IStringLocalizer / ResourceManager infrastructure.

At minimum provide:

English resource

Persian resource

Unlimited locale behavior:

requested culture resolved centrally

if exact culture unavailable, standard parent/fallback chain

default configured fallback

NEVER automatically treat unknown locale as Persian

Example:
tr-TR with no Turkish resource => configured fallback, e.g. English.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R3/offer-localization-migration.md

5. Central localization provider

Evolve IErrorMessageLocalizer so contributors do not implement arbitrary language-switch logic.

Preferred:

central resource localizer

module resource marker types

descriptor.LocalizationKey lookup

If contributor model remains:
contributors may only provide resource namespace/marker/catalog ownership,
not hardcoded language branching.

Rules:

one culture resolution path

standard CultureInfo fallback

no raw Accept-Language parsing outside IRequestLocaleResolver

no direct AcceptLanguage handling inside Offer

no FA-first logic anywhere in new foundation

6. ApiResponseFactory cleanup

Refactor ApiResponseFactory toward one canonical context-based API.

Target responsibilities:

map exception via SafeErrorMapper

get current ProblemDetails context

get current request culture via central locale resolver/context

localize descriptor key

build safe ProblemDetails

Prefer APIs like:

CreateProblemDetails(Exception exception)

FromException(Exception exception)

Raw acceptLanguage override may remain only for tests if explicitly separated.

Production endpoints/global handler should not pass header strings manually.

ProblemDetails standard extensions:

errorCode

correlationId

traceId

requestId if available

validation errors if applicable

No:

raw exception.Message

stack trace

exception type in Production body

7. Centralize exception logging decision

Current ToobaExceptionHandler should become thin.

Move log severity / classification policy into one central service, e.g.:

IExceptionPresentationService

IErrorResponseCoordinator

or equivalent

One orchestration should:

map safe error

create ProblemDetails

decide log level

emit structured log once

write response

Avoid duplicate mapper invocation.

Expected business failures:

Validation / Business / NotFound / Conflict / Forbidden
should not be Error-level.

Unexpected/system failures:

Error-level

exception object logged

safe client body

Log fields:

ErrorCode

Classification

StatusCode

CorrelationId

TraceId

SpanId

RequestId

TenantId/StoreId/ActorId when safe/available

Path

Method

Do not duplicate fields already present in BeginScope unnecessarily, but explicit event fields for queryability are acceptable.

8. PlatformHttpException containment

Audit repository usage.

R3 rules:

keep compatibility if broadly used

central mapper handles it

do not introduce new usages

add architecture guard preventing new module Domain/Application code from throwing PlatformHttpException

preferred future path is SemanticError/typed result

If safe to migrate Offer-specific occurrences to SemanticError, do so.

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R3/platform-http-exception-audit.md

9. Offer final error pipeline adoption

Offer seller endpoints must end R3 with:

Endpoint
→ ISender
→ exception bubbles
→ global exception pipeline / canonical ApiResponseFactory

Preferred:
remove local try/catch for SemanticException/PlatformHttpException if global handler can preserve exact response behavior.

If authorization transport failures need endpoint-local handling, keep only narrowly justified transport concern.

After R3 Offer endpoint source must have:

no local error switch

no local language switch

no hardcoded localized error strings

no Accept-Language parsing

no duplicate ProblemDetails mapping

no exception.Message leak

Evidence:
docs/evidence/TB-TMAR-FND-OBSERR-001-R3/offer-error-pipeline-final.md

10. Validation errors

Ensure FluentValidation errors are represented structurally and safely.

Requirements:

field/property key

stable validation error code(s)

optional localized field message if central catalog supports it

no raw validator English Message leak by default

do not expose internal property paths if they are implementation-only

Keep current behavior compatible where possible.

11. Resource organization

Do not put localization strings in generic BuildingBlocks for module-specific errors.

Conceptually:

Foundation generic resources in BuildingBlocks

Offer resources in Offer.Endpoints or dedicated presentation resource assembly/folder

Exact project placement should minimize new projects.

Example:
Tooba.Offer.Endpoints/Resources/OfferErrors.resx
Tooba.Offer.Endpoints/Resources/OfferErrors.fa.resx

Use English invariant/default resource if that matches .NET resource convention.

12. Tests

Required:

Error descriptor registry:

explicit Offer code → expected status/classification

duplicate code registration fails

unknown semantic code safe fallback

no substring heuristic classification

Localization:

en

fa

en-US parent fallback

fa-IR parent fallback

tr-TR missing resource => configured default, NOT Persian

malformed Accept-Language safe

q-values respected by resolver

Offer:

every public OfferErrorCode used by seller API has descriptor

every descriptor localization key has English resource

Persian resource coverage for current Offer user-facing errors

OfferEndpointLocalizer file absent

no StartsWith("en") / manual culture switch

no hardcoded Persian in Offer endpoint source

ProblemDetails:

400 business

404 explicit descriptor

409 explicit descriptor

403 explicit descriptor

validation

500 unexpected

all contain coherent trace/correlation

Production body safe

Exception logging:

business failure logged once at expected level

unexpected failure logged once at Error

no duplicate handler/mapper logging

13. Architecture guards

Add guards:

no ClassifySemanticCode

no code-name substring status inference

no OfferEndpointLocalizer

no manual culture branch in Offer endpoint

no Accept-Language parsing outside central resolver

every Offer API semantic code registered in explicit catalog

no new PlatformHttpException throw in Offer Domain/Application

no local Results.Json ProblemDetails mapping in Offer endpoints

no raw validation ErrorMessage exposure if code exists

global handler uses central orchestration

14. Repo-wide scan scope

Do a repository-wide scan for:

AcceptLanguage.Contains

StartsWith("fa")

StartsWith("en")

PlatformHttpException

Results.Json(new ProblemDetails

exception.Message

ex.Message

local ProblemDetails switches

local SemanticException catches

hardcoded Persian in endpoint error branches

Do NOT migrate the entire repo in R3.

Classify findings:

FOUNDATION_FIXED

OFFER_FIXED

LEGACY_OTHER_MODULE

BLOCKS_GOLDEN_OFFER

FUTURE_MIGRATION

Produce:
docs/evidence/TB-TMAR-FND-OBSERR-001-R3/repo-error-pipeline-scan.md

15. Anti-pattern gate

Reject:

giant central module switch

reflection scan with unstable order if deterministic registration is simpler

resource lookup swallowing all errors silently

module-specific hardcoded strings in BuildingBlocks

unknown locale → Persian

substring-based HTTP classification

duplicate logging

exception.Message to client

endpoint try/catch boilerplate

service locator

static IHttpContextAccessor

culture stored in global static state

Expected:
AntiPattern-Gate: CLEAN

16. Validation

Run:

BuildingBlocks.Tests

Offer.Tests

Host error contract tests

correlation/runtime tests

localization tests

architecture guards

Host build

Offer build

backend solution build

Frontend:
NONE.

17. Recovery truth

Update:

Master Recovery

Architect Bootstrap

R3 recovery-sot

Foundation success state:
FOUNDATION_ERROR_LOCALIZATION_COMPLETE

Offer state after successful R3:
READY_FOR_FINAL_REFERENCE_REVERIFY

Do NOT mark Offer COMPLETE yet.

Expected next:
TB-TMAR-FND-OBSERR-001-R4

R4 is final integrated verification:

foundation end-to-end

trace topology

logging

correlation

localization

ProblemDetails

Offer Golden reference re-verification

repo-wide architecture guards

COMPLETE or further repair

18. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Error-Descriptor-Model
Error-Catalog-Registry
SafeErrorMapper
Locale-Localization
Offer-Localization-Migration
ApiResponseFactory
Exception-Logging-Orchestration
PlatformHttpException-Audit
Offer-Error-Pipeline
Validation-Errors
Resource-Organization
Repo-Error-Pipeline-Scan
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
Module-Recovery-State: FOUNDATION_ERROR_LOCALIZATION_COMPLETE
Offer-State: READY_FOR_FINAL_REFERENCE_REVERIFY
Next-Recommended-Task: TB-TMAR-FND-OBSERR-001-R4

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK