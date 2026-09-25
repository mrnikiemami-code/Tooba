# TB-TMAR-HOST-ACCESSCONTROL-ENDPOINTS-FOUNDATION-001 — Bootstrap route migration

Mode: one complete route only. No skeleton that delegates back to Host.

## Migrated route

`POST /v1/admin/access-control/bootstrap` — moved from Host to a new module-owned Endpoints project
on a real `ISender` path.

| | Before | After |
| --- | --- | --- |
| Mapping | `AccessControlEndpoints.cs` (`admin.MapPost("/bootstrap", AdminBootstrapAsync)`) | `Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs` |
| Handler | `AccessControlEndpoints.AdminBootstrapAsync` (Host) | `BootstrapAsync` thin endpoint + `EnsureAccessControlBootstrapHandler` (Application) |
| Auth | `Tooba.Host.Admin.AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct)` | `Tooba.BuildingBlocks.Security.IAdminPanelAccess.RequireAuthorizedAsync(request, ct)` (generic seam) |
| Dispatch | direct `IAccessControlDirectory.EnsureBootstrapAsync(...)` | `ISender.Send(new EnsureAccessControlBootstrapCommand(...))` |
| Response | `Results.Json(new { ok = true })` | identical `Results.Json(new { ok = true })` |

## New files

### A. Application CQRS

`src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Commands/EnsureBootstrap/EnsureAccessControlBootstrapCommand.cs`

- `EnsureAccessControlBootstrapCommand(Guid ActorUserId, string? TenantId) : IRequest<Result>`
- `EnsureAccessControlBootstrapHandler : IRequestHandler<EnsureAccessControlBootstrapCommand, Result>`
- Handler owns the existing `_directory.EnsureBootstrapAsync(request.ActorUserId, Array.Empty<Guid>(), request.TenantId, ct)` call — empty `<Guid>()` semantics preserved exactly.
- No FluentValidation validator (actor/tenant are trusted authorization/context-derived values).
- No direct HTTP types in Application.

### B. Endpoints project

`src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/`

- `Tooba.AccessControl.Endpoints.csproj` — `FrameworkReference Microsoft.AspNetCore.App`, references `Tooba.AccessControl.Application` + `Tooba.BuildingBlocks` only. **No Host reference.**
- `AccessControlEndpointModule.cs` — `MapAccessControlModuleEndpoints(this IEndpointRouteBuilder)` maps group `/v1/admin/access-control` and delegates to `AccessControlAdminEndpoints.Map`.
- `Admin/AccessControlAdminEndpoints.cs` — `Map(RouteGroupBuilder)` maps only `"/bootstrap"`; handler injects `ISender`, `IAdminPanelAccess`, `ICurrentTenant`; no `IAccessControlDirectory`, no Host types.

## C. Host changes

- Removed only the `admin.MapPost("/bootstrap", AdminBootstrapAsync);` mapping and the `AdminBootstrapAsync` method from `AccessControlEndpoints.cs`. All other routes/handlers unchanged; no rename/reformat of the file.
- Added `using Tooba.AccessControl.Endpoints;` and `app.MapAccessControlModuleEndpoints();` after the existing `app.MapAccessControlEndpoints();`.
- Registered the Application assembly with the canonical CQRS foundation (`AddToobaCqrsFoundation(... EnsureAccessControlBootstrapCommand ...).Assembly`).
- Added `Tooba.AccessControl.Endpoints` project reference for composition.

## Validation

- Host bootstrap residue: **ZERO** (`AdminBootstrapAsync`, `/bootstrap`, `EnsureBootstrapAsync` all absent from `AccessControlEndpoints.cs`).
- Production mappings for `POST /bootstrap`: **exactly one** (`AccessControlAdminEndpoints.cs:19`).
- New endpoint uses `ISender` and contains **no** direct `IAccessControlDirectory`.
- `AccessControl.Endpoints` → Host project references: **ZERO**.
- `dotnet build Tooba.AccessControl.Endpoints.csproj --no-restore`: Build succeeded, 0 errors (one-time restore performed to generate assets for the new csproj).
- `dotnet build Tooba.Host.csproj --no-restore`: Build succeeded, 0 errors.

## Boundaries honored

- `AccessControlDevelopmentSeed.cs`, `AccessControlDemoSnapshot.cs` untouched.
- User enrichment, catalog scope-resource handlers, role/assignment/ceiling routes, seller routes untouched.
- Fulfillment, Checkout, frontend untouched.
- No `AccessControl.Contracts` created (route has no transport body).
- MediatR remains 12.5.0 via the canonical `Tooba.BuildingBlocks` `AddToobaCqrsFoundation` path.
