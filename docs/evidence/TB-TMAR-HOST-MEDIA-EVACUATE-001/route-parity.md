# TB-TMAR-HOST-MEDIA-EVACUATE-001 — Route Parity

## Route table (before → after)

| Route | Before (Host) | After (Media.Endpoints) | Parity |
| --- | --- | --- | --- |
| `POST /v1/admin/media/upload` | `admin.MapPost("/upload", UploadAsync).DisableAntiforgery()` | same, `Admin/MediaAdminEndpoints.cs` | Exact |
| `GET /v1/admin/media/` | `admin.MapGet("/", QueryAsync)` | same | Exact |
| `GET /v1/admin/media/{id:guid}` | `admin.MapGet("/{id:guid}", GetAsync)` | same | Exact |
| `GET /v1/media/{id:guid}` | `app.MapGet("/v1/media/{id:guid}", ServeAsync)` | `MediaEndpointModule` → `MediaAssetServing.ServeAsync` | Exact |

Group prefix `"/v1/admin/media"` is unchanged.

## Query parameter parity

`QueryAsync` signature preserved verbatim:

```
string? search = null, string? contentTypePrefix = null, string? kind = null,
int page = 1, int pageSize = 24, CancellationToken cancellationToken = default
```

`ResolveContentTypePrefix` moved unchanged — `contentTypePrefix` trim wins; otherwise
`kind` maps `image → "image/"`, `video → "video/"`, `file → "application/pdf"`, default `null`.

## Upload parity

- `!request.HasFormContentType` → 400 `media.upload.failed`, title "درخواست multipart لازم است."
- `files.GetFiles("files")` then `form.Files` then empty array fallback.
- zero files → 400 `media.upload.failed`, title "هیچ فایلی برای آپلود ارسال نشده است."
- per-file result shape unchanged: `{ items: [ { ok, asset } | { ok:false, fileName, title, errorCode } ] }`
- `PlatformHttpException` → `Results.Json(new { title, errorCode }, ex.StatusCode)`.

## Get / serve parity

- unknown id → 404 `{ title: "رسانه یافت نشد.", errorCode: "media.missing" }`.
- `TryServeStoredMediaAsync`: `GetAsync` → `GetStorageKeyAsync` → `OpenReadAsync` →
  `Results.File(stream, info.ContentType, enableRangeProcessing: true)`.
- fallback `PlaceholderSvg(id)` byte-identical (same hue formula, same SVG text, same
  `image/svg+xml; charset=utf-8`).

## Authorization parity

All three admin handlers call the neutral platform seam exactly once each
(`adminAccess.RequireAuthorizedAsync(request, ct)`) — behavior-equivalent to the prior
`AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, ct)`.

## Deliberate non-changes (no redesign)

`IMediaDirectory` / `IMediaObjectStore` semantics, upload business rules, storage behavior,
persistence, schemas, migrations, frontend — untouched.
