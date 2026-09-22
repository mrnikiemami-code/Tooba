# TMAR Architecture Locks (canonical)

Status: CANONICAL after TB-TMAR-FND-001

These locks govern NEW work during TMAR recovery. Existing LOCK-SF-* storefront locks remain intact.

## ARCH-OWN-001
Domain ownership is determined by bounded-context invariant/lifecycle, never persistence convenience.

## ARCH-CONTRACT-001
Cross-module business boundaries move through `Tooba.<Module>.Contracts`; foreign Application/Infrastructure is not the intended public boundary.

## ARCH-DOMAIN-001
Domain errors are semantic/error-code based; localized FA/EN user-facing text is forbidden in Domain.

## ARCH-TIME-001
Application/Infrastructure orchestration uses `IClock`; pure Domain methods may receive explicit `now`.

## ARCH-ID-001
ID generation uses an approved abstraction at orchestration boundaries; direct implementation calls are forbidden in protected Domain/Application paths.

## ARCH-HOST-001
Host is transport/composition root only; no NEW business write, transaction, pricing/inventory/seller/campaign decision, or Domain ownership.

## HOST-FOLDER-001
New Host production `.cs` source must live under an approved responsibility folder unless explicitly exempted. Root allowlist is enforced by `HostFolderStructureTests` (Program.cs + reviewed deferred job/policy leftovers). Do not dump new arbitrary platform files at `Tooba.Host/` root.

## HOST-HYGIENE-001
Host runtime log artifacts (`*.log` / `*.err.log` under `Tooba.Host`) must not be tracked in source control. `.gitignore` covers Host log paths; local diagnostics remain untracked.


## ARCH-FE-FREEZE-001
Until explicit Architect/User release (`TMAR-Execution-Mode` must change from `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`):
no production modification under `src/frontend/**`; no frontend refactor/package/dependency/folder migration; no frontend Orders or god-file work.
Backend tasks may READ frontend only for compatibility evidence.
Machine guard: `TmarDurableGuardTests` + `docs/architecture/tmar-execution-mode.json`. Accidental bypass is forbidden; release requires deliberate architecture update of that mode file and this lock.

## ARCH-FOLDER-OWNERSHIP-001
No new production source may be placed in a project/module root when it belongs to an identifiable responsibility/capability.
Small explicit root allowlists only; existing flat debt is shrink-only; no Common/Misc/Helpers dumping grounds when ownership is knowable; baselines must not widen to absorb new root dumping.
Host enforcement: `HOST-FOLDER-001` / `HostFolderStructureTests`.

## ARCH-RECOVERY-001
TMAR is incremental recovery, not rewrite.
No Big Bang rewrite; no broad replacement of working subsystems; characterization/evidence before risky extraction; behavior preserved unless an explicit product-change task says otherwise; cosmetic relocation must not hide unresolved ownership.

## ARCH-USERWORK-001
Protect user-authored/local work.
Destructive git operations prohibited; conflict => `RECOVERY_CONFLICT`; protected commit `18ca10c9` remains ancestor; every Result reports user work preserved.

## ARCH-BASELINE-001
Architecture baselines are shrink-only.
No widening to pass tests; no wildcard suppression; removed debt must shrink baseline in the same task; a new violation is failure, not baseline addition.
Machine guard: `TmarDurableGuardTests` baseline integrity checks.

## ARCH-NOWORKAROUND-001
No temporary/dirty workaround without architectural justification.
Explicitly prohibit: polling loops; magic sleeps/timeouts/intervals; silent catch-and-ignore; suppressing errors; hardcoded business shortcuts; first-item/first-seller shortcuts; test-only branches; duplicated fallback logic instead of fixing ownership/boundary.

## ARCH-DATA-001
Safe incremental data evolution.
No destructive migration without explicit dedicated approval; schema/migration owned by module; no new cross-module DB FK; no shared mega-DbContext expansion; additive migration preferred; rollback/compatibility documented.


