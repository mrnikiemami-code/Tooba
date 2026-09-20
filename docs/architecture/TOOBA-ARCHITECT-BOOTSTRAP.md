Tooba Architect Bootstrap

Canonical bootstrap for recovering the Tooba architecture context after chat/session loss.

1. Primary Goal

The highest architectural goal of Tooba is:

Keep Tooba as a strict Modular Monolith today while making future migration to Microservices low-friction, incremental, and without painful rewrites.

Every architecture decision must be evaluated against this goal.

2. Current Architecture Recovery Program

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Last accepted Product task:
TB-P10-T022-R21

Accepted architecture baseline:
TB-TMAR-ARCH-BASELINE

Current next task:
TB-TMAR-HOST-W4 (continue Host direct-write reduction; Appearance PASS)

Product development rule:
Foundation (TB-TMAR-FND-001) is ACCEPTED.
Host Wave 1 Slice 1 accepted after TB-TMAR-HOST-W1 + TB-TMAR-HOST-W1-R1.
Host Wave 2 (TB-TMAR-HOST-W2) PASS.
Boundary verification (TB-TMAR-BOUNDARY-V1) PASS — cross-module leaks confirmed and frozen.
Boundary repair (TB-TMAR-BOUNDARY-V1-R1) PASS — god-file growth frozen; Infra→foreign Application growth frozen.
Contracts Wave 1 (TB-TMAR-CONTRACTS-W1) PASS — Offer/Wallet Contracts; Domain→Offer Domain and Payment→Wallet.Domain removed.
Contracts Wave 2 (TB-TMAR-CONTRACTS-W2) PASS — Wallet payment port; Offer lookup Contracts; ARCH-TX-001.
Contracts Wave 3 (TB-TMAR-CONTRACTS-W3) PASS — Returns→Wallet.Contracts refund port; Order.App→Offer.Contracts SalesChannel.
Contracts Wave 4 (TB-TMAR-CONTRACTS-W4) PASS — Cart→Offer.Contracts; Tax.Contracts calculator; Order.App→Tax.Contracts.
Contracts Wave 5 (TB-TMAR-CONTRACTS-W5) PASS — Inventory→Offer.Contracts; Pricing.Contracts lookup; Order.App→Pricing.Contracts.
Contracts Wave 6 (TB-TMAR-CONTRACTS-W6) PASS — Promotion→Offer.Contracts; Cart→Pricing.Contracts; FE READY.
Frontend Baseline (TB-TMAR-FE-BASELINE) PASS — inventory/ownership/target arch; FE-SIZE/SEO/BOUNDARY locks+guards; root `src/frontend`; no broad refactor.
Frontend F1 (TB-TMAR-FE-F1) PASS — FE-FOLDER freezes; canonical test discovery; admin-languages migrated to features/.
Frontend ADMIN-W1 (TB-TMAR-FE-ADMIN-W1) PASS — admin-promotions migrated; admin-api/screens shrink.
Frontend ADMIN-W2 (TB-TMAR-FE-ADMIN-W2) PASS — admin-reviews migrated; admin-api/screens shrink; Admin-Migration-Pattern PROVEN.
Frontend ADMIN-W3 (TB-TMAR-FE-ADMIN-W3) PASS — admin-sellers migrated; admin-api/screens shrink; Flat CONTINUE_FEATURE_MIGRATION.
Frontend ADMIN-W4 (TB-TMAR-FE-ADMIN-W4) PASS — admin-customers migrated; admin-api/screens shrink; Flat CONTINUE_FEATURE_MIGRATION.
Frontend ADMIN-W5 (TB-TMAR-FE-ADMIN-W5) PASS — admin-receipts migrated; admin-screens 1036→894; Flat exit NOT_READY; Architecture-Priority FE_ADMIN.
Frontend ADMIN-W6 (TB-TMAR-FE-ADMIN-W6) PASS — admin-dashboard migrated; admin-api 1022→1002; exports 38→35; admin-screens 894→810; Flat exit READY_TO_PIVOT; Architecture-Priority HOST.
Host W3 (TB-TMAR-HOST-W3) PASS — StoreAppearanceSettings write → Catalog CQRS Directory; Host-write baseline shrink; CONTINUE_HOST; next HOST-W4.
Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL (Order hub still frozen; do not expand App→App / Infra→App; no NEW cross-context ACID).
Last Product Task = TB-P10-T022-R21.
Architecture Baseline = TB-TMAR-ARCH-BASELINE.
Primary goal = painless future Microservice migration.
After Foundation PASS, product development may continue in parallel with gradual TMAR refactoring, but all new code must follow the new architecture locks.

3. Worker Protocol

