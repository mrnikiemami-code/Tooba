PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CART-MULTICURRENCY-LINES-001
Parent-Task: TB-TMAR-CART-MULTICURRENCY-AUDIT-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: CART_MULTICURRENCY_LINES
Title: Make Cart line currency authoritative and add per-currency totals
Backend-Only: YES

Architect verdict on parent

TB-TMAR-CART-MULTICURRENCY-AUDIT-001 is ARCHITECT-ACCEPTED.

Verified on main:

audit commit 28208446347c1829f281b7008a7183beef044448 exists

no production code changed in the audit

StoreContext golden acceptance was stamped correctly

PRICING_CURRENCY_SELECTION_BLOCKER is real

CartLine.QuotedCurrency is already the correct line-level currency truth

scalar Cart subtotal is invalid for mixed currencies

Closed architecture decision

Resolve the Pricing currency-selection blocker as follows:

StoreContext.DefaultCurrency is ONLY the default selection.

Add-line may carry an OPTIONAL requested currency chosen by the caller.

Requested currency is selection input, NOT pricing authority.

Pricing remains authoritative:

Cart requests a quote for the selected currency.

if Pricing has no valid active quote in that currency, the line cannot be priced.

For a NEW offer line:

selected currency = requested currency when supplied;

otherwise cart DefaultCurrency.

For an EXISTING offer line:

the line's existing QuotedCurrency is sticky/authoritative for requote/increase;

do NOT silently switch that line to another currency.

Different offers MAY have different line currencies in the same Cart.

No FX conversion is introduced.

No Order/Checkout/Payment behavior is changed.

One objective only

Implement one coherent Cart multi-currency slice:

remove cart-level currency as transaction authority;

keep a persisted cart DefaultCurrency only as default selection;

use line-level quoted currency for pricing/repricing;

allow new lines to choose an explicit currency;

expose totals grouped by currency;

never sum unlike currencies.

Do NOT implement Order/Checkout/Payment multi-currency in this task.

Exact production files

Primary files:

src/backend/Modules/Cart/Tooba.Cart.Domain/Aggregates/ShoppingCart.cs

src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartDirectory.cs

src/backend/Modules/Cart/Tooba.Cart.Application/Commands/AddCartLine/AddCartLineCommand.cs

src/backend/Modules/Cart/Tooba.Cart.Application/Commands/AddCartLine/AddCartLineCommandValidator.cs

src/backend/Modules/Cart/Tooba.Cart.Endpoints/Storefront/CartStorefrontEndpoints.cs

src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Directories/CartDirectory.cs

src/backend/Modules/Cart/Tooba.Cart.Application/Presentation/CartPresentationComposer.cs

src/backend/Modules/Cart/Tooba.Cart.Contracts/Checkout/CartContracts.cs

src/backend/Modules/Cart/Tooba.Cart.Contracts/Presentation/CartPresentationContracts.cs

src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Persistence/CartDbContext.cs

src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Persistence/Migrations/CartDbContextModelSnapshot.cs

Read-only unless compilation proves a direct Cart contract consumer needs a mechanical compatibility edit:

Cart tests

Host Cart architecture guards

Do NOT change Pricing Contracts/Infrastructure.
Do NOT change Offer.
Do NOT change Order/Checkout/Payment business logic.
Do NOT change frontend.

A. ShoppingCart semantics

Rename:

ShoppingCart.Currency -> ShoppingCart.DefaultCurrency

Rename creation parameters:

currency -> defaultCurrency

Keep validation:

non-empty

canonical 3-character currency code semantics as today

But update documentation to state:

this is default selection only

it is NOT the currency invariant of all CartLines

Persistence:

map ShoppingCart.DefaultCurrency to the EXISTING DB column "currency"

no DB migration

no schema change

update model snapshot so future EF diffs do not try to rename/drop the existing column

Do NOT rename the physical database column in this task.

B. Add line request + CQRS

Wire model:

CartAddLineRequest

add optional:
string? Currency = null

Command:

AddCartLineCommand

add optional:
string? Currency

Endpoint passes body.Currency to command.

FluentValidation:

currency optional

when present, trim/shape validation must require exactly 3 characters

do not validate price existence in FluentValidation

do not add business logic to validator

Keep:
Endpoints -> ISender -> Application

MediatR 12.5 unchanged.

C. ICartDirectory contract

Change:

AddOrIncreaseLineAsync(...)

to carry:
string? requestedCurrency

