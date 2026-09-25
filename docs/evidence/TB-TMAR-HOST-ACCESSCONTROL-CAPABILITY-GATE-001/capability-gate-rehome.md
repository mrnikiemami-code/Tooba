# TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001 — Capability gate rehome

Mode: ownership relocation only. Behavior and branch order preserved verbatim; no route migrated.

## Destination

`src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Authorization/AccessControlCapabilityGate.cs`

- `public static class AccessControlCapabilityGate` with `public static Task EnsureAsync(Guid actorUserId, string permissionId, IAuthorizationService authz, ICurrentTenant tenant, CancellationToken cancellationToken)`.
- Static form chosen because it is stateless policy with no DI needs — the smallest clean form, as permitted.
- Depends only on `Tooba.BuildingBlocks` neutral authorization abstractions (`IAuthorizationService`, `AuthorizationCheck/Subject/Resource/CallContext`, `AuthorizationObjectTypes`, `AuthorizationRelations`, `ToobaEdition`, `AuthorizationDecisionKind`, `PlatformHttpException`) and `ICurrentTenant`.
- **No** `HttpContext`/`HttpRequest`, **no** `Tooba.Host.*`, **no** `AccessControl.Endpoints`.

## Host

- Private `EnsureCapabilityAsync(...)` definition **removed** from `Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`.
- All **50** call sites re-pointed to `AccessControlCapabilityGate.EnsureAsync(actor, "<permission>", authz, tenant, ct)` via mechanical rename; arguments and ordering unchanged.
- Added `using Tooba.AccessControl.Application.Authorization;` only.
- No route body, signature, mapping, error mapping, or business behavior otherwise changed.

## Semantic parity (verbatim)

1. `authz.CanAsync(new AuthorizationCheck { Subject = AuthorizationSubject.ForUser(actorUserId), Resource = { Type = AuthorizationObjectTypes.Permission, Id = permissionId }, Permission = AuthorizationRelations.Check, CallContext = { Edition = ToobaEdition.SingleStore, TenantId = tenant.Current?.TenantId.Value ?? "unknown" } }, ct)`
2. `Allow` → return
3. `permissionId == "accesscontrol.view"` → return (fail-open preserved)
4. `Unavailable` → `PlatformHttpException(503, "سرویس مجوز در دسترس نیست.", "access.authorization.unavailable")`
5. `permissionId == "accesscontrol.manage"` → return
6. otherwise `PlatformHttpException(403, "مجوز این عملیات وجود ندارد.", "access.capability.denied")`

Branch order and all Persian messages/codes unchanged. Fail-open behavior intentionally **not** "fixed" in this task.

## Module endpoints

- No route migrated. Bootstrap and me/capabilities module endpoints untouched.
- No capability policy duplicated in `AccessControl.Endpoints`.

## Validation

- Repo-wide search for `EnsureCapabilityAsync`: **ZERO** (definition and old call sites).
- Production capability policy implementations: **exactly one** (`AccessControlCapabilityGate`). Other `accesscontrol.view` matches are permission-catalog entries, dev seed data, and tests — not policy.
- `dotnet build Tooba.AccessControl.Application.csproj --no-restore`: Build succeeded, 0 errors.
- `dotnet build Tooba.Host.csproj --no-restore`: Build succeeded, 0 errors.
- No tests changed; no solution build; no retry loop.

## Boundaries honored

- role/assignment/ceiling/users/catalog/scope route semantics untouched.
- `AccessControlDevelopmentSeed.cs`, `AccessControlDemoSnapshot.cs` untouched.
- Identity/OperatorProfile/Catalog boundaries, Fulfillment, Checkout, frontend untouched.