Architect:
ChatGPT

Execution Worker:
Cursor only — tooba-worker-01

Channel:
tooba-main

Protocol:
BRIDGE-WAKE-V1

Rules:

Worker executes only when user explicitly sends a task.

Worker claims exact Task-ID.

Worker executes only that task.

Worker sends canonical Result through Bridge.

Worker then STOPS completely.

No polling.

No fetching the next task automatically.

Never write Worker IDLE.

4. Task File Rule

For every Tooba task file:

filename MUST exactly match Task-ID.

Example:
TB-TMAR-FND-001.task.md

Inside the file:
Task-ID: TB-TMAR-FND-001

Before handing a task to the user, verify filename ↔ Task-ID equality.

5. Git / User-Work Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Protected user-work ancestor:
18ca10c9

Never use destructive recovery over user work:

no git reset

no git clean

no unsafe checkout --

no unsafe restore

no unsafe rebase

no blind stash manipulation

no broad git add .

If pre-existing user work conflicts with a task:
return RECOVERY_CONFLICT.

6. Target Module Architecture

Each business module should converge toward:

src/Modules/<Module>/
  Tooba.<Module>.Domain/
  Tooba.<Module>.Application/
  Tooba.<Module>.Contracts/
  Tooba.<Module>.Infrastructure/

Physical folder moves are NOT the first step.
Ownership and dependency boundaries must be corrected first.

7. Bounded-Context Ownership Rule

A Domain type belongs to the bounded context that owns its:

business invariant

lifecycle

business responsibility

It must NOT be placed in a module merely because:

persistence was convenient there

that module already had a DbContext

UI happened to consume it there

a historical implementation started there

Known suspicious names such as:
StoreAppearance*, StoreLandingPage*, StoreMenu*, StoreCheckout*
are EXAMPLES ONLY.

Ownership audit is repository-wide across ALL *.Domain projects.

8. Persistence Boundaries

Required:

one schema per module

one DbContext per module

migrations owned by the module

no global business DbContext

no cross-schema FK

no cross-module SQL JOIN

no foreign-module DbContext in business write paths

9. Cross-Module Contracts

Target:
Tooba.<Module>.Contracts

Cross-module dependencies should use Contracts/Gates/Events.

Forbidden target state:

Module A.Application → Module B.Application

Module A → Module B.Infrastructure

foreign Domain entities crossing module boundaries

Existing Application→Application debt is legacy and should shrink during TMAR.

10. CQRS + MediatR

All NEW application use-cases must converge to:

HTTP Endpoint
  → ISender
  → Command / Query
  → Handler
  → Domain + Repository/Gates/Contracts

Approved MediatR version:
12.5.0

Validation:
FluentValidation through MediatR pipeline.

Migration strategy:

first wrap existing Directory/Application Services behind handlers

then gradually move orchestration into handlers

narrow/remove Directories only when safe

No Big Bang rewrite.

11. Host Rule

Host must converge to:

HTTP transport

authentication/session boundary

middleware

DI/composition root

endpoint mapping

serialization

minimal view composition

Host must NOT gain new:

business writes

SaveChanges

transactions

pricing decisions

inventory decisions

seller/buy-box decisions

campaign eligibility decisions

domain ownership

Legacy Host debt is migrated in waves.

12. Read-Side Microservice Readiness

Cross-module reads should converge to contracts/gateways:

Query Handler / Composer
  → ICatalogReadGateway
  → IPricingReadGateway
  → IInventoryReadGateway
  → ...

Today:
in-process implementations are allowed.

Future:
same contracts may be backed by HTTP/gRPC/read services.

Host/composers must compose authoritative projections, not calculate business truth.

13. Clock and ID

Time:
use canonical IClock at Application/Infrastructure orchestration boundaries.

Pure Domain methods may receive now explicitly.

ID:
use canonical IIdGenerator / UUID abstraction.
UUIDv7 implementation may live behind the abstraction.

Do not inject IClock into every Domain entity.
Do not add interfaces around pure deterministic helpers merely for style.

14. Errors + Localization

Domain/Application errors must be semantic and stable.

Target:
catalog.category.invalid_slug

NOT:
hardcoded Persian/English user-facing Domain messages.

HTTP boundary maps semantic errors to localized ProblemDetails.detail.

Locale design must support unlimited locales, not only fa/en.

Locale normalization/fallback must be centralized, not duplicated across features.

15. Cache

Canonical abstractions:

ICache

ICacheKeyBuilder

ICacheInvalidator

No new direct IMemoryCache bypass.

Redis is a future provider, not a module dependency.

Redis work must also cover:

distributed invalidation

stampede protection

jittered TTL

tenant/store/locale-aware keys

metrics

16. Architecture Enforcement

Architecture rules must be enforced through tests/CI, not only documentation.

Important guards include:

Domain ↛ Infrastructure/Host

Application ↛ foreign Infrastructure

Infra A ↛ Infra B

no new App→App edges

no new Host business writes

no new direct system clock in protected layers

no new direct UUID implementation calls in protected layers

no new localized Domain exception text

no new direct IMemoryCache bypass

no cross-schema FK/JOIN

Legacy debt should be explicitly baselined and the baseline should only shrink.

17. Migration Strategy

Never Big Bang.

Order:

Architecture Foundation

Host dangerous-write removal

CQRS adoption

Contracts extraction

Domain ownership corrections

Read-gateway migration

Error/locale standardization

Cache adoption cleanup

Physical folder reorganization

Microservice-readiness verification

New code follows target architecture immediately after Foundation PASS.
Old code migrates when touched, high-risk, or extraction-critical.

18. Recovery Trigger

If a chat/session is lost, user can say:

برگردیم به TMAR

or:

Bootstrap Tooba from TOOBA-ARCHITECT-BOOTSTRAP.md

Then restore:

program = TMAR

last product task = TB-P10-T022-R21

baseline = TB-TMAR-ARCH-BASELINE

foundation = TB-TMAR-FND-001 ACCEPTED

host-wave-1-slice-1 = TB-TMAR-HOST-W1 + TB-TMAR-HOST-W1-R1 PASS (handlers in Application)

host-wave-2 = TB-TMAR-HOST-W2 PASS (Store Menu CQRS)

boundary-v1 = TB-TMAR-BOUNDARY-V1 PASS (claims verified; foreign Domain edges frozen)

boundary-v1-r1 = TB-TMAR-BOUNDARY-V1-R1 PASS (source-size + Infra→foreign Application frozen)

contracts-w1 = TB-TMAR-CONTRACTS-W1 PASS (Offer.Contracts + Wallet.Contracts; Domain/Infra foreign Domain baselines empty)

contracts-w2 = TB-TMAR-CONTRACTS-W2 PASS (Wallet payment port + Offer lookup; ARCH-TX-001)

contracts-w3 = TB-TMAR-CONTRACTS-W3 PASS (Returns Wallet refund port + Order→Offer.Contracts)

contracts-w4 = TB-TMAR-CONTRACTS-W4 PASS (Cart→Offer.Contracts + Tax.Contracts calculator)

contracts-w5 = TB-TMAR-CONTRACTS-W5 PASS (Inventory→Offer.Contracts + Pricing.Contracts lookup)

contracts-w6 = TB-TMAR-CONTRACTS-W6 PASS (Promotion→Offer.Contracts + Cart→Pricing.Contracts; FE READY)
fe-baseline = TB-TMAR-FE-BASELINE PASS (FE architecture baseline + guards; locks FE-ARCH/SIZE/SEO/BOUNDARY)
fe-f1 = TB-TMAR-FE-F1 PASS (flat freezes + discovery + admin-languages feature slice)
fe-admin-w1 = TB-TMAR-FE-ADMIN-W1 PASS (admin-promotions feature; admin-api shrink)
fe-admin-w2 = TB-TMAR-FE-ADMIN-W2 PASS (admin-reviews feature; admin-api shrink; pattern PROVEN)
fe-admin-w3 = TB-TMAR-FE-ADMIN-W3 PASS (admin-sellers feature; admin-api shrink; Flat CONTINUE)
fe-admin-w4 = TB-TMAR-FE-ADMIN-W4 PASS (admin-customers feature; admin-api shrink; Flat CONTINUE)
fe-admin-w5 = TB-TMAR-FE-ADMIN-W5 PASS (admin-receipts feature; screens→894; Flat exit NOT_READY; Priority FE_ADMIN)
fe-admin-w6 = TB-TMAR-FE-ADMIN-W6 PASS (admin-dashboard feature; screens→810; Flat exit READY_TO_PIVOT; Priority HOST)
host-w3 = TB-TMAR-HOST-W3 PASS (StoreAppearanceSettings CQRS; Host-write baseline shrink; CONTINUE_HOST)
next task = TB-TMAR-HOST-W4 unless Recovery SoT says otherwise

primary goal = painless future Microservice migration

Always prefer current repository Recovery SoT over stale chat memory.

19. Canonical Recovery Files

The repository should maintain:

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-DOMAIN-OWNERSHIP.yaml (after TMAR ownership task)

docs/architecture/TMAR-architecture-locks.md

per-task docs/evidence/<Task-ID>/recovery-sot.md

These files are the durable source of truth; chat memory is secondary.