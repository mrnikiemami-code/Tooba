PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CART-GOLDEN-001

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-005-R1

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
Cart HTTP Ownership + MediatR CQRS + Host Orchestration Closure

Backend-Only:
YES

Architect decision

User explicitly requested:

focus ONLY on Cart until Cart is truly complete

issue one task, wait for Result, review repo directly, then issue next Cart task if needed

when Cart is complete, STOP and wait for user review

do NOT choose/start the next module

This task is the first Cart-only closure task.

0. Direct repository findings

Architect directly verified current main.

Cart currently has NO MediatR/CQRS application surface

Tooba.Cart.Application.csproj has no MediatR/CQRS foundation reference/registration.

Current Application folders are essentially:

Ports

Lifetime

Conversion

There are no real Cart HTTP Commands/Queries/Handlers.

Cart currently has NO Endpoints project

Current module projects:

Tooba.Cart.Domain

Tooba.Cart.Application

Tooba.Cart.Contracts

Tooba.Cart.Infrastructure

Tooba.Cart.Tests

Missing:

Tooba.Cart.Endpoints

Real Cart HTTP routes are buried in Host StorefrontEndpoints

Current routes:

POST /v1/storefront/cart

GET /v1/storefront/cart/current

GET /v1/storefront/cart/{cartId:guid}

POST /v1/storefront/cart/merge

POST /v1/storefront/cart/{cartId:guid}/lines

PATCH /v1/storefront/cart/{cartId:guid}/lines/{lineId:guid}

DELETE /v1/storefront/cart/{cartId:guid}/lines/{lineId:guid}

Current route flow:
Host StorefrontEndpoints
→ StorefrontCartComposer
→ Cart Directory / Cart Query Gateway
→ CatalogDbContext + Party.Application + Catalog.Application

This violates the TMAR HTTP-owning module rule.

Host StorefrontCartComposer still owns Cart presentation/orchestration

Verified current constructor dependencies include:

ICartDirectory

ICartQueryGateway

CatalogDbContext

IPartyLookupGateway (Party.Application)

ICatalogLookupGateway (Catalog.Application)

CurrentAuthenticatedSession

It directly queries:

Catalog.Variants

Catalog.Products

Catalog.MediaReferences

Catalog.LocalizedTexts

and enriches seller/product/cart presentation inside Host.

This must not remain the Cart business/application presentation authority.

1. Non-negotiable Cart target

Cart is an HTTP-owning module.

Required end-state:

Tooba.Cart.Endpoints
→ ISender
→ Tooba.Cart.Application Command/Query
→ Handler
→ Cart ports / foreign Contracts only
→ Infrastructure

Host:

composition only

calls app.MapCartEndpoints()

no Cart HTTP handlers

no StorefrontCartComposer business/orchestration

no Cart-specific CatalogDbContext query authority

Do not claim Cart COMPLETE without this.

2. Create real Cart Endpoints project

Create:

src/backend/Modules/Cart/Tooba.Cart.Endpoints/

with:

Tooba.Cart.Endpoints.csproj

physical responsibility folders as needed

namespace rooted at Tooba.Cart.Endpoints

Expected project references:

Cart.Application

BuildingBlocks presentation/results as needed

ASP.NET framework reference if required

Must NOT reference:

Host

Cart.Infrastructure

Catalog.Application/Infrastructure

Party.Application/Infrastructure

foreign DbContexts

Add project to src/backend/Tooba.slnx.

3. Move Cart HTTP route ownership

Move ONLY Cart routes out of Host StorefrontEndpoints.

Preserve exact public URLs:

POST /v1/storefront/cart

GET /v1/storefront/cart/current

GET /v1/storefront/cart/{cartId:guid}

POST /v1/storefront/cart/merge

POST /v1/storefront/cart/{cartId:guid}/lines

PATCH /v1/storefront/cart/{cartId:guid}/lines/{lineId:guid}

DELETE /v1/storefront/cart/{cartId:guid}/lines/{lineId:guid}

Host storefront must keep unrelated routes.

After repair Host should call:
app.MapCartEndpoints();

and StorefrontEndpoints must not map Cart routes.

4. MediatR / CQRS

Use existing Tooba CQRS foundation.

MediatR version must resolve to exactly:
12.5.0

Create real application use cases for the seven Cart route families.

Suggested naming:

Commands:

CreateGuestCartCommand

MergeCartAfterLoginCommand

AddCartLineCommand

ChangeCartLineQuantityCommand

RemoveCartLineCommand

Queries:

GetCurrentAuthenticatedCartQuery

