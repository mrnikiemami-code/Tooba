PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-001

Parent-Task:
TB-TMAR-REFBATCH-TP-RESULT-001

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
NEXT_REFERENCE_MODULE_BATCH

Title:
Inventory + Promotion Fast-Safe Golden Recovery Batch

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Accepted Golden

Foundation: RESULT_PATTERN_FOUNDATION_COMPLETE
Offer: COMPLETE_REFERENCE_PATTERN
Tax: COMPLETE_REFERENCE_PATTERN
Pricing: COMPLETE_REFERENCE_PATTERN
Checkout: PAUSED_AT_SAFE_W5_CHECKPOINT
Frontend: frozen

Golden rules:

Result/Result<T> for expected business failures

ApiResponseFactory central success/failure mapping

ErrorDescriptor/localization/ProblemDetails central

MediatR CQRS for owned HTTP/Application use cases

IClock/IIdGenerator

no Host DbContext/business authority

Contracts for module boundaries

IModuleCallTracer for meaningful cross-module calls

no TypeForwardedTo / foreign Application or Domain leakage

no cross-module SQL/FK

no raw Results.Json for owned Result-based HTTP use cases

no frontend changes

Objective

Recover Inventory + Promotion in one bounded Fast-Safe batch.
Do NOT add Cart/Order/Checkout to this batch.
Do NOT reopen closed Offer/Pricing/Foundation unless a direct compile/shared-contract defect proves necessary.

Speed rule

Audit + repair + guard + focused tests in one pass.
No broad re-audit of closed modules.
No full Host integration suite merely for reassurance.
No WebApplicationFactory expansion unless changed HTTP behavior requires it.
One final backend build.

Git safety

Verify main, HEAD==origin/main, staged=0, protected 18ca10c9 ancestor, stashes untouched, .rar untouched.
Forbidden: reset, clean, destructive restore/checkout, unsafe rebase, stash pop/drop, broad git add .

Evidence:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001/recovery-start.md

Inventory

Audit structure/ownership and repair to Golden.

Domain:

no Host/Infrastructure/ASP.NET/EF

no foreign Application or foreign Domain implementation

no localized user-facing exception prose

no expected-business SemanticException control flow where Result is canonical

Application:

owned commands/queries use MediatR

expected failures return Result/Result<T>

no DbContext/Host/foreign Application

no DateTime.UtcNow/DateTimeOffset.UtcNow/Guid.NewGuid/UuidV7.New

Contracts:

stable Inventory public ports/types

seller inventory write contract remains stable

Checkout reservation/lifecycle ports remain stable

no Domain namespace masquerading

no TypeForwardedTo

Infrastructure:

owns InventoryDbContext/config/migrations

seller-write expected failures return Result

no cross-module SQL/FK

meaningful cross-module access through contracts/gates

Endpoints:

thin

ISender for owned use cases

ApiResponseFactory for Result mapping

no try/catch business mapping

no raw Results.Json for owned Result paths

no local Accept-Language/ProblemDetails/status mapping

Seller inventory alias:
Verify ISellerOfferInventoryGateway.SetInventoryAsync(...) -> Task<Result>.
Expected failures such as invalid quantity/reason/offer missing/seller mismatch => Result.Failure.
Success => Result.Success.
Unexpected infrastructure failures still throw.
No catch-all Exception->Result.

Checkout-related Inventory ports:
Only repair architecture/result/ownership leaks.
Do NOT resume Checkout process-manager work.

Promotion

Audit structure/ownership and repair to Golden.

Domain:

Promotion owns campaign/promotion rules

no Pricing/Offer Domain implementation refs

no Host/HTTP/EF leakage

expected invariant failures use Result where they cross Application use cases

no localized exception prose

Application:

owned HTTP/admin/runtime use cases via MediatR

Result/Result<T> for expected failures

no foreign Application

no DbContext/Host

Contracts ports for Pricing/Offer/Cart interactions

Contracts:
Preserve campaign cart price authority, checkout promotion port, campaign/Offer boundary DTOs/enums where applicable.
No TypeForwardedTo or Domain namespace leakage.

Infrastructure:

owns PromotionDbContext/migrations

no foreign DbContext/cross-module SQL

IClock/IIdGenerator

IModuleCallTracer for genuine cross-module calls

no raw ActivitySource/StartActivity

