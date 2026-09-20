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