Place it adjacent to Offer/quantity inputs in a clear position.

Do NOT add StoreContext types to Cart.Application.
Cart.Application continues to own its existing port boundary.

D. Pricing selection behavior in CartDirectory

Introduce a small private currency-selection helper in CartDirectory or a cohesive Cart-owned helper if needed.
Do NOT create a god-file.

For NEW line:

if requestedCurrency nonblank:

normalize/validate with existing Pricing.Contracts.CurrencyCode

use it for campaign/base quote resolution

else:

use cart.DefaultCurrency

For EXISTING same-offer line:

use existing line.QuotedCurrency

if missing/null, fail closed with stable code:
cart.line.currency_missing

do NOT switch it to requestedCurrency during increase/requote

PriceQuote.Currency returned from Pricing is authoritative and must be persisted into CartLine.QuotedCurrency.

Different lines may have different QuotedCurrency.

Do NOT compare quote currency against cart.DefaultCurrency.
Do NOT reject a second line because another line has another currency.

E. Change quantity + revalidation

Every requote of an existing line must use that line's own QuotedCurrency.

Update:

ChangeLineCoreAsync

RevalidateCampaignQuotesAsync

any exact current requote path in CartDirectory

No cart.DefaultCurrency fallback for existing lines.

F. Merge behavior

Remove:
source.QuotedCurrency ?? target.Currency

Rules:

source-only line keeps source.QuotedCurrency as selected currency

existing target same-offer line keeps its existing QuotedCurrency

if required line currency is missing, fail closed with cart.line.currency_missing

never fall back to cart.DefaultCurrency during merge of an already-quoted line

Do not create duplicate same-offer lines; preserve current unique (CartId, OfferId) behavior.

G. Contracts
CartSnapshot

Rename:
Currency -> DefaultCurrency

Document it as default-selection metadata, not line/order currency.

Keep:
CartLineSnapshot.QuotedCurrency

unchanged.

CartLineView

Keep:
Currency

as the line's quoted currency.

It must come only from line currency truth.

CartPage

Replace ambiguous single-currency presentation with:

string DefaultCurrency

IReadOnlyList<CartCurrencyTotal> TotalsByCurrency

Add:

public sealed record CartCurrencyTotal(string Currency, decimal SubtotalExclusiveOfTax);

Remove the single scalar page-level Currency meaning.
Remove the single scalar page-level SubtotalExclusiveOfTax.

This is BREAKING_PRE_RELEASE_SAFE and intentionally backend-only.

For Converted Cart:

lines remain empty as today

TotalsByCurrency must be empty

DefaultCurrency remains available as metadata

H. Presentation

Remove:
line.QuotedCurrency ?? snapshot.Currency

Line currency:

require line.QuotedCurrency

fail closed with stable semantic path if missing

no default-currency fallback

Compute:
TotalsByCurrency

by grouping line amounts by line currency.

Example:

USD line totals -> one USD total

IRR line totals -> one IRR total

Never compute:
10 USD + 500000 IRR

into one scalar.

Ordering:

deterministic ordinal currency order.

I. Error semantics

Use existing Cart semantic/exception mapping style.

Required new stable code only if needed:
cart.line.currency_missing

Do not expose localized prose from Domain/Application.
Do not parse exception messages for classification.

If a public error catalog requires registration, make the smallest Cart-owned update.

J. No Pricing redesign

Do NOT change:

PriceResolutionQuery

PriceQuote

IPriceLookupGateway

Pricing DB/model

The requested/default currency is only the selector passed into existing Pricing Contracts.

Pricing remains the authority for whether a quote exists.

K. Tests / guards

Add focused coverage for:

Add line without Currency uses cart.DefaultCurrency.

Add new line with explicit USD can coexist with another IRR line.

Pricing quote currency is stored as line.QuotedCurrency.

Existing line increase requotes in existing line currency even if request supplies another currency.

Change quantity uses line.QuotedCurrency.

Merge source-only line preserves source currency.

Merge same-offer target line preserves target line currency.

missing quoted currency fails closed; no default fallback.

CartPage produces TotalsByCurrency with separate USD/IRR totals.

CartPage never exposes one cross-currency subtotal.

CartSnapshot exposes DefaultCurrency, not Currency.

ShoppingCart.DefaultCurrency maps to physical column currency.

no migration/schema change generated.

Cart remains ARCH-COMPLETE-002 structurally certified.