## ARCH-READ-001
NEW cross-module read composition uses declared read contracts/gateways; direct foreign DbContext composition is legacy-only and must not expand.

## ARCH-DB-001
Each module owns schema/DbContext/migrations; no cross-schema FK/JOIN or foreign-module business-write DbContext access.

## ARCH-CQRS-001
All NEW application use-cases use CQRS + MediatR Handler.

Additionally: any HTTP-owning module claiming `COMPLETE_REFERENCE_PATTERN` MUST place its HTTP business use-cases behind real MediatR 12.5.0 Commands/Queries/Handlers. Module Endpoints MUST NOT invoke Directory/Application services directly for business use-cases. Fake/ceremonial handlers are forbidden.

## ARCH-CQRS-002
Approved MediatR version is EXACTLY **12.5.0**.

## ARCH-COMPLETE-001
A module MUST NOT be marked `COMPLETE_REFERENCE_PATTERN` unless its declared HTTP applicability, endpoint ownership, CQRS/MediatR boundary, Result/error semantics, physical structure, cross-module contracts, Host authority, behavior preservation, and recovery state are all verified. Green build/tests alone are insufficient.

For an HTTP-owning module, `COMPLETE_REFERENCE_PATTERN` requires ALL of:
- real physical `Tooba.<Module>.Endpoints` project
- module owns its business HTTP routes
- Host only maps module endpoint composition (`Map…`)
- Endpoint invokes Application use-cases through `ISender`
- real MediatR 12.5.0 Command/Query + Handler in Application
- expected business outcomes use Result/SemanticError + centralized HTTP presentation
- foreign module dependencies are Contracts/Gates/Events only
- no foreign DbContext/cross-module SQL ownership
- physical folder + namespace ownership verified
- architecture guards enforce these properties
- behavior preservation evidence exists
- recovery SoT updated in the same accepted task cycle

For true internal-only modules: Endpoints may be `NOT_APPLICABLE` only when explicitly proven and recorded. CQRS applies to actual application use-case boundaries. Internal-only status cannot be assumed merely because no Endpoints project exists.

## ARCH-VAL-001
NEW request validation uses FluentValidation through MediatR pipeline.

## ARCH-LOCALE-001
Locale normalization/fallback is centralized and unlimited-locale safe.

## ARCH-CACHE-001
NEW cache consumption uses `ICache`; direct `IMemoryCache` use must not expand.

## ARCH-FOLDER-001
Physical folder moves happen only after ownership/dependency repair.

## ARCH-SIZE-001
No new hand-written source file may exceed the approved oversized-file threshold (800 physical LOC) without explicit architecture approval. Guard: `tmar-source-size-baseline.json` + `TmarSourceSizeAndInfraAppTests`.

## ARCH-SIZE-002
Existing oversized legacy / critical god files may only stay equal or shrink; growth above their recorded baseline LOC is forbidden. Baseline entries must be removed or reduced when files are split or deleted; baselines never auto-raise.

## ARCH-MODULE-FILE-001
No new multi-responsibility module god-files. New production files must have one cohesive responsibility and obey source-size guards. Prefer split before 800 LOC. Proven by Offer reference module (`TB-TMAR-OFFER-REFERENCE-W1`).

## ARCH-MODULE-PHYSICAL-001
A reference-complete module must place production source files under the approved module/project responsibility folders on disk. Namespace alignment and documentation alone are insufficient for `COMPLETE_REFERENCE_PATTERN`. Empty ceremonial folders are forbidden. Guard: `OfferPhysicalStructureGuardTests` (Offer; extend per module). Proven by `TB-TMAR-OFFER-REFERENCE-W1-R1` after prior COMPLETE was reopened on visual evidence.

## HOST-MODULE-ENDPOINT-001
For HTTP-owning COMPLETE modules the required flow is:
`Module.Endpoints → ISender → Module.Application` (MediatR Commands/Queries/Handlers).

Host may:
- register adapters/services
- map `Map<Module>…()` composition methods
- own global auth/session/tenant/middleware/platform endpoints

