# TB-TMAR-HOST-ADMIN-CANON-003 — Validation

## Focused commands

1. `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj`
   → **Build succeeded. 0 Errors.**
2. `dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj`
   → **Build succeeded. 0 Errors.**
3. `dotnet test Tooba.Host.Tests --filter "HostAdminCanon003GuardTests|HostAdminCanon002GuardTests"`
   → **Passed! Failed: 0, Passed: 24, Total: 24**
4. `dotnet test Tooba.Support.Tests --filter "SupportArchitectureGuardTests"`
   → **Passed! Failed: 0, Passed: 3, Total: 3**
5. `dotnet test Tooba.Wallet.Tests --filter "WalletArchitectureGuardTests|WalletCqrsAndHttpContractTests"`
   → **Passed! Failed: 0, Passed: 12, Total: 12**

## Behavior coverage (HostAdminCanon003GuardTests)

For BOTH Support and Wallet:

| Case | Assertion | Result |
| --- | --- | --- |
| Allow | actor returned equals requested admin actor | PASS |
| Deny | `PlatformHttpException` 403 + `*.AdminAuthorizationDenied` | PASS |
| Unavailable | `PlatformHttpException` 503 + `*.AuthorizationUnavailable` | PASS |
| Panel-gate failure | original `PlatformHttpException` propagates unchanged (401/403 + code) | PASS |

Structural guard assertions, both files:

- `IAdminPanelAccess adminAccess` constructor dependency present, delegated via
  `adminAccess.RequireAuthorizedAsync(`.
- `RequestServices` / `GetRequiredService` / direct `AdminPanelAccess.RequireAuthorizedAsync` = ZERO.
- No `fail-open` literal; explicit `Unavailable` branch that throws.
- `Host/Admin` file count = 15.
- CANON-002 surface preserved (settlement adapter still `IAdminPanelAccess`-only, no
  `MarketplacePlatformTenantId`).

## Repair iterations

- Iteration 1: `Module_error_catalogs_own_the_unavailability_descriptors` used a reflective
  probe that resolved constant *names* rather than values; assertion failed. Simplified the
  guard to assert the contributor source wires the new constant. **Repair iterations = 1**
  (within `MAX_REPAIR_ITERATIONS = 1`).

A second, non-source edit was needed: `WalletCqrsAndHttpContractTests` asserted the old
"Unavailable fail-open" doc string (`Assert.Contains("fail closed", …)` vs the actual
"fails closed (503)"). That is an in-scope test adjustment required by the in-scope doc update.

## Baseline observation (pre-existing, not caused by this task)

`ErrorCatalogUniqueCodeGuardTests` fails on the untouched baseline as well: the composed
catalog already contains duplicate `reservation.policy.initial.invalid`,
`reservation.policy.max.invalid`, `reservation.policy.retry.invalid` descriptors owned by both
`CatalogErrorCatalogContributor` and `OrderErrorCatalogContributor`. Confirmed by stashing this
task's changes and re-running the same filter (still 2 failed / 0 passed). The new
`support.authorization.unavailable` / `wallet.authorization.unavailable` codes add **no** new
duplicates — the failure list contains only the `reservation.policy.*` codes.
Different module, different concern, out of scope for CANON-003; not repaired.

## Validation command runs

6 (build Host, build Host.Tests, guard filter, Support test, Wallet test, and the baseline
confirmation/stash re-run).
