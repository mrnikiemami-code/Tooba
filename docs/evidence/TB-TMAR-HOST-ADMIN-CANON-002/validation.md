# TB-TMAR-HOST-ADMIN-CANON-002 — Validation

## Commands run (focused only)

1. `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj`
   → **Build succeeded. 0 Errors.**
2. `dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj`
   → **Build succeeded. 0 Errors.**
3. `dotnet test --filter "HostAdminCanon002GuardTests|HostAdminCanon001GuardTests|HostAdminAmcW33MerchandisingGuardTests"`
   → **Passed! Failed: 0, Passed: 22, Skipped: 0, Total: 22**

## Repair iterations

- Iteration 1: `HostAdminCanon002GuardTests.Simple_admin_authorizers_depend_on_IAdminPanelAccess`
  failed because the assertion literal `adminAccess.RequireAuthorizedAsync(context.Request`
  did not match the Settlement/Promotion parameter names (`httpContext.Request` / `context.Request`).
  Guard assertion normalized to `adminAccess.RequireAuthorizedAsync(` + `.Request,`.
- Repair iterations used: **1** (within `MAX_REPAIR_ITERATIONS = 1`).

## Baseline observation (not caused by this task)

`HostDevelopmentAmcGuardTests.Development_folder_matches_exact_retained_allowlist` fails on
the untouched baseline as well: the `Host/Tooba.Host/Development` folder currently contains
template/seed hosts (`CatalogAttributeSchemaDevelopmentSeedHost.cs`,
`FashionTemplateCatalogSeedHost.cs`, `IndustryBatchATemplateCatalogSeedHost.cs`,
`IndustryBatchBTemplateCatalogSeedHost.cs`, …) beyond the guard's 3-file allowlist.
Confirmed by stashing this task's changes and re-running: still failing on baseline.
Out of scope for CANON-002 (`Development/` is not an in-scope file) and therefore not repaired.

## Service locator scan

`RequestServices`, `GetRequiredService`, `GetService(` appear **zero** times in all four
in-scope simple adapters (asserted by `Simple_admin_authorizers_contain_no_service_locator`).

## Boundary scan

`ICurrentTenant`, `IAuthorizationGuard`, `ControlPlaneRegistry`, `CurrentAuthenticatedSession`,
`AuthorizationCheck`, `ToobaEdition` appear **zero** times in all four in-scope simple adapters
(asserted by `Simple_admin_authorizers_own_no_tenant_or_guard_decision_logic`).
