PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-CART-GOLDEN-001-R1

Parent-Task:
TB-TMAR-CART-GOLDEN-001

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
CART_GOLDEN_CLOSURE

Title:
Cart Exception/Result Semantics Closure

Backend-Only:
YES

Architect verdict

Parent PASS is REOPENED.

Direct repository verification ACCEPTS:

real Tooba.Cart.Endpoints project exists

Cart routes moved out of Host

Host calls app.MapCartEndpoints()

Cart Application is registered in CQRS foundation

real MediatR Commands/Queries/Handlers exist

StorefrontCartComposer is deleted

Cart presentation moved to Application

Catalog/Party enrichment is Contracts-only

architecture guards cover endpoint ownership

BUT one serious anti-pattern remains in newly introduced Cart Application code:

src/backend/Modules/Cart/Tooba.Cart.Application/Errors/CartExceptionMapper.cs

It maps InvalidOperationException.Message using:

Persian prose fragments

English prose fragments

Contains(...) heuristics

generic fallback of every other InvalidOperationException to cart.rejected

This violates TMAR rules and the parent task itself:

expected business outcomes must be stable semantic errors

no localized prose matching

no arbitrary unexpected InvalidOperationException swallowed into Result

no workaround that merely makes tests pass

Do NOT start another module.
Cart remains the only active module.

1. Concrete defect

Current CartExceptionMapper.ToSemanticError contains mappings such as:

پیدا نشد

راز

مجوز

کهنه

همزمان

منقضی

رزرو

آزادسازی

موجودی

تعداد

غیرفعال

فقط سبد Active

قابل جهش خط

Offer

Held

and then:
return new SemanticError(CartErrorCodes.Rejected);

for all unmatched InvalidOperationException.

This is not acceptable.

2. Required target

Cart expected failures must originate as stable machine codes or Result semantics.

Preferred target:

Domain/Directory/Ports
→ stable code / typed expected outcome
→ Application handler
→ Result<T>
→ Endpoint ApiResponseFactory

No localized prose parsing.

No arbitrary message parsing.

No generic swallow of unexpected InvalidOperationException.

3. Repair strategy

Audit every Cart production throw new InvalidOperationException(...) and any other expected-business exception source reachable by Cart HTTP use cases.

Classify each occurrence:

A. Expected business failure

replace prose message with stable machine code, OR

return Result / typed outcome at the appropriate owned boundary if already natural

B. Unexpected invariant/programming/system failure

leave as exception

Application must NOT convert it to expected Result

C. Foreign Contracts failure

use stable foreign contract error/outcome

map exact machine code only when Cart owns the presentation semantic

do not parse prose

Do NOT broadly redesign CartDirectory.
Do NOT rewrite unrelated modules.

4. Exact-code mapping only

If a transitional mapper remains, it may map ONLY exact known stable machine codes.

Example acceptable shape:

switch (exception.Message)
{
case "cart.missing": ...
case "cart.version.stale": ...
...
default:
throw;
}

No:

Contains

Persian strings

English prose

heuristic fragments

catch-all -> cart.rejected

If a stable code is foreign-owned, preserve it or explicitly map exact code with evidence.

5. Audit scope

Search all Cart production sources:

Domain

Application

Contracts

Infrastructure

Endpoints

Search relevant foreign contract seams actually invoked by Cart:

Offer.Contracts

Inventory.Contracts

Catalog.Contracts

Only change foreign module code if absolutely required to expose stable contract outcome already semantically owned there.

No broad foreign module recovery.

6. Handler behavior

All Cart MediatR handlers must:

convert only expected known business failures to Result

allow unexpected exceptions to propagate to global exception handling

not duplicate try/catch boilerplate unnecessarily

not hide infrastructure failures

CartExceptionMapper.TryAsync must not be a generic unexpected-exception sink.

7. Stable Cart error codes

Review actual Cart flow and keep only meaningful stable codes.

At minimum likely:

cart.missing

cart.guest.invalid / cart.access.denied

cart.version.conflict

cart.expired

cart.quantity.invalid

