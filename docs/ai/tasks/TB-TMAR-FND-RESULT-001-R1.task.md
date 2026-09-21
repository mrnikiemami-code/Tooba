PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-FND-RESULT-001-R1

Parent-Task:
TB-TMAR-REFBATCH-TP-001

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
RESULT_PATTERN_GOLDEN_REPAIR

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Mode:
FAST-SAFE

Track:
FOUNDATION_RESULT_PATTERN_AND_OFFER_ADOPTION

Title:
Result Pattern + ApiResponseFactory Success/Failure Mapping + Offer Golden Adoption

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect Correction

The previous Offer COMPLETE_REFERENCE_PATTERN claim is REOPENED.

User correctly identified that Offer seller endpoints still do:

Results.Json(await sender.Send(...))

direct Results.Json(..., 201)

and Tooba currently has no real shared:

Result

Result<T>

ApiResults / Result HTTP mapper

success Result mapping through ApiResponseFactory

The current central ApiResponseFactory only handles exception/problem response paths.

That is incomplete relative to the intended Golden pattern.

The external reference repository:
mrnikiemami-code/BuildingBlocks
branch:
mastertest

Architect verified reference contains:

BuildingBlocks.SharedKernel/Common/Results/Result.cs

ResultT.cs

BuildingBlocks.Presentation/Results/ApiResults.cs

DefaultResultHttpMapper.cs

IResultHttpMapper

success envelope + failure ProblemDetails mapping

IMPORTANT:
Do NOT blindly copy the reference static service-locator implementation.
Tooba previously rejected static IHttpContextAccessor/service-location as an anti-pattern.

Adopt the Result semantics and HTTP mapping responsibilities into Tooba cleanly.

1. Supersession / Scheduling

TB-TMAR-REFBATCH-TP-001 has now completed successfully:

Tax-State: COMPLETE_REFERENCE_PATTERN

Pricing-State: COMPLETE_REFERENCE_PATTERN

Module-Recovery-State: REFERENCE_BATCH_COMPLETE

However, both states are PROVISIONAL relative to the newly reopened Result Pattern gap because that batch ran before Result/ApiResponseFactory became part of the Golden contract.

Do NOT start any next module batch.

After this task succeeds:

Offer becomes Golden with Result Pattern.

Run one bounded delta-repair pass on Tax/Pricing ONLY for Result Pattern/API response adoption where applicable.

Then continue to next modules.

2. Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Verify:

main

HEAD == origin/main

staged = 0

protected 18ca10c9 ancestor

stashes untouched

.rar user files untouched

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
docs/evidence/TB-TMAR-FND-RESULT-001-R1/recovery-start.md

3. Characterize Current Contracts First

Before mutation document:

current Offer endpoint success response shapes

current Offer error response shapes

current seller frontend/Host/API consumers if any

current tests asserting raw JSON vs envelope

current ApiResponseFactory

current SafeErrorMapper

current SemanticError

current validation pipeline

current Pricing/Inventory seller write gateway signatures used by Offer aliases

Produce:
docs/evidence/TB-TMAR-FND-RESULT-001-R1/current-result-gap.md

Do not guess external response compatibility.

4. Shared Result Model

Implement a canonical Tooba Result Pattern in BuildingBlocks.

Required:

Result

Result<T>

invariant: success has no errors; failure has >=1 error

IsSuccess

IsFailure

typed Value

Errors

FirstError

Success(...)

Failure(...)

Match APIs if useful

Reuse existing SemanticError as the canonical business failure payload unless there is a strong technical reason to introduce another Error type.

Preferred:
Result carries SemanticError / IReadOnlyList<SemanticError>.

Do NOT create a second parallel error-code model that duplicates:

ErrorDescriptor

ErrorClassification

SemanticError

Unexpected exceptions remain exceptions.

Expected business failures should become Result failures.

Validation pipeline may remain exception-based temporarily only if generic Result conversion would require unsafe reflection/service-location; document explicitly.

5. Result Mapping through ApiResponseFactory

Extend the existing central ApiResponseFactory to map:

Result

Result<T>

optional paged Result if Tooba already has a paging model

Created success

Result failure → canonical ProblemDetails

Target endpoint ergonomics should be equivalent to:

var result = await sender.Send(command, token);
return api.From(result);

and:

var result = await sender.Send(command, token);
return api.Created(location, result);

You MAY introduce:

IResultHttpMapper

