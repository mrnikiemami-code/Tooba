# TB-TMAR-HOST-ADMIN-CANON-004 — Validation

## Focused commands

1. `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj`
   → **Build succeeded. 0 Errors** (10 pre-existing XML/using warnings, none from this change).
2. `dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj`
   → **Build succeeded. 0 Errors.**
3. `dotnet test Tooba.Host.Tests --filter "HostAdminCanon004GuardTests|HostAdminCanon003GuardTests|HostAdminCanon002GuardTests"`
   → **Passed! Failed: 0, Passed: 38, Total: 38**
4. `dotnet test Tooba.Order.Tests --filter "OrderEndpointPresentationTests|OrderEndpointOrganizationGuardTests"`
   → **Build FAILED (exit 1)** on a *pre-existing, unrelated* baseline defect:
   `Architecture/OrderSellerPanelArchitectureGuardTests.cs(13,13)` and `(14,13)` —
   `CS0234: The type or namespace name 'AccessControl' does not exist in the namespace 'Tooba'`.
   No `.csproj` was modified by this task, so the missing `Tooba.AccessControl` project reference in
   `Tooba.Order.Tests` predates this change. Different file, different concern, not repaired.

## Behavior coverage (`HostAdminCanon004GuardTests`)

| Case | Assertion | Result |
| --- | --- | --- |
| Allow | actor returned equals the panel actor | PASS |
| Deny | `PlatformHttpException` 403 + `order.operation.denied` | PASS |
| Unavailable | `PlatformHttpException` 503 + `order.authorization.unavailable` | PASS |
| `RequireAdminAsync` | delegates to panel gate only; capability engine untouched (unavailable engine still returns actor) | PASS |
| Panel-gate failure | original `PlatformHttpException` (401 `admin.actor.missing`) propagates unchanged | PASS |
| Blank `permissionId` | `ArgumentException` before any panel/authorization call | PASS |

Structural guard assertions:

- constructor is `(IAdminPanelAccess adminAccess, IAuthorizationService authz, ICurrentTenant tenant)`;
- delegated through `adminAccess.RequireAuthorizedAsync(context.Request, …)`;
- ZERO `AdminPanelAccess.RequireAuthorizedAsync`, ZERO `Tooba.AccessControl.Application`,
  ZERO `Tooba.AccessControl.Domain`, ZERO `IAccessControlDirectory`;
- ZERO `RequestServices` / `GetRequiredService`; no `ex.Message`;
- explicit `AuthorizationDecisionKind.Unavailable` branch that throws (no `fail-open`);
- distinct 403 and 503 Order codes (`OperationDenied != AuthorizationUnavailable`);
- Order catalog contributor wires `OrderErrorCodes.AuthorizationUnavailable`, and both
  `OrderErrors.resx` / `OrderErrors.fa.resx` carry `order.authorization.unavailable`;
- `Program.cs` still registers `AddScoped<IOrderAdminAuthorizer, HostOrderAdminAuthorizer>()`;
- `HostOrderAdminEffectiveAccessReader.cs` remains out of scope and still AccessControl-backed;
- CANON-003 surface preserved (Support/Wallet still `IAdminPanelAccess` + `Unavailable` branch);
- `Host/Admin` file count = 15.

`OrderEndpointPresentationTests` was updated so `AllCodes` includes the new admin-panel code, keeping
the "every Order code has an explicit descriptor + en/fa resource" invariant intact. That assembly
cannot be executed until the pre-existing `Tooba.AccessControl` reference defect above is repaired
(out of scope for CANON-004).

## Baseline observations (pre-existing, not caused by this task)

1. **`Tooba.Order.Tests` does not build on the untouched baseline.** Its `.csproj` has no
   `Tooba.AccessControl.*` project reference, yet
   `Architecture/OrderSellerPanelArchitectureGuardTests.cs` imports `Tooba.AccessControl.*`
   (CS0234). This task modified no `.csproj` and no AccessControl file, so the break predates
   CANON-004. Not repaired (different module, different concern).
2. **`ErrorCatalogUniqueCodeGuardTests` fails on the untouched baseline.** The composed catalog
   already contains duplicate `reservation.policy.initial.invalid` / `reservation.policy.retry.invalid`
   / `reservation.policy.max.invalid` descriptors owned by both
   `CatalogErrorCatalogContributor` and `OrderErrorCatalogContributor` — exactly the failure
   documented by CANON-003. Re-confirmed here: `Failed: 2, Passed: 14`, and the failure list contains
   only the `reservation.policy.*` codes. The new `order.authorization.unavailable` code adds **no**
   new duplicate.

## Repair iterations

`Repair-Iterations = 0` (no failing in-scope command required a repair).

## Validation command runs

4 (`build Host`, `build Host.Tests`, focused CANON-004/003/002 guard filter, `Order.Tests`
presentation filter). Plus one reproduction re-run of `ErrorCatalogUniqueCodeGuardTests` to confirm
the `reservation.policy.*` duplicates are baseline. Within `MAX_VALIDATION_COMMAND_RUNS = 6`.