HTTP/admin campaign surface:
If Promotion owns use case: ISender + Result + ApiResponseFactory.
If Host owns transport: Host transport/composition only; no PromotionDbContext, aggregate mutation, or business calculation.

Do not duplicate Pricing authority or move Cart/Checkout logic into Promotion.

Focused Foundation scan

Scan only Inventory, Promotion, and direct Host transport/composition call sites for:
PlatformHttpException, SemanticException, Results.Json, ProblemDetails, Accept-Language branches, ex.Message response leakage, UtcNow, Guid.NewGuid, UuidV7.New, ActivitySource/StartActivity, TypeForwardedTo, foreign .Application/.Domain refs, Host InventoryDbContext/PromotionDbContext, direct Host aggregate mutation.

Classify hits as canonical / justified / repaired violation / blocker.

Evidence:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001/foundation-adoption-scan.md
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001/host-leak-scan.md

Result Pattern rule

Use Result only for explicit expected business outcomes.
Do not wrap pure lookups/null semantics or canonical domain outcome types just for uniformity.
Unexpected exceptions remain exceptions.
No duplicate error taxonomy.

Error/localization

For actual HTTP/business errors introduced or migrated:
explicit ErrorDescriptor, stable code, classification, status, localization key, safe fallback.
English default + Persian where user-facing.
No ceremonial resx.

Observability

Preserve existing OTel/correlation/logging foundation.
Result failure = business_failure, not system error.
No duplicate spans or ActivitySource.
Add IModuleCallTracer only for real cross-module calls.

Guards

Add only high-value regression guards.
Inventory: seller write stays Task<Result>, no expected SemanticException on seam, Host no InventoryDbContext.
Promotion: no foreign App/Domain refs, Host no PromotionDbContext/business mutation, owned HTTP paths use Result+ApiResponseFactory, no TypeForwardedTo/raw StartActivity.
Avoid duplicate global guards.

Validation budget

Required:

Tooba.Inventory.Tests

Tooba.Promotion.Tests

focused Host tests only for changed Host seams

dotnet build src/backend/Tooba.slnx

Conditional:

Offer.Tests only if Offer production changes

Pricing.Tests only if Pricing production/contracts change

BuildingBlocks.Tests only if BuildingBlocks production changes

Cart/Order tests only if their call sites/contracts change

No unrelated full suites.
No retry/sleep workaround.

Evidence

Write:

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001/inventory-audit.md

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001/promotion-audit.md

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001/foundation-adoption-scan.md

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001/host-leak-scan.md

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001/recovery-sot.md

Update only affected architecture SoT sections.

Success

Inventory-State: COMPLETE_REFERENCE_PATTERN
Promotion-State: COMPLETE_REFERENCE_PATTERN
Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE
Offer-State: COMPLETE_REFERENCE_PATTERN
Pricing-State: COMPLETE_REFERENCE_PATTERN
Tax-State: COMPLETE_REFERENCE_PATTERN
Batch-State: COMPLETE
Module-Recovery-State: NEXT_REFERENCE_BATCH_COMPLETE
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Frontend-Production-Changes: NONE
Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-002

If one module is blocked, preserve the clean module's accepted state and issue a focused repair for only the blocked module.

AntiPattern Gate

Reject broad rewrite, Host business logic/DbContext, foreign App/Domain leakage, TypeForwardedTo, expected-business exceptions where Result is canonical, endpoint manual mapping, raw Results.Json for owned Result paths, direct Accept-Language branches, raw StartActivity duplication, DateTime/Guid bypass, catch-all Exception->Result, fake tests/workarounds, polling/magic sleeps, frontend changes.

Expected:
AntiPattern-Gate: CLEAN

Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:
Summary
Program-Name
Track
Recovery-Start
Inventory-Audit
Inventory-Repairs
Inventory-Result-Adoption
Inventory-Foundation-Adoption
Inventory-Guards
Inventory-Validation
Inventory-State
Promotion-Audit
Promotion-Repairs
Promotion-Result-Adoption
Promotion-Foundation-Adoption
Promotion-Guards
Promotion-Validation
Promotion-State
Host-Leak-Scan
Foundation-Adoption-Scan
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Batch-State
Foundation-State
Offer-State
Pricing-State
Tax-State
Checkout-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Module-Recovery-State
Next-Recommended-Task

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK