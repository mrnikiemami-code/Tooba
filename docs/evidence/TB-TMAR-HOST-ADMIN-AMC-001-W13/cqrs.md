# CQRS — W13 ProductMedia

| Route | Request | Handler | Port |
|---|---|---|---|
| GET media | GetProductMediaQuery | GetProductMediaHandler | ListAsync |
| GET media/readiness | GetProductMediaReadinessQuery | GetProductMediaReadinessHandler | GetReadinessAsync |
| POST media | AttachProductMediaCommand | AttachProductMediaHandler | AttachReferenceAsync + List |
| POST media/placeholder | AttachPlaceholderProductMediaCommand | AttachPlaceholderProductMediaHandler | AttachPlaceholderAsync + List |
| PUT media/order | ReorderProductMediaCommand | ReorderProductMediaHandler | ReorderAsync + List |
| PUT media/{assetId}/primary | SetPrimaryProductMediaCommand | SetPrimaryProductMediaHandler | SetPrimaryAsync + List |
| PATCH media/{assetId} | PatchProductMediaCommand | PatchProductMediaHandler | PatchAltAsync + List |
| DELETE media/{assetId} | DetachProductMediaCommand | DetachProductMediaHandler | DetachAsync + List |

All return `Result` / `Result<T>` → ApiResponseFactory (POST attach/placeholder → 201 JSON without Location, Host parity).
Endpoints inject ISender only (no directory/DbContext/Composer).