Host must NOT:
- implement module-owned business routes
- call module business directories directly from HTTP endpoints
- own module-specific business response mapping/composers/query engines

Development-only bootstrap is separately allowlisted and does not count as endpoint/business ownership.
Guard: `HostModuleEndpointOwnershipTests` (multi-module durable) + module architecture tests.

## ARCH-REFACTOR-001
Critical giant-file decomposition requires characterization tests around the touched slice before structural splitting. Preserve public behavior; split incrementally by capability/use-case; keep architecture guards green; do not rely only on AI-generated diff inspection.

## ARCH-TX-001
No NEW business workflow may rely on a single ACID transaction spanning multiple bounded contexts. Existing confirmed cross-context/shared-database transactions are explicit migration debt (baseline `tmar-cross-context-transaction-files.json`). Future extraction requires an explicit distributed-consistency design (see TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN). Do not add a global MediatR TransactionBehavior.

## ARCH-CHECKOUT-001
No new checkout step may rely on cross-bounded-context shared ACID. Existing `CheckoutDirectory` TransactionScope is migration debt only.

## ARCH-CHECKOUT-002
All future checkout participant operations must be idempotency-designable and retry-safe at the contract boundary.

## ARCH-CHECKOUT-003
Irreversible external side effects (payment capture, wallet debit, fulfillment dispatch) must occur only after required preconditions and documented point-of-no-return rules are explicitly satisfied.

## ARCH-CHECKOUT-004
Cross-context checkout integration must use owned Contracts/Commands/Events; no new foreign Application/Domain coupling for checkout participants.

## ARCH-CHECKOUT-005
Checkout workflow state must be recoverable after process crash/restart; no in-memory-only authoritative workflow state.

## FE-ARCH-001
New App Router route files (`page`/`layout`/route composition) should remain thin composition/transport layers; business feature UI must not accumulate directly in route files. Canonical frontend root: `src/frontend`.

## FE-SIZE-001
No new hand-written frontend source file may exceed 800 physical LOC without explicit architecture approval. Guard: `docs/evidence/TB-TMAR-FE-BASELINE/frontend-source-size-baseline.json` + `src/frontend/lib/architecture/frontend-source-size.guard.test.ts` (also covered by repo-wide `tmar-source-size-baseline.json`).

## FE-SIZE-002
Existing oversized frontend files are shrink-only against their recorded baseline LOC. Baselines never auto-raise; entries reduce/remove when files shrink or split.

## FE-SEO-001
New storefront implementation must not make primary indexable content client-only without explicit architectural justification. Route metadata and canonical product/category/article/store content must remain server-capable. Guard: `seo-rendering.guard.test.ts` + `npm run test:critical-storefront`.

## FE-BOUNDARY-001
Shared UI (`design-system`) and technical libraries (`lib`) must not gain new dependencies on business feature modules under `app/admin` or `app/storefront`. Existing reverse edges are baselined shrink-only (`frontend-import-boundary-baseline.json`).

## FE-BOUNDARY-002
New cross-feature imports must use an approved public feature boundary rather than deep internal imports. Do not introduce a giant barrel-file architecture; deepen enforcement during FE-F2+ feature extraction.

## FE-FOLDER-001
No NEW business-feature implementation file may be added directly under the baselined flat admin accumulation directory (`src/frontend/app/admin/*` files). App Router convention files (`page`/`layout`/`loading`/`error`/`not-found`/`route`/…) remain allowed when they belong in App Router. Baseline: `docs/evidence/TB-TMAR-FE-F1/frontend-flat-folder-baseline.json`. Guard: `frontend-flat-folder.guard.test.ts`.

## FE-FOLDER-002
No NEW capability-specific API client/service export may be added to the generic `admin-api.ts` dumping-ground path. Existing exports are shrink-only against the FE-F1 baseline. Shared technical primitives belong under `lib/admin/` (or equivalent), not new capability methods on `admin-api.ts`.