GetCartQuery

Use cohesive folders:

Commands/CreateGuestCart/...

Commands/MergeCartAfterLogin/...

Commands/AddCartLine/...

Commands/ChangeCartLineQuantity/...

Commands/RemoveCartLine/...

Queries/GetCurrentCart/...

Queries/GetCart/...

No giant CartHandlers.cs.

Register Cart Application assembly in CQRS foundation.

5. Result pattern

Expected Cart HTTP/business outcomes use:

Result

Result<T>

SemanticError

Cart error catalog/descriptors

ApiResponseFactory in Endpoints

At minimum audit and preserve stable errors for:

cart not found

cart access denied / guest-secret invalid

version conflict

expired cart

invalid quantity

line not found

offer unavailable

inventory unavailable

authentication required where current route semantics require it

Do not invent duplicate taxonomy if stable existing codes already exist.

Do NOT:

expose ex.Message

catch arbitrary Exception into Result

use localized prose in expected failures

manually create {title,errorCode,detail} envelopes

6. Cart presentation ownership

Current StorefrontCartComposer.PresentAsync is not allowed to remain in Host.

Move Cart response composition behind Cart Application.

Preserve current response semantics including:

CartId

Version

Market

Currency

Channel

total quantity

subtotal

status

guest secret behavior

line id

offer id

catalog variant id

seller party id

product id

slug

product title

seller display name

media asset id

quantity

quoted unit amount

line amount

currency

tax-exclusive flag

unit code/display

decimal places

step

availability

merchandising campaign id

Converted cart behavior must remain:

zero quantity/subtotal

empty lines

same status

guest-secret behavior unchanged

7. Foreign data must use Contracts only

Cart Application/Infrastructure must not use foreign implementation layers.

Party

Current Host uses Party.Application.IPartyLookupGateway.

Use existing:
Tooba.Party.Contracts.IPartyLookup
if sufficient.

If insufficient:

minimally extend Party.Contracts

implement in Party module

no broad Party recovery

Catalog

Current Host directly queries CatalogDbContext for:

variant→product

product slug

localized product name

media asset

quantity policy

Inspect and reuse existing Catalog.Contracts, especially Catalog/Cart and ICatalogVariantLookup.

If insufficient:
extract ONE minimal Cart-facing read contract into:
Tooba.Catalog.Contracts.Cart

Implementation belongs to Catalog module, not Host.

No Cart→Catalog.Application/Infrastructure references.
No cross-module SQL.

8. Authentication / guest secret boundary

Do not introduce Cart.Endpoints → Host dependency.

Preserve:

anonymous guest cart

authenticated current cart

guest secret authorization

merge-after-login auth requirement

raw guest secret returned only where current behavior returns it

Use an existing shared neutral authenticated-user abstraction if one exists.
If none exists, extract the smallest transport-neutral current-user abstraction to an appropriate shared building block.

Forbidden:

Cart.Endpoints/Application referencing Tooba.Host.CurrentAuthenticatedSession

service locator

static HttpContextAccessor in Application

Host callback implementing a Cart Application business port

Endpoint may parse transport-only guest secret/version values and pass explicit inputs.

9. Expected-version behavior

Preserve exact optimistic concurrency semantics:

current header/query/body precedence

missing version behavior

stale version error behavior

line mutation semantics

Move transport parsing to Cart.Endpoints.
Keep business enforcement in Application/Domain/Directory.

Do not change concurrency model.

10. StorefrontCartComposer removal

Target:
StorefrontCartComposer.cs deleted.

If Checkout-adjacent Host code consumes it:

replace only with Cart public boundary needed to compile

preserve exact behavior

do NOT resume/rewrite Checkout

No new Host Cart composer wrapper.

11. Checkout freeze protection

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

This task MUST NOT:

change CheckoutProcessManager semantics

alter point-of-no-return

resume W6

redesign cart→checkout consistency

change reservation/payment compensation logic

Any Checkout-adjacent touched file must be compile-only seam adaptation and documented.

12. Physical structure

Cart COMPLETE requires:

Tooba.Cart.Endpoints

endpoint files

wire models only if endpoint-owned

dependency registration only if real

Tooba.Cart.Application

Commands/

Queries/

Models/

Errors/

existing Ports/

existing Lifetime/

existing Conversion/

No root dumping.
Namespace must align with physical folder.

Update Cart architecture guard to enforce Endpoints too.

13. Architecture guard upgrades

Tooba.Cart.Tests/Architecture/CartArchitectureGuardTests.cs

Add guards:

Endpoints:

