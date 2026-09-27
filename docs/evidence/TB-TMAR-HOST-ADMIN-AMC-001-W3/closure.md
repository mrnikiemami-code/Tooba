# Closure — W3

## PASS checklist

- Host `UnitOfMeasureEndpoints.cs` removed; Admin 58 → 57.
- Five routes Catalog-owned exactly once under `/v1/admin/catalog/units`.
- All ops CQRS/ISender + ApiResponseFactory + ICatalogAdminAuthorizer.
- Host DbContext / Localization.Application / language gate DI = ZERO for UoM.
- Catalog→Host ZERO; Catalog→Localization.Application ZERO; Endpoints→Infrastructure ZERO.
- Localization cross-module = Contracts-only (`ILanguageLookup`).
- No PlatformHttpException / InvalidOperationException message-as-code in Catalog UoM surface.
- Capability-first Units foldering; legacy *Contracts/*Handlers eliminated.
- Validator matrix exhaustive; error catalog/resx complete for unit.* codes.
- StoreAppearance still deferred; W1/W2/W2-R1 Quantity preserved; W4 not started.
- Schema/frontend unchanged.

## Residual Admin blockers

- StoreAppearance Host.Storefront projector coupling.
- Remaining Catalog Admin HTTP still Host-owned (attributes, categories, menus, landing, merch, workspace, …).
