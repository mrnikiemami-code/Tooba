PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-STORECONTEXT-FOUNDATION-001
Parent-Task: TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: STORE_CONTEXT_MODULE_FOUNDATION
Title: Extract StoreCommerce From BuildingBlocks Into StoreContext Module
Backend-Only: YES

Architect verdict on parent

TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001 is ACCEPTED.

Verified on main:

implementation commit exists: 98daf3ec7f653424fff8fcf6c4bb004fccf5f4d5

SoT stamp exists: ebad47578cbbd87513aa57f96cf18c47e2eafee4

Production fail-fast validation is present

canonical SalesChannel startup validation is present

Cart production code was not changed

no new fallback was introduced

One objective only

Move StoreCommerce ownership OUT of Tooba.BuildingBlocks and establish a real internal module boundary:

Tooba.StoreContext.Contracts

Tooba.StoreContext.Infrastructure

This is FOUNDATION PHASE ONLY.

Do NOT redesign Shared-DB.
Do NOT move Tenant/Edition/ConnectionReference out of BuildingBlocks.
Do NOT touch Checkout.
Do NOT audit every module.
Do NOT create Endpoints.
Do NOT create an empty/ceremonial Application project.

Architecture decision — CLOSED

TenantId, ToobaEdition, ConnectionReference, CommerceContext remain platform primitives in BuildingBlocks.

Market, Currency, SalesChannel effective storefront/store context do NOT belong in BuildingBlocks.

Canonical StoreContext boundary for this phase:

Tooba.StoreContext.Contracts

must own:

StoreCommerceContext

ICurrentStoreCommerceContext

IStoreCommerceContextAssigner

Tooba.StoreContext.Infrastructure

must own:

scoped StoreCommerceContextAccessor

StoreContextModule : IToobaModule

DI registration for current-context read + assign seams

No DB.
No schema.
No migrations.
No Endpoints.
No MediatR request is required because this phase introduces no application use-case.

Exact production changes
A. New Contracts project

Create:

src/backend/Modules/StoreContext/Tooba.StoreContext.Contracts/Tooba.StoreContext.Contracts.csproj

Create under capability folder, not project root dumping:

src/backend/Modules/StoreContext/Tooba.StoreContext.Contracts/Current/StoreCommerceContext.cs

Namespace MUST be:

Tooba.StoreContext.Contracts.Current

That file owns:

public sealed record StoreCommerceContext(string? Market, string? Currency, string? SalesChannel);

public interface ICurrentStoreCommerceContext

StoreCommerceContext? Current { get; }

public interface IStoreCommerceContextAssigner

void Assign(StoreCommerceContext context);

Do not add Cart, Host, Pricing, Offer implementation dependencies here.
Do not introduce another SalesChannel enum in this task.
SalesChannel remains stable string at this boundary for now.

B. New Infrastructure project

Create:

src/backend/Modules/StoreContext/Tooba.StoreContext.Infrastructure/Tooba.StoreContext.Infrastructure.csproj

References:

Tooba.StoreContext.Contracts

Tooba.ModuleContracts

Create:

src/backend/Modules/StoreContext/Tooba.StoreContext.Infrastructure/Current/StoreCommerceContextAccessor.cs

Namespace:
Tooba.StoreContext.Infrastructure.Current

Requirements:

scoped service

implements BOTH ICurrentStoreCommerceContext and IStoreCommerceContextAssigner

no HttpContext dependency

no static mutable state

no AsyncLocal

fail on null assignment using standard argument guard

Create module composition entry:

src/backend/Modules/StoreContext/Tooba.StoreContext.Infrastructure/StoreContextModule.cs

Namespace:
Tooba.StoreContext.Infrastructure

It must register one scoped accessor instance and expose it through both Contracts interfaces.

Root .cs allowlist for Infrastructure is ONLY:

StoreContextModule.cs

C. BuildingBlocks cleanup

Modify only:

src/backend/BuildingBlocks/Tooba.BuildingBlocks/CommerceContext.cs

Remove:

StoreCommerceContext record entirely

StoreCommerce property/constructor parameter from CommerceContext

Do NOT modify:

ToobaEdition

TenantStatus

TenantId

ConnectionReference

EditionContext

TenantContext

ICurrentCommerceContext

ICurrentEdition

ICurrentTenant

HostNormalizer behavior

After this task, BuildingBlocks must contain ZERO StoreCommerceContext / Market-Currency-SalesChannel effective-store semantics.

D. Host composition

Modify:

src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs

Add exactly one explicit:
new StoreContextModule()

Place it before modules that consume StoreContext, including Cart.

Add required StoreContext Infrastructure project reference to Host.

E. Request path assignment

Modify:

src/backend/Host/Tooba.Host/MultiTenancy/TenantResolutionMiddleware.cs

Do NOT recreate StoreCommerce in BuildingBlocks.

Inject IStoreCommerceContextAssigner.

Keep existing Host/control-plane lookup for this phase.

For a successfully resolved non-skip request:

resolve technical CommerceContext

resolve/select the already-built StoreCommerceContext from ControlPlaneRegistry

assign technical context as today

assign StoreContext through IStoreCommerceContextAssigner

then call _next

Marketplace uses:
_registry.DeploymentStoreCommerce

SingleStore uses:
record.StoreCommerce

Unknown/disabled tenant behavior stays unchanged.

No raw request/header value may become Market/Currency/SalesChannel authority.

F. Host control-plane type references

Modify:

src/backend/Host/Tooba.Host/Configuration/ToobaPlatformOptions.cs

Use:
Tooba.StoreContext.Contracts.Current.StoreCommerceContext

instead of BuildingBlocks StoreCommerceContext.

Preserve parent fail-fast behavior EXACTLY.

Do not move options/registry ownership yet.
Do not change StoreCommerce fallback rules.
Do not change production validation behavior.

G. Background worker path

Modify:

src/backend/Host/Tooba.Host/Outbox/OutboxWorkerSeams.cs

Technical WorkerCommerceContextFactory must continue returning BuildingBlocks CommerceContext WITHOUT StoreCommerce.

Add the smallest Host composition adapter in THIS EXISTING FILE only:

IWorkerStoreCommerceContextFactory is NOT to be invented in Host.

Instead add this interface to StoreContext.Contracts.Current:

IWorkerStoreCommerceContextFactory

StoreCommerceContext FromTarget(ToobaEdition edition, string? tenantId);

StoreContext.Contracts may reference Tooba.BuildingBlocks ONLY for ToobaEdition.

Implement IWorkerStoreCommerceContextFactory in OutboxWorkerSeams.cs as a THIN Host control-plane adapter over ControlPlaneRegistry.
It may only select:

deployment StoreCommerce for Marketplace

active tenant StoreCommerce for SingleStore

It must not contain Market/Currency/SalesChannel business defaults or normalization rules.

Register the adapter in Program.cs.

This Host adapter is explicitly allowed as a temporary platform composition adapter in Foundation Phase and must be recorded in evidence.

H. Cart consumption

Modify:

src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Lifetime/CartCommerceContextResolver.cs

Replace dependency:

ICurrentCommerceContext

with:

ICurrentStoreCommerceContext

Read:
Current

directly.

Preserve current fail-closed stable codes:

cart.commerce.context_unavailable

cart.commerce.market_unconfigured

cart.commerce.currency_unconfigured

cart.commerce.channel_unconfigured

Do NOT add fallback.

Cart Infrastructure must reference StoreContext.Contracts directly.

Modify:

src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Lifetime/CartExpiryWorker.cs

Within each created scope:

assign technical CommerceContext through existing ICommerceContextAssigner

assign StoreCommerceContext through IStoreCommerceContextAssigner

obtain worker store context through IWorkerStoreCommerceContextFactory

then resolve/reconcile Cart

No Cart -> Host reference.

Solution/project registration

Add both new projects to:
src/backend/Tooba.slnx

Do not add unused projects.

Architecture guards

Add a focused guard to existing architecture tests (do not create a large new test file unless necessary) proving:

CommerceContext.cs no longer declares or contains StoreCommerceContext

BuildingBlocks has no StoreCommerceContext

Cart resolver depends on ICurrentStoreCommerceContext, not ICurrentCommerceContext

Cart has no Host dependency

StoreContext.Contracts has no dependency on Cart or Host

StoreContext.Infrastructure root only allows StoreContextModule.cs

new files obey path ↔ namespace

Host StoreContext adapter contains no IR, IRR, Direct, Marketplace fallback literal used as commerce authority

enum/edition comparisons needed for routing are allowed

business fallback values are forbidden

Do not certify StoreContext under ARCH-COMPLETE-002 in this task.
This is a foundation extraction, not certification.

Explicit non-goals

Do NOT:

touch Pricing

touch Offer ownership of SalesChannel

touch Catalog

touch Order

touch Checkout

implement Shared-DB

add StoreContext endpoints

add DB persistence

add MediatR just for ceremony

perform repository-wide search loops

redesign Host control plane

change frontend

change routes

If a required compile dependency outside the listed files is discovered:
make only the minimum project-reference/using compatibility change.
If a NEW architecture decision is required, STOP and return INCOMPLETE.
Do not broaden scope.

Validation

Run only:

focused StoreContext foundation tests/guards

PlatformOptionsValidatorTests

HostCartResidualGuardTests

Cart focused commerce/create-guest tests

TmarDurableGuardTests

dotnet build src/backend/Tooba.slnx

One full build at the end.
Do not run broad unrelated suites.

Evidence

Create:

docs/evidence/TB-TMAR-STORECONTEXT-FOUNDATION-001/store-context-foundation.md

Must record:

why StoreCommerce left BuildingBlocks

exact new ownership

request assignment path

background-worker assignment path

Cart dependency before/after

allowed temporary Host adapter

no Shared-DB claim

no new default

validation results

Recovery SoT

On PASS:

parent fail-fast remains accepted

Cart remains COMPLETE_REFERENCE_PATTERN

Cart remains ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Order remains ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

StoreContext state = FOUNDATION_EXTRACTED_NOT_YET_STRUCTURE_CERTIFIED

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

Set:
nextTask = USER_REVIEW_STORECONTEXT_FOUNDATION_001

Do NOT automatically start relationship cleanup phase.

PASS criteria

PASS only if:

StoreCommerceContext is absent from BuildingBlocks

StoreContext Contracts + Infrastructure projects exist and build

request path assigns StoreContext separately from technical CommerceContext

worker path assigns StoreContext separately

Cart consumes only StoreContext.Contracts for effective store commerce context

Cart has zero Host dependency

existing production fail-fast behavior is preserved

no Market/Currency/SalesChannel default is introduced

no Shared-DB claim/implementation is introduced

frontend untouched

Checkout untouched

focused tests + full build pass

SoT/evidence updated

working tree task-related only

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-STORECONTEXT-FOUNDATION-001
Parent-Task: TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-FailFast-State:
StoreContext-Contracts-State:
StoreContext-Infrastructure-State:
BuildingBlocks-StoreCommerce-State:
Request-Context-Assignment-State:
Worker-Context-Assignment-State:
Cart-Dependency-State:
Host-Adapter-State:
FailFast-Preservation-State:
Default-Authority-State:
SharedDB-State:
Structure-Guard-State:
Focused-Validation:
Full-Build:
Cart-Certification-State:
Order-Certification-State:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After emitting Result:
STOP completely.
Do not start relationship cleanup.
Do not touch Pricing/Offer/Catalog/Order/Checkout.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
