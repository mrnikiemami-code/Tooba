# TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1 — Canonicalization & parity verification

Mode: verification + canonicalization only. No new migration family, no behavior change.

## Unsolicited commit

`f059c82544c9710c6beb8301f51be2d41930c767` ("refactor(tmar): TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001
rehome capability gate policy to AccessControl.Application") was pushed to `main` **without** an
Architect-issued task, and its canonical task artifact was never created.

Decision: **adopt, do not revert**. R1 verified semantics first; the commit is now treated as an
unsolicited intermediate commit adopted only after this verification. The missing unissued parent
task file is intentionally **not** recreated (per task instruction).

## Verification results

| Check | Result |
| --- | --- |
| `AccessControlCapabilityGate.EnsureAsync` is the only production implementation | PASS — single implementation at `Tooba.AccessControl.Application/Authorization/AccessControlCapabilityGate.cs` |
| All former Host call sites re-pointed | PASS — 50 `AccessControlCapabilityGate.EnsureAsync` call sites in `Host/AccessControl/AccessControlEndpoints.cs` |
| Host `EnsureCapabilityAsync` definition/references | PASS — **ZERO** repo-wide |
| Branch order preserved | PASS (see below) |
| `AuthorizationCheck` shape unchanged | PASS (see below) |
| No HTTP/Host dependency in Application | PASS — no `using Microsoft.AspNetCore.*` and no `Tooba.Host.*` under `AccessControl.Application` |
| Accepted routes unchanged | PASS — bootstrap + admin/seller `me/capabilities` still module-owned, one mapping each |

## Branch parity (verbatim, unchanged)

1. `decision.Kind == Allow` → `return`
2. `permissionId == "accesscontrol.view"` → `return`
3. `decision.Kind == Unavailable` → `throw new PlatformHttpException(503, "سرویس مجوز در دسترس نیست.", "access.authorization.unavailable")`
4. `permissionId == "accesscontrol.manage"` → `return`
5. otherwise → `throw new PlatformHttpException(403, "مجوز این عملیات وجود ندارد.", "access.capability.denied")`

Fail-open behavior intentionally **unchanged** in this task.

## AuthorizationCheck parity (unchanged)

- `Subject = AuthorizationSubject.ForUser(actorUserId)`
- `Resource = { Type = AuthorizationObjectTypes.Permission, Id = permissionId }`
- `Permission = AuthorizationRelations.Check`
- `CallContext = { Edition = ToobaEdition.SingleStore, TenantId = tenant.Current?.TenantId.Value ?? "unknown" }`

## Accepted routes (from ARCHITECT-ACCEPTED 50a31d2b)

- `POST /v1/admin/access-control/bootstrap` → `AccessControlAdminEndpoints.cs:21`
- `GET /v1/admin/access-control/me/capabilities` → `AccessControlAdminEndpoints.cs:22`
- `GET /v1/seller/access-control/me/capabilities` → `AccessControlSellerEndpoints.cs:20`

## Canonicalization

- Added `docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1.task.md`.
- This evidence file added under `docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1/`.

## Focused builds

- `dotnet build Tooba.AccessControl.Application.csproj --no-restore`: Build succeeded, 0 errors.
- `dotnet build Tooba.Host.csproj --no-restore`: Build succeeded, 0 errors.
- No test suite, no solution build, no retry loop.

## Boundaries honored

- No route migrated; permission catalog untouched.
- Fail-open behavior not altered.
- `AccessControlDevelopmentSeed.cs`, `AccessControlDemoSnapshot.cs` untouched.
- Fulfillment, Checkout, frontend untouched.
- No validators added.