cart.line.missing

cart.offer.unavailable

cart.inventory.insufficient

cart.inventory.stale

cart.rejected

checkout.authentication_required where current compatibility requires it

Do not create aliases without need.
Do not change externally visible codes unnecessarily.

8. Behavior preservation

Preserve exact business behavior for:

guest secret invalid

missing cart

current authenticated cart

merge auth requirement

stale expected version

expired cart

invalid quantity

missing line

inactive/unavailable offer

inventory failure

converted/non-active cart rejection

The only intended change is INTERNAL failure classification quality:

expected known business failures → Result

unexpected failures → propagate

HTTP semantic status/code for expected failures should remain as parent task established.

9. Tests

Focused only.

Add/adjust tests proving:

stable exact cart.missing maps correctly

stable exact guest/access code maps correctly

version conflict maps correctly

quantity/line/offer/inventory expected codes map correctly as applicable

localized/prose message is NOT parsed into a business Result

unknown InvalidOperationException propagates

no Persian/English prose matching remains in CartExceptionMapper

existing Cart endpoint tests still pass

Do not add large redundant suites.

10. Architecture guard

Strengthen Cart guard to fail if CartExceptionMapper.cs contains:

.Contains(

Persian Unicode prose literals

"Offer" prose matching

"Held" prose matching

generic fallback of unknown InvalidOperationException to CartErrorCodes.Rejected

Guard should also scan Cart Application for message-based business classification patterns where feasible.

11. Evidence

Create:
docs/evidence/TB-TMAR-CART-GOLDEN-001-R1/

Required:

recovery-start.md

cart-exception-source-audit.md

cart-error-code-map.md

behavior-preservation-audit.md

antipattern-scan.md

recovery-sot.md

cart-exception-source-audit.md:
list every expected-business exception source changed/retained and its stable code.

antipattern-scan.md target:

localized prose matching = 0

Contains-based exception classification = 0

unknown InvalidOperationException swallowed = 0

12. Validation

Required:

Cart.Tests

focused Host Cart endpoint tests only if affected

final dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

Checkout workflow

Tax/Pricing

frontend

unrelated modules

13. Protected state

Must remain:
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Tax/Pricing: untouched
Frontend: untouched
No next module.

Do not alter stashes.
No reset/clean/force push.

14. Success

Only if true:

Cart-HTTP-Ownership:
MODULE_ENDPOINTS

Cart-Endpoints-State:
REAL_PROJECT_PRESENT

Cart-CQRS-State:
MEDIATR_12_5_APPLICATION_HANDLERS

Cart-Host-Routes:
REMOVED

Cart-Host-Composer:
REMOVED

Cart-CrossModule-Boundary:
CONTRACTS_ONLY

Cart-Exception-Classification:
STABLE_CODES_ONLY

Cart-Prose-Mapping:
NONE

Cart-Unexpected-Exception-Swallow:
NONE

Cart-Result-Adoption:
HTTP_USE_CASES_ADOPTED

Cart-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Cart-Architecture-Guards:
ENFORCED

Cart-Behavior-Preservation:
VERIFIED

Cart-State:
COMPLETE_REFERENCE_PATTERN

Next-Recommended-Task:
USER_CART_REVIEW_CHECKPOINT

If any message/prose heuristic remains:
return INCOMPLETE with exact path.

15. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Cart-Exception-Source-Audit
Cart-Error-Code-Map
Cart-Exception-Classification
Cart-Prose-Mapping
Cart-Unexpected-Exception-Swallow
Behavior-Preservation-Audit
AntiPattern-Scan
Architecture-Guards
Cart-Validation
Focused-Validation
Full-Validation
Residual-Defects
Cart-HTTP-Ownership
Cart-Endpoints-State
Cart-CQRS-State
Cart-Host-Routes
Cart-Host-Composer
Cart-CrossModule-Boundary
Cart-Result-Adoption
Cart-Physical-State
Cart-State
Checkout-State
Tax-State
Pricing-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Next-Recommended-Task

After Result:
STOP completely.
Do NOT start another module.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK