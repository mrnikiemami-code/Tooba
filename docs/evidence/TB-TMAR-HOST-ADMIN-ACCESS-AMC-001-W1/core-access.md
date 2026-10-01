# core-access — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W1

## Scope

`AdminPanelAccess.cs` + `HostAdminPanelAccess.cs` only.

## Changes

- Expected failures throw `SemanticException(SemanticError(FoundationErrorCodes.*))` — no `PlatformHttpException` titles.
- Auth branch semantics preserved: session actor > Dev header (Development-only) > `admin.actor.missing`.
- Single-Store: tenant missing / Allow / Unavailable / Deny unchanged.
- Marketplace Dev: synthetic `marketplace-platform` + edition gate unchanged.
- Files not merged; AdminPanelAccess remains internal static helper; HostAdminPanelAccess remains sole `IAdminPanelAccess` DI impl.

## Hardcoded runtime titles

AdminPanelAccess: ZERO  
HostAdminPanelAccess: ZERO