Strengthen durable Cart guard so future code cannot reintroduce:

cart.Currency as price-resolution selector

QuotedCurrency ?? ...DefaultCurrency

scalar cross-currency subtotal

line-currency mismatch rejection based merely on another line's currency

Golden locks

Preserve:

ARCH-COMPLETE-001/002

ARCH-CQRS-001/002

ARCH-VAL-001

ARCH-CONTRACT-001

ARCH-OWN-001

ARCH-DB-001 / ARCH-DATA-001 / ARCH-READ-001

ARCH-HOST-001

ARCH-FOLDER-OWNERSHIP-001

ARCH-SIZE-001/002

ARCH-MODULE-FILE-001

ARCH-NOWORKAROUND-001

ARCH-TX-001

ARCH-FE-FREEZE-001

ARCH-USERWORK-001

ARCH-BASELINE-001

No new cross-module DbContext.
No Host business implementation.
No frontend changes.

Compile fallout rule

If renaming CartSnapshot/CartPage causes compile failures in other backend modules:

make ONLY mechanical member-name/constructor compatibility edits required to compile;

do NOT change their business behavior;

record each file in Result.

If Order/Checkout/Payment requires a business decision rather than a mechanical compile fix:
STOP with INCOMPLETE.
Do not decide that business behavior inside this task.

Validation

Run only:

AddCartLine command/validator focused tests

Cart domain/infrastructure multi-currency focused tests

Cart presentation focused tests

HostCartResidualGuardTests

TmarCompleteReferenceStructureGateTests

TmarDurableGuardTests

full Tooba.Cart.Tests

one final dotnet build src/backend/Tooba.slnx

Do not run broad unrelated test suites.

Evidence

Create:

docs/evidence/TB-TMAR-CART-MULTICURRENCY-LINES-001/cart-multicurrency-lines.md

Record:

architect blocker resolution

default vs line currency semantics

exact pricing selection rule

merge/requote rules

persistence no-schema-change proof

TotalsByCurrency contract

compile-only external edits, if any

explicitly deferred Order/Checkout/Payment decisions

validation results

Recovery SoT

On PASS:

Cart remains COMPLETE_REFERENCE_PATTERN

Cart remains ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

StoreContext remains PLATFORM_CONTEXT_REFERENCE_PATTERN + STRUCTURE_CERTIFIED

Order certification unchanged

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

Record Cart multi-currency state:
LINE_LEVEL_CURRENCY_AUTHORITY_WITH_DEFAULT_SELECTION_AND_TOTALS_BY_CURRENCY

Do NOT claim Order/Checkout/Payment multi-currency support.

Set:
nextTask = USER_REVIEW_CART_MULTICURRENCY_LINES_001

PASS criteria

PASS only if:

ShoppingCart no longer has transaction-semantic Currency; only DefaultCurrency metadata

physical DB column stays currency

no migration/schema change

new line may explicitly choose currency or fall back to DefaultCurrency

Pricing remains authority for the quote

existing line requotes use its own QuotedCurrency

mixed-currency lines can coexist

no cart-level currency equality invariant exists

presentation totals are grouped per currency

no cross-currency scalar subtotal remains in CartPage

Cart stays Golden/ARCH-COMPLETE-002 certified

Order/Checkout/Payment business behavior untouched

frontend untouched

focused tests + Cart tests + build pass

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-CART-MULTICURRENCY-LINES-001
Parent-Task: TB-TMAR-CART-MULTICURRENCY-AUDIT-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Audit-Acceptance-State:
Pricing-Blocker-Resolution:
ShoppingCart-DefaultCurrency-State:
Physical-DB-Column-State:
Schema-Migration-State:
AddLine-Currency-Selection:
Existing-Line-Requote-State:
Merge-Currency-State:
CartLine-Currency-Authority:
CartSnapshot-Contract-State:
CartPage-Contract-State:
TotalsByCurrency-State:
CrossCurrency-Subtotal-State:
Pricing-Contracts-State:
CQRS-MediatR-State:
FluentValidation-State:
Compile-Only-External-Edits:
Architecture-Guards:
Focused-Validation:
Cart-Tests:
Full-Build:
Cart-Certification-State:
StoreContext-Certification-State:
Order-Certification-State:
Checkout-State:
Frontend-Production-Changes:
Deferred-Order-Checkout-Payment:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not start Order multi-currency.
Do not resume Checkout.
Do not modify Payment.
Do not start Shared-DB.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