DefaultResultHttpMapper

scoped ApiResults facade

ONLY if it improves separation.

Forbidden:

static mutable IHttpContextAccessor

RequestServices service locator

static mapper configured at startup

fallback mapper that invents runtime context in production path

Unit tests without HttpContext should instantiate mapper/factory with explicit test doubles through DI/constructors.

6. Success Response Contract

Use one canonical success response contract.

First inspect current Tooba API conventions.

If a canonical ApiResponse<T> envelope already exists, reuse it.

If none exists:
introduce a minimal stable envelope consistent with the intended BuildingBlocks reference, conceptually:

{
  "data": ...,
  "meta": ...
}

Requirements:

success mapping centralized

no endpoint-specific JSON shape construction

Created sets 201 + Location

no error data mixed into success envelope

IMPORTANT:
If changing Offer seller success JSON shape would break an already-shipped client, preserve compatibility and document the compatibility adapter.
Do not silently break public consumers.

Evidence:
docs/evidence/TB-TMAR-FND-RESULT-001-R1/api-success-contract.md

7. Safe Failure Mapping

Result failure must go through existing:

ErrorDescriptor catalog

localization

ProblemDetails context

correlation/trace

ApiResponseFactory

Do NOT:

throw SemanticException just to turn Result back into exception

duplicate ErrorDescriptor logic

infer status from error code names

map errors inside endpoint switch statements

Add central mapper methods for SemanticError/Result errors if needed.

Multi-error behavior:

define deterministic primary status/code

preserve structured validation/multi-error details safely

no raw messages

8. Offer CQRS Adoption

Migrate all Offer-owned seller CQRS request signatures from raw DTO to Result:

CreateOfferCommand

UpdateOfferCommand

GetOfferQuery

ListSellerOffersQuery

Expected signatures:

IRequest<Result<SellerOfferDetailPage>>

etc.

Handlers:

expected business failures return Result.Failure(...)

success returns Result.Success(...)

unexpected infrastructure failures still throw

Examples that MUST become Result failure rather than expected SemanticException:

catalog variant missing

seller missing

seller kind invalid

duplicate listing

duplicate seller SKU

unsupported status

offer not found

seller-scope mismatch

return-policy business rejection

quantity business rejection

Do not catch arbitrary Exception and convert to business failure.

9. Domain Expected Failure Strategy

Audit SellerOffer domain methods currently throwing SemanticException:

SetOrderQuantityLimits

Activate

other Offer domain policies

Choose ONE clean strategy:

A. Domain returns Result for expected invariant failures;
OR
B. Domain exposes explicit validation/policy Result before mutation and mutating method assumes validated input;
OR
C. Application-level policy validates expected failures before invoking domain mutation.

Preferred: avoid try/catch SemanticException as normal control flow.

Do not weaken invariants.
Do not leave duplicate validation rules that can drift.

Document:
docs/evidence/TB-TMAR-FND-RESULT-001-R1/domain-result-strategy.md

10. Return Policy / Business Policies

Any Offer application/domain policy that currently throws SemanticException for expected seller input must be migrated to Result-based failure for the Golden HTTP use cases.

No exception-based normal branch.

Unexpected programming/system exceptions still throw.

11. Offer Seller Endpoints Final Shape

Offer endpoints must stop using raw Results.Json for Offer-owned CQRS success.

Target pattern:

private static async Task<IResult> GetOfferAsync(
    ...,
    ApiResponseFactory api,
    ...)
{
    var result = await sender.Send(...);
    return api.From(result);
}

Create:

use central Created mapping

set canonical Location

no hand-written 201

Patch/List/Get:

use central Result mapping

No local:

try/catch

ProblemDetails

error switch

language handling

status mapping

12. Pricing / Inventory Alias Boundary

Offer seller price/inventory routes are compatibility aliases but call owner module contracts.

Audit:

ISellerOfferPricingGateway

ISellerOfferInventoryGateway

Goal:
expected owner business failures should also be representable as Result, not exceptions, on these routes.

Preferred:

migrate bounded gateway method return types to Result

update owner implementation and direct call sites

then map result centrally

If this touches Pricing/Inventory contracts, keep scope strictly to these seller-write boundary methods.

Do NOT broadly refactor Pricing/Inventory modules here.

After write success, subsequent GetOfferQuery returns Result and is mapped centrally.

13. Global Exception Pipeline Role After Result

The exception pipeline remains required for:

