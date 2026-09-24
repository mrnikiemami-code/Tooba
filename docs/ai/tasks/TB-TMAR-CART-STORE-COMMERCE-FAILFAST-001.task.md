PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001
Parent-Task: TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: STORE_COMMERCE_FAILFAST
Title: Fail Fast on Invalid StoreCommerce Configuration
Backend-Only: YES

Architect decision

R1 is accepted for the Cart authority repair itself.

This task fixes ONE remaining issue only:
Production can currently start with missing/invalid StoreCommerce configuration and fail later when Cart is used.

Do NOT work on Shared-DB edition/runtime in this task.
Do NOT reopen Cart Host cleanup, Cart worker ownership, Cart localization, persistence-hours, CQRS, endpoints, Checkout, Order, or frontend.

Exact files to inspect/change

Primary:

src/backend/Host/Tooba.Host/Configuration/ToobaPlatformOptions.cs

src/backend/Host/Tooba.Host.Tests/TenantResolutionTests.cs

Read-only unless strictly required:

src/backend/BuildingBlocks/Tooba.BuildingBlocks/CommerceContext.cs

src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Lifetime/CartCommerceContextResolver.cs

Do not perform a repository-wide architecture audit.

Required behavior
Production Marketplace

When Tooba = Marketplace, startup validation must fail if any are missing/blank:

Tooba:StoreCommerce

Tooba:StoreCommerce

Tooba:StoreCommerce

Production SingleStore

For every ACTIVE tenant, startup validation must fail if effective StoreCommerce is incomplete:

Market missing after existing DefaultMarketReference fallback

Currency missing

SalesChannel missing

Disabled/Suspended tenants do not need runtime commerce validation.

SalesChannel validation

Validate StoreCommerce.SalesChannel at startup against canonical:
Tooba.Offer.Contracts.Dtos.SalesChannel

Invalid values such as Marketplce must fail startup validation.

Do NOT introduce a second enum.
Do NOT move SalesChannel ownership into BuildingBlocks.
Do NOT hardcode Direct or Marketplace as fallback.

Currency

Require non-empty configured currency.
Do not invent a currency list here.
Do not add IRR/USD/EUR hardcodes.

Market

Keep existing SingleStore behavior:
explicit StoreCommerce.Market first, otherwise Tenant.DefaultMarketReference.

No new Cart-side fallback.

Comment consistency

Fix the misleading TenantRecordOptions.StoreCommerce comment.

Correct meaning:

Market may fall back to DefaultMarketReference

Currency and SalesChannel require explicit tenant StoreCommerce values

Do not change runtime inheritance semantics in this task.

Tests

Add focused tests to existing PlatformOptionsValidatorTests in:
src/backend/Host/Tooba.Host.Tests/TenantResolutionTests.cs

Required cases:

Production Marketplace rejects missing StoreCommerce.

Production Marketplace rejects invalid SalesChannel.

Production Marketplace accepts complete valid StoreCommerce.

Production SingleStore rejects active tenant missing Currency.

Production SingleStore rejects active tenant missing SalesChannel.

Production SingleStore rejects invalid SalesChannel.

Production SingleStore accepts Market from DefaultMarketReference when Currency and SalesChannel are valid.

Disabled tenant may remain without StoreCommerce and must not block startup.

Keep tests focused. Do not modify unrelated tenant-resolution scenarios except helper data needed to keep them valid.

Architecture locks

Preserve:

ARCH-HOST-001

ARCH-CONTRACT-001

ARCH-NOWORKAROUND-001

ARCH-FOLDER-OWNERSHIP-001

ARCH-SIZE-001/002

ARCH-FE-FREEZE-001

ARCH-USERWORK-001

ARCH-BASELINE-001

No new production file is expected.
No new project/reference should be added unless Tooba.Host lacks the required Offer.Contracts reference; if required, add only that explicit Contracts reference and report it.

Validation

Run ONLY:

focused PlatformOptionsValidatorTests

HostCartResidualGuardTests

TmarDurableGuardTests

dotnet build src/backend/Tooba.slnx

Do not run broad unrelated suites.

PASS criteria

PASS only if:

Production Marketplace cannot start with incomplete StoreCommerce.

Production SingleStore cannot start with incomplete StoreCommerce for ACTIVE tenants.

invalid SalesChannel fails during startup validation, not later in Cart.

no default Market/Currency/SalesChannel is invented.

Cart code is unchanged unless compilation requires a trivial compatibility edit.

Host still owns zero Cart-specific implementation.

frontend unchanged.

Checkout remains paused.

build passes.

working tree contains only task-related changes.

Recovery SoT

On PASS, record this as a focused post-certification platform validation repair.

Preserve:

Cart COMPLETE_REFERENCE_PATTERN

Cart ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Order ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Checkout PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

Set:
nextTask = USER_REVIEW_CART_STORE_COMMERCE_FAILFAST_001

Do not start the next issue automatically.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001
Parent-Task: TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Marketplace-Production-Validation:
SingleStore-Production-Validation:
SalesChannel-Startup-Validation:
Currency-Validation:
Market-Fallback-State:
Disabled-Tenant-State:
Comment-Consistency-State:
Architecture-Locks-State:
Cart-Host-Closure-Preserved:
Focused-Validation:
Full-Build:
Frontend-Production-Changes:
Checkout-State:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not start Shared-DB work.
Do not start another repair.
Do not resume Checkout.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
