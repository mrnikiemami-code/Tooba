# TB-TMAR-HOST-MEDIA-EVACUATE-001 — Closure

## Delivered

- **New project** `src/backend/Modules/Media/Tooba.Media.Endpoints` following the canonical
  module Endpoints pattern (`Application` + `BuildingBlocks`), registered with the other four
  Media projects in `src/backend/Tooba.slnx`.
- `MediaEndpointModule.MapMediaModuleEndpoints(IEndpointRouteBuilder)` — thin composition seam
  consumed by Host `Program.cs`.
- `Admin/MediaAdminEndpoints` — the three admin handlers, `ResolveContentTypePrefix`,
  unchanged titles/statuses/error codes.
- `Admin/MediaAssetServing` — stored-media serving helper (range processing) + placeholder SVG.
- **Removed** `src/backend/Host/Tooba.Host/Media/MediaEndpoints.cs` and the Host `Media/` folder.
- Host `Program.cs`: dropped `using Tooba.Host.Media;`, dropped `app.MapMediaEndpoints();`,
  added `app.MapMediaModuleEndpoints();`.
- Host `Tooba.Host.csproj`: added `Tooba.Media.Endpoints` project reference.
- `Storefront/StorefrontEndpoints.cs`: now consumes `Tooba.Media.Endpoints.Admin.MediaAssetServing`.
- `MediaDamTests.cs` + `AdminPanelCompositionTests.cs`: repointed to module ownership and the
  neutral seam (the `AdminPanelAccess.RequireAuthorizedAsync` count assertion became
  `adminAccess.RequireAuthorizedAsync`, still exactly 3).
- New durable guard `HostMediaEvacuationGuardTests` (9 facts).

## Preserved

- All four route paths, group prefix, query parameters/defaults, upload form semantics,
  per-file result shape, 400/404 statuses, `media.upload.failed` / `media.missing`,
  range processing, SVG fallback bytes, `DisableAntiforgery()`.
- `Tooba.Host/Admin` certification — 15 nested files, 0 top-level files, no writes,
  certification guard untouched.
- Media business behavior: `IMediaDirectory` / `IMediaObjectStore` semantics, storage,
  persistence, migrations, schema, frontend.

## Result

`PASS` — Host Media endpoint implementation removed, Media module owns all four routes,
Media.Endpoints has zero Host dependency, route/behavior parity proven by focused guards,
Host build passes, Admin certification untouched.

`workflowStop = USER_REVIEW_HOST_MEDIA_EVACUATE_001`

STOP — no OperatorProfile, no Seller/Storefront/root boundary work. Awaiting Architect review.
