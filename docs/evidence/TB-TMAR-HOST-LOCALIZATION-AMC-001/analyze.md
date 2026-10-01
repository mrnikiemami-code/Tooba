# Analyze — Host/Localization AMC-001

## Target

`src/backend/Host/Tooba.Host/Localization/` (2 files):

- `LocaleAdminEndpoints.cs`
- `ContentLanguageReferenceGuard.cs`

## True ownership

| Responsibility | Owner |
|---|---|
| Admin language registry HTTP (`/v1/admin/languages`) | Localization.Endpoints |
| Language directory / bootstrap / EF | Localization.Application + Localization.Infrastructure |
| Language error codes | Localization.Contracts.Errors |
| Language reference guard contract | Localization.Contracts.ILanguageReferenceGuard |
| Content article locale reference check | Content.Infrastructure (owns ContentDbContext) |
| Host/Localization folder | ZERO after evacuation |

## Coupling / blockers

1. Host owned Localization HTTP with direct Application directory calls.
2. `errorCode = ex.Message` message-as-code residue on LocaleAdminEndpoints.
3. `ContentLanguageReferenceGuard` held ContentDbContext on Host (illegal Host→module Persistence).
4. `ILanguageReferenceGuard` lived in Localization.Application — blocked Contracts-only Content adapter.

## Final disposition

`READY_TO_MIGRATE` → Localization.Endpoints + Content.Infrastructure guard + Contracts seam; `HOST_ZERO`.