project exists

path↔namespace aligned

references Application, not Infrastructure

no Host reference

no foreign Application/Infrastructure

no DbContext

uses ISender

uses ApiResponseFactory for failure presentation

no raw ex.Message

no manual semantic error envelopes

Application:

real Commands/Queries exist

MediatR/CQRS present

foreign modules Contracts-only

Host:

StorefrontEndpoints.cs contains no Cart route mappings

StorefrontCartComposer.cs absent

no Host CartDbContext outside bootstrap allowlist

no Host Cart presentation directly using CatalogDbContext

no Host implementation of Cart business ports as workaround

General:

no TypeForwardedTo

no clock/id bypass

no hidden fallbacks

no localized exception prose

no silent catch

no raw StartActivity

root dump=0

14. Focused behavior tests

Required high-value tests only:

Create guest cart

Get guest cart valid secret

Invalid guest secret baseline failure

Current authenticated cart

Unauthenticated current-cart stable failure

Merge after login

Add line and response presentation shape

Change quantity + optimistic version behavior

Remove line

Product/seller/media/quantity-policy enrichment

Converted cart returns empty lines and zero totals

Architecture ownership guard

Reuse existing Host tests where useful.
Do not create redundant test explosion.

15. Behavior-preservation audit

Compare against task-start main.

Classify:

structural-only

endpoint ownership

application boundary

foreign Contracts extraction

intentional behavior repair

accidental behavior change

Target:
accidental behavior change = 0

Do not silently fix unrelated Cart bugs in this task.

16. Evidence

Create:
docs/evidence/TB-TMAR-CART-GOLDEN-001/

Required:

recovery-start.md

cart-http-ownership-audit.md

cart-cqrs-audit.md

cart-host-authority-audit.md

cart-presentation-boundary.md

cart-foreign-contracts-audit.md

cart-physical-tree.md

behavior-preservation-audit.md

architecture-guard-audit.md

recovery-sot.md

cart-physical-tree.md lists every handwritten production Cart .cs:

relative path

namespace

responsibility

17. Fast-Safe validation

Required:

Cart.Tests

focused Host cart/checkout-identity tests affected by seam

Catalog/Party focused build/tests ONLY if Contracts changed

final:
dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

full Checkout workflow

Tax/Pricing

frontend

unrelated modules

No retry/sleep workaround.

18. Protected areas

Do NOT modify:

Tax

Pricing

frontend

Checkout workflow semantics

unrelated modules except minimal Catalog/Party Contracts seam

stashes

.rar files

No force push.
No reset/clean.
No broad git add ..

19. Success criteria

PASS only if all are true:

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

Cart-Host-CatalogDbAuthority:
NONE

Cart-CrossModule-Boundary:
CONTRACTS_ONLY

Cart-Result-Adoption:
HTTP_USE_CASES_ADOPTED

Cart-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Cart-Architecture-Guards:
ENFORCED

Cart-Behavior-Preservation:
VERIFIED

Checkout-State:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend-Production-Changes:
NONE

Tax-State:
DEFERRED_PHYSICAL_REVIEW_BY_USER

Pricing-State:
DEFERRED_PHYSICAL_REVIEW_BY_USER

20. Repository-wide Cart completion scan

Before PASS, search entire repository for:

CartDbContext

StorefrontCartComposer

ICartDirectory usage outside Cart

ICartQueryGateway usage outside Cart

/cart

Cart Application ports implemented in Host

Cart-specific manual error mapping

Any remaining production Cart ownership leak:
return INCOMPLETE with exact paths.

21. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Cart-HTTP-Ownership-Audit
Cart-Endpoints-State
Cart-CQRS-State
Cart-Application-UseCases
Cart-Result-Adoption
Cart-Presentation-Boundary
Cart-Foreign-Contracts-Audit
Cart-Host-Authority-Audit
Cart-Host-Routes
Cart-Host-Composer
Cart-Host-CatalogDbAuthority
Cart-CrossModule-Boundary
Cart-Physical-State
Cart-Architecture-Guards
Cart-Behavior-Preservation
Checkout-Impact
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Cart-State
Checkout-State
Tax-State
Pricing-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Next-Recommended-Task

If Cart is not fully complete:
Next-Recommended-Task:
TB-TMAR-CART-GOLDEN-001-R1

If Cart is truly complete:
Cart-State:
COMPLETE_REFERENCE_PATTERN
Next-Recommended-Task:
USER_CART_REVIEW_CHECKPOINT

After Result:
STOP completely.
Do NOT poll.
Do NOT start another module.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK