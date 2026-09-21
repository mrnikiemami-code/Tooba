PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-REFBATCH-TP-RESULT-001

Parent-Task:
TB-TMAR-FND-RESULT-001-R1

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Mode:
FAST-SAFE

Track:
REFERENCE_MODULE_RESULT_DELTA

Title:
Tax + Pricing Result Pattern Delta against final Golden Offer/Foundation

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Accepted baseline

Verified Architect state after TB-TMAR-FND-RESULT-001-R1:

Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE

Offer-State: COMPLETE_REFERENCE_PATTERN

ApiResponseFactory now maps Result/Result<T>/Created centrally

Offer seller CQRS uses Result<T>

expected Offer business failures do not require SemanticException

Result business failure is not treated as system exception in tracing/logging

seller HTTP success body remains raw DTO/array for compatibility

Tax/Pricing prior batch already completed architecture cleanup

Current repository main is ahead of the worker-reported implementation commit due Bridge/tip-alignment.
Use actual current main as source of truth.

IMPORTANT:
Do NOT reopen Offer unless a shared-contract compile break caused by this task proves necessary.

1. Objective

Perform ONLY the Result Pattern delta required after the final Golden changed.

This is NOT another full Tax/Pricing recovery.
Do not redo audits already closed.

Goal:

Pricing expected seller-write business failures align with Result Pattern

Tax is changed ONLY if it actually owns an expected business outcome crossing an HTTP/Application boundary that should use Result

no ceremonial Result adoption

no next-module work

2. Speed rule

This task is intentionally small.

Do NOT run broad/redundant suites.

Validation budget:

affected Pricing tests

affected Tax tests ONLY if Tax production code changes

focused BuildingBlocks/Offer test ONLY if shared Result/API code changes unexpectedly

backend build

No full Host integration suite unless compile/runtime evidence requires it.
No WebApplicationFactory test expansion merely for reassurance.
No retry/sleep workaround.

3. Git safety

Verify:

branch main

HEAD == origin/main

staged 0

protected 18ca10c9 remains ancestor

stashes untouched

user .rar untouched

Forbidden:

reset

clean

destructive restore/checkout

unsafe rebase

stash pop/drop

broad git add .

Evidence:
docs/evidence/TB-TMAR-REFBATCH-TP-RESULT-001/recovery-start.md

4. Pricing delta

Inspect every production implementation/call site of the seller pricing write boundary changed by Result task.

Required outcome:

ISellerOfferPricingGateway.SetPriceAsync(...) returns canonical Task<Result>

implementation returns Result.Failure(SemanticError) for EXPECTED business failures

implementation returns Result.Success() on success

no expected SemanticException control flow remains on this seller-write path

unexpected DB/system failures still throw

no catch-all Exception → Result conversion

no duplicate Error model

no endpoint-local status/error mapping

Offer endpoint remains central ApiResponseFactory consumer

Expected Pricing seller failures include, where applicable:

invalid amount

offer not found

seller mismatch/authorization ownership failure

unsupported currency/market/qualifier business rule

Do not invent new errors if the code does not have those cases.

Preserve:

existing Pricing ErrorDescriptor codes/localization

IModuleCallTracer Pricing→Offer

Pricing ownership

no Host PricingDbContext

base-price qualifier behavior repaired by prior batch

5. Pricing internal APIs

Do NOT force Result<T> onto:

internal pure queries that cannot produce expected business failure

batch price lookup paths where absence is already represented by nullable/collection semantics

calculation APIs whose existing explicit domain outcome is already canonical

Result is for explicit expected failure semantics, not a blanket wrapper.

6. Tax delta

Tax prior evidence says:

no Tax-owned HTTP route

calculation failure is represented by TaxOutcome

no SemanticException HTTP surface

no ceremonial MediatR/resx

Revalidate this narrowly.

If still true:

make NO Tax production change

explicitly record RESULT_DELTA_NOT_APPLICABLE

keep TaxOutcome as the canonical calculation outcome

Do NOT replace a meaningful Tax domain outcome with Result simply for uniformity.

Only change Tax if concrete current code shows an expected business failure crossing Application/HTTP boundary through exception/raw HTTP mapping.

7. ApiResponseFactory / Result Foundation

Do not redesign the new foundation.

Verify only:

Pricing Result failures are compatible with current SafeErrorMapper/ErrorDescriptor catalog

error code localization remains central

correlation/trace fields remain central

no static service locator

no direct Results.Json introduced in Pricing/Tax endpoints

If no shared defect exists, touch ZERO BuildingBlocks production files.

8. Observability

For Pricing Result business failure:

TracingBehavior sees IResultStatus

result.status=business_failure

stable error.code may be tagged

Activity not marked system Error merely because Result failed

no duplicate ActivitySource

no raw StartActivity

Do not add extra spans.

Tax unchanged if delta not applicable.

9. Guards

Add or minimally adjust guard ONLY where needed to prevent regression:

Pricing:

seller write gateway stays Task<Result>

seller write expected failures do not throw SemanticException

no raw HTTP mapping

no direct Host DbContext

Tax:

no new guard if existing Golden guards already cover it and no code changed

Avoid duplicate guards.

10. Focused validation

Required:

Tooba.Pricing.Tests

Tooba.Tax.Tests only if Tax production changed; otherwise optional/skip

architecture guard containing Pricing Result seam

dotnet build src/backend/Tooba.slnx

Only run Offer tests if Pricing contract changes beyond the already-applied SetPriceAsync Result signature or Offer code changes.
Only run BuildingBlocks tests if BuildingBlocks changes.

Expected:

0 errors

no new warnings attributable to task

11. Evidence

Write:

docs/evidence/TB-TMAR-REFBATCH-TP-RESULT-001/result-delta.md

docs/evidence/TB-TMAR-REFBATCH-TP-RESULT-001/recovery-sot.md

result-delta.md must clearly state:

Pricing changes actually required

Tax RESULT_DELTA_NOT_APPLICABLE or exact necessary repair

exact tests run and why

exact tests deliberately NOT run and why

12. Final state

Success:

Tax-State:
COMPLETE_REFERENCE_PATTERN

Pricing-State:
COMPLETE_REFERENCE_PATTERN

Foundation-State:
RESULT_PATTERN_FOUNDATION_COMPLETE

Offer-State:
COMPLETE_REFERENCE_PATTERN

Module-Recovery-State:
REFERENCE_RESULT_DELTA_COMPLETE

Then and ONLY then:
Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-001

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend:
NONE

13. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Pricing-Result-Delta
Tax-Result-Delta
Foundation-Compatibility
Observability
Architecture-Guards
Focused-Validation
Skipped-Validation
AntiPattern-Gate
Residual-Defects
Tax-State
Pricing-State
Foundation-State
Offer-State
Frontend-Production-Changes
Checkout-State
Git
Blockers
User-Work-Preserved
Module-Recovery-State
Next-Recommended-Task

Expected:
Module-Recovery-State: REFERENCE_RESULT_DELTA_COMPLETE

If a real shared Foundation defect is found:
STOP with INCOMPLETE and report exact defect.
Do not expand scope.

After Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK