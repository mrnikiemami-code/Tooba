# TB-TMAR-HOST-MEDIA-EVACUATE-001 — Analyze

## Audit result (mandatory audit first)

| Question | Finding |
| --- | --- |
| Does `Tooba.Media.Endpoints` exist? | **No.** Media module held only `Domain`, `Contracts`, `Application`, `Infrastructure`. |
| Media Application seams | `IMediaDirectory`, `IMediaObjectStore`, `MediaAssetInfo`, `MediaPagedResult<T>` in `Tooba.Media.Application`. |
| Media module in solution? | Media was absent from `Tooba.slnx` entirely (loose folder). |
| Host-owned Media surface | `src/backend/Host/Tooba.Host/Media/MediaEndpoints.cs` (203 lines, `namespace Tooba.Host.Media`). |
| Host Media mapping | `Program.cs:44 using Tooba.Host.Media;` + `Program.cs:496 app.MapMediaEndpoints();` |
| Second consumer | `Storefront/StorefrontEndpoints.cs:176-178` called `Tooba.Host.Media.MediaEndpoints.TryServeStoredMediaAsync` / `PlaceholderSvg`. |
| Authorization | Host code used certified `Tooba.Host.Admin.Access.AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, ct)`. |

## Neutral seam decision

`Tooba.BuildingBlocks.Security.IAdminPanelAccess` is a single-method neutral platform seam
(`Task<Guid> RequireAuthorizedAsync(HttpRequest, CancellationToken)`) already referenced by
module endpoint projects. Per task instruction, Media.Endpoints injects it directly — **no**
Media-specific authorizer adapter was created.

## Canonical project pattern chosen

Two canonical endpoint shapes exist in the repo:

1. `Application + Contracts + BuildingBlocks` (Order, Settlement, Support, Content).
2. `Application + BuildingBlocks` (platform-neutral authorizer modules, e.g. ProductWorkspace).

Media.Endpoints follows shape 2 because the area uses the platform seam rather than a
Contracts-owned module authorizer. Project name, folder placement, and solution grouping match
the five-project module pattern; all five Media projects were registered in `Tooba.slnx`.

## Evacuation shape

- `MediaEndpointModule.MapMediaModuleEndpoints(IEndpointRouteBuilder)` — thin composition seam.
- `Admin/MediaAdminEndpoints` — upload/query/get handlers + `ResolveContentTypePrefix`.
- `Admin/MediaAssetServing` — stored-media serving helper, range processing, placeholder SVG.
