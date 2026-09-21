PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-001-R1

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-001

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
NEXT_REFERENCE_MODULE_PHYSICAL_REPAIR

Title:
Inventory + Promotion Physical Folder / Namespace / Anti-Workaround Closure

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Architect verdict

Parent PASS is REOPENED. Do NOT start BATCH-002.

Architect directly inspected current main and found Inventory/Promotion are not yet eligible for COMPLETE_REFERENCE_PATTERN.

Concrete residuals:

Inventory:

many handwritten Application/Domain/Infrastructure files still sit at project root

InventoryDomain.cs is multi-responsibility

InventoryDirectory/module/outbox/gateway files are root-dumped

CheckoutInventoryReservationAdapter.ReleaseAsync contains silent catch:
catch (InvalidOperationException) { }

InventoryDirectory constructor has hidden implementation fallbacks:
clock ?? new SystemUtcClock()
ids ?? new UuidV7IdGenerator()
tracer ?? new ModuleCallTracer()

Promotion:

Application/Domain/Infrastructure are still heavily root-dumped

PromotionDomain.cs and MerchandisingCampaignDomain.cs are broad root files

directories/module/outbox remain root-dumped

PromotionDirectory/MerchandisingCampaignDirectory use optional clock/id fallbacks

MerchandisingCampaignDirectory still has Persian exception prose:
throw new InvalidOperationException("گونهٔ مرچندایزینگ یافت نشد.");

Parent states are provisional:
Inventory-State: REOPENED_PHYSICAL_NAMESPACE_ANTIPATTERN
Promotion-State: REOPENED_PHYSICAL_NAMESPACE_ANTIPATTERN

IMPORTANT:
Tax and Pricing MUST NOT be modified in this task.
User explicitly deferred their physical folder/namespace repair.

Objective

Close only Inventory + Promotion:

physical Visual Studio folder structure

namespace alignment

root dumping

hidden DI fallback construction

catch-and-ignore

localized exception prose

guards proving these cannot regress

No broad business redesign.

Physical ownership rule

Apply ARCH-MODULE-PHYSICAL-001.

For every handwritten production .cs file:

classify responsibility

move into meaningful physical folder inside its owning project

align namespace to module + layer + responsibility

update usages atomically

no TypeForwardedTo, alias, duplicate wrapper, or compatibility shim to hide incomplete ownership

Generated EF migrations may remain under Persistence/Migrations.
.csproj stays at project root.
No empty ceremonial folders.

Inventory target shape

Use meaningful folders such as:

Tooba.Inventory.Domain/
Aggregates/
Entities/
ValueObjects/
Events/
Policies/

Tooba.Inventory.Application/
Ports/
Models/
Checkout/
Orders/
Returns/

Tooba.Inventory.Contracts/
Seller/
Checkout/
Orders/
Availability/
Errors/

Tooba.Inventory.Infrastructure/
Persistence/
Directories/
Adapters/
Events/
Messaging/
DependencyInjection/

Only create folders that contain real files.

Classify/move current root files including:

InventoryContracts.cs

CheckoutInventoryReservationAdapter.cs

InventoryReturnGateway.cs

OrderInventoryLifecycleAdapter.cs

OrderSupplyContracts.cs

InventoryDomain.cs

InventoryDirectory.cs

InventoryModule.cs

InventoryOutboxRegistration.cs

Infrastructure InventoryReturnGateway.cs

Files named *Contracts.cs inside Application are not automatically public Contracts.
Move only true cross-module contracts to Contracts.
Keep Application-owned ports/models in Application under correct folders.

Promotion target shape

Use meaningful folders such as:

Tooba.Promotion.Domain/
Aggregates/
ValueObjects/
Events/
Policies/
Merchandising/

Tooba.Promotion.Application/
Ports/
Models/
Checkout/
Merchandising/
Promotions/

Tooba.Promotion.Contracts/
Checkout/
Merchandising/
Pricing/
Errors/

Tooba.Promotion.Infrastructure/
Persistence/
Directories/
Queries/
Adapters/
Events/
Messaging/
DependencyInjection/

Classify/move current root files including:

CheckoutPromotionAdapter.cs

MerchandisingCampaignContracts.cs

MerchandisingCampaignRuntimeContracts.cs

PromotionContracts.cs

MerchandisingCampaignDomain.cs

PromotionDomain.cs

CampaignCartPriceAuthority.cs

MerchandisingCampaignDirectory.cs

MerchandisingCampaignQuery.cs

PromotionDirectory.cs

PromotionModule.cs

PromotionOutboxRegistration.cs

Namespace rule

Physical path and namespace must agree.

Examples:
Tooba.Inventory.Domain.Aggregates
Tooba.Inventory.Application.Checkout
Tooba.Inventory.Application.Ports
Tooba.Inventory.Contracts.Seller
Tooba.Inventory.Infrastructure.Directories
Tooba.Inventory.Infrastructure.Persistence

Tooba.Promotion.Domain.Merchandising
Tooba.Promotion.Application.Merchandising
Tooba.Promotion.Contracts.Checkout
Tooba.Promotion.Infrastructure.Directories
Tooba.Promotion.Infrastructure.Queries

Do not keep flat namespaces merely to reduce compile work when the physical folder now expresses responsibility.

If a public contract namespace changes, update every in-repo consumer atomically.
No TypeForwardedTo.

Domain decomposition