unexpected exceptions

transitional legacy PlatformHttpException

FluentValidation if retained

infrastructure failures

But expected Offer business outcomes must not need the global exception handler.

Add test proving:

business failure path returns ProblemDetails from Result mapping without throwing

unexpected exception uses global IExceptionHandler

14. MediatR Tracing / Logging with Result

Update tracing/logging semantics:

For Result failure:

Activity should not be marked system Error automatically

add bounded tags:

result.status=business_failure

error.code primary stable code

no error message tag

no payload

Logging:

Result business failure should not emit Error log

unexpected exception remains Error at global boundary

No duplicate logs.

15. Tests

Required focused tests:

Result core:

success invariant

failure invariant

Value access on failure

multi-error deterministic behavior

ApiResponseFactory:

Result<T> success

Result failure 400/404/409/403 through descriptor

Created Result success 201 + Location

Created Result failure maps ProblemDetails

traceId/correlationId preserved

localization preserved

no exception required for business failure

Offer handlers:

each expected failure returns Result.Failure

no expected SemanticException

success Result values correct

Offer endpoints:

no raw Results.Json for Offer-owned CQRS

Result mapping central

Created mapping central

exact status/body compatibility tests

Pricing/Inventory aliases:

expected owner failure maps centrally

success then GetOffer result maps centrally

Unexpected path:

still goes through global exception pipeline

16. Architecture Guards

Add guards:

Offer Application handler return type is Result/Result<T>

no expected SemanticException throw in Offer Application

no raw Results.Json in Offer-owned seller CQRS routes

no endpoint branch on Result error code

no static IHttpContextAccessor/result service locator

no duplicate error model

no code-name status heuristic

no local ProblemDetails mapping

no local localization switch

If Domain still contains SemanticException after chosen strategy:
guard must prove it is not used for expected Golden HTTP business flow.
Prefer eliminating it from Offer Domain expected paths.

17. Compatibility Scan

Search repo for current consumers of:

CreateOfferCommand

UpdateOfferCommand

GetOfferQuery

ListSellerOffersQuery

seller Pricing/Inventory write gateways

Update compile-safe call sites.

Do not use adapter hacks that unwrap .Value without checking failure.

18. Validation

Run:

BuildingBlocks.Tests

Offer.Tests

affected Pricing/Inventory tests if contracts changed

Host focused tests

Host build

backend solution build

Expected:
0 errors
0 new warnings

No frontend changes.

19. Anti-Pattern Gate

Reject:

exception as expected Offer business flow

try/catch SemanticException in handlers

static service locator ApiResults

endpoint manual status mapping

endpoint Results.Json for Result success

duplicated Error/ErrorDescriptor taxonomy

.Value access without success check

catch-all failure conversion

response-shape breaking without compatibility evidence

magic retry/sleep

suppressed failing tests

Expected:
AntiPattern-Gate: CLEAN

20. Recovery State

On success:

Foundation:
RESULT_PATTERN_FOUNDATION_COMPLETE

Offer:
COMPLETE_REFERENCE_PATTERN

Golden reference now explicitly includes:

Result/Result<T> for expected business outcomes

ApiResponseFactory/mapper for success + failure

exception pipeline only for exceptional/transitional paths

Then resume:
TB-TMAR-REFBATCH-TP-001

Update:

Master Recovery

Architect Bootstrap

Capability Map

recovery-sot

Remove any stale claim that the Golden pattern is exception-first for expected business failures.

21. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Current-Result-Gap
Result-Core
Api-Success-Contract
ApiResponseFactory-Result-Mapping
Safe-Failure-Mapping
Offer-CQRS-Adoption
Domain-Result-Strategy
Offer-Endpoint-Adoption
Pricing-Inventory-Alias-Adoption
Exception-Pipeline-Role
Tracing-Logging-Result-Semantics
Compatibility-Scan
Architecture-Guards
Focused-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Foundation-State
Offer-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Module-Recovery-State
Next-Recommended-Task

Expected:
Module-Recovery-State: RESULT_PATTERN_FOUNDATION_COMPLETE
Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE
Offer-State: COMPLETE_REFERENCE_PATTERN
Next-Recommended-Task: TB-TMAR-REFBATCH-TP-001

If any expected Offer business path still requires SemanticException:
Module-Recovery-State: INCOMPLETE
and recommend:
TB-TMAR-FND-RESULT-001-R2

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK