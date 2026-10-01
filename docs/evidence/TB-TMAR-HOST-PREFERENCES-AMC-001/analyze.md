# Analyze — Host/Preferences AMC-001

## Target

`src/backend/Host/Tooba.Host/Preferences/` (2 files: `UserPreferenceEndpoints.cs`, `UiPreferenceEndpoints.cs`)

## True ownership

| Responsibility | Owner |
|---|---|
| Locale + UI preference aggregates + schema | UserPreference.Domain / Infrastructure |
| Use cases | UserPreference.Application CQRS (`LocalePreferences`, `UiPreferences`) |
| HTTP routes | UserPreference.Endpoints (Customer + Admin) |
| Guest actor seam | Order.Contracts `StorefrontGuestActor` |
| Admin auth | `IUserPreferenceAdminAuthorizer` → Host `HostUserPreferenceAdminAuthorizer` → `IAdminPanelAccess` |
| Host | composition + thin admin authorizer only |

## Coupling / blockers

1. Host endpoints called Application directories directly (no MediatR).
2. Admin auth used `AdminPanelAccess.RequireAuthorizedAsync` inline.
3. Failures used `InvalidOperationException` / `ex.Message` JSON, not `SemanticException` + `ApiResponseFactory`.
4. Module lacked Endpoints / Contracts.Errors / CQRS surface.

## Final disposition

`READY_TO_MIGRATE` → evacuate Host folder to UserPreference Endpoints/CQRS (`HOST_ZERO`).