Do not simply move broad root god-files unchanged.

InventoryDomain.cs:
split by cohesive responsibility where useful:

InventoryLocation

Stock position/item

StockReservation

adjustment/value enums

domain events/policies

PromotionDomain.cs / MerchandisingCampaignDomain.cs:
split by cohesive responsibility where useful:

PromotionDefinition

promotion value enums/policies

MerchandisingCampaign

MerchandisingPromotionType

campaign membership/value types

Do not over-fragment tiny types.

Remove hidden DI fallback construction

Inventory orchestrators/directories must require:

IClock

IIdGenerator

IModuleCallTracer where used

No:

?? new SystemUtcClock()

?? new UuidV7IdGenerator()

?? new ModuleCallTracer()

Promotion directories must require IClock/IIdGenerator.
Tests use explicit fakes/registrations.

Remove silent catch

Current Inventory checkout adapter silently ignores InvalidOperationException during release.

Characterize intended semantics.

Preferred:
make release idempotent at Inventory boundary so already-released is an explicit successful no-op if business semantics allow it.

Unexpected failures must propagate.

Forbidden:

empty catch

catch(Exception)

log-and-ignore

retry/sleep workaround

Add one focused behavior test.

Remove localized exception prose

Replace remaining Persian/English sentence exception text in Inventory/Promotion Domain/Infrastructure.

Expected business failure:
use canonical Result/SemanticError when appropriate.

Internal invariant/unexpected:
stable machine-safe code only; raw message must not reach HTTP.

Result / HTTP

Preserve existing parent Result behavior.
No broad redesign.
No new HTTP surface.
No raw Results.Json.
No local ProblemDetails/status/locale mapping.

Visual Studio acceptance evidence

Mandatory.

Write:

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001-R1/inventory-physical-tree.md

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001-R1/promotion-physical-tree.md

Each file must list every handwritten production .cs relative path + namespace exactly as it appears after repair.

A module cannot be COMPLETE if:

physical path and namespace disagree

identifiable-responsibility files remain dumped in project root

wrong-layer namespaces remain

Namespace audit evidence

Write:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001-R1/namespace-audit.md

Report per project:

handwritten production .cs count

namespace mismatches before/after

root-dump count before/after

TypeForwardedTo hits

wrong-layer namespace hits

Anti-workaround evidence

Write:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001-R1/antiworkaround-audit.md

Report before/after:

new SystemUtcClock

new UuidV7IdGenerator

new ModuleCallTracer

empty/silent catches

localized exception prose

Architecture guards

Add strong general guards for Inventory and Promotion:

handwritten production file not at project root except tiny explicit reviewed allowlist

project/layer matches namespace prefix

responsibility folder matches namespace suffix for moved files

Contracts namespace starts Tooba.<Module>.Contracts

Domain starts Tooba.<Module>.Domain

Application starts Tooba.<Module>.Application

Infrastructure starts Tooba.<Module>.Infrastructure

no TypeForwardedTo

no hidden clock/id/tracer fallback implementation construction

no silent empty catch

no localized exception prose

Prefer general guards, not giant exact file allowlists.

Scope boundaries

DO NOT modify:

Tax

Pricing

Checkout workflow semantics

frontend

Offer changes allowed only if compile-only namespace consumer updates are required.
Do not start BATCH-002.

Validation budget

Required:

Inventory focused architecture/behavior tests

Promotion focused architecture/behavior tests

focused test for idempotent release behavior

build changed dependent projects as needed

one final dotnet build src/backend/Tooba.slnx

Conditional:

Cart/Order tests only if public namespace moves touch them

Host focused tests only if composition namespace updates require them

Offer full tests not needed for compile-only using changes

BuildingBlocks tests only if BuildingBlocks production changes (prefer none)

No broad Host integration suite.
No unrelated suites.
No frontend tests.
No retry/sleep workaround.

Recovery evidence

Also write:

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001-R1/recovery-start.md

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001-R1/recovery-sot.md

Success state

Only on true closure:

Inventory-State: COMPLETE_REFERENCE_PATTERN
Inventory-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE

Promotion-State: COMPLETE_REFERENCE_PATTERN
Promotion-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE

Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE
Offer-State: COMPLETE_REFERENCE_PATTERN

Tax-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Pricing-State: DEFERRED_PHYSICAL_REVIEW_BY_USER

Batch-State: COMPLETE
Module-Recovery-State: NEXT_REFERENCE_BATCH_001_PHYSICAL_COMPLETE
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Frontend-Production-Changes: NONE
Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-002

If any mandatory physical/namespace issue remains:
return INCOMPLETE with exact file/path.
Do not claim COMPLETE.

AntiPattern gate

Must be CLEAN for:

root dumping

namespace masquerading

TypeForwardedTo

hidden DI fallbacks

catch-and-ignore

localized exception prose

Host DbContext leak

direct clock/id bypass

raw StartActivity

frontend change

Checkout resume

fake/ceremonial folder structure

Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Inventory-Physical-Tree
Inventory-Namespace-Audit
Inventory-Antiworkaround-Repairs
Inventory-Validation
Inventory-State
Inventory-Physical-State
Promotion-Physical-Tree
Promotion-Namespace-Audit
Promotion-Antiworkaround-Repairs
Promotion-Validation
Promotion-State
Promotion-Physical-State
Architecture-Guards
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Batch-State
Foundation-State
Offer-State
Tax-State
Pricing-State
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