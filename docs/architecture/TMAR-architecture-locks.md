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

## ARCH-READ-001
NEW cross-module read composition uses declared read contracts/gateways; direct foreign DbContext composition is legacy-only and must not expand.

## ARCH-DB-001
Each module owns schema/DbContext/migrations; no cross-schema FK/JOIN or foreign-module business-write DbContext access.

## ARCH-CQRS-001
All NEW application use-cases use CQRS + MediatR Handler.

## ARCH-CQRS-002
Approved MediatR version is EXACTLY **12.5.0**.

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

## ARCH-REFACTOR-001
Critical giant-file decomposition requires characterization tests around the touched slice before structural splitting. Preserve public behavior; split incrementally by capability/use-case; keep architecture guards green; do not rely only on AI-generated diff inspection.

## ARCH-TX-001
No NEW business workflow may rely on a single ACID transaction spanning multiple bounded contexts. Existing confirmed cross-context/shared-database transactions are explicit migration debt (baseline `tmar-cross-context-transaction-files.json`). Future extraction requires an explicit distributed-consistency design (see TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN). Do not add a global MediatR TransactionBehavior.

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
