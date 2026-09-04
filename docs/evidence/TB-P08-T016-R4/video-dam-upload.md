# Video DAM upload

- Upload: `POST /v1/admin/media/upload` (multipart).
- List filter: `GET /v1/admin/media?contentTypePrefix=video/` (or `kind=video`).
- FE media library passes `contentTypePrefix` from `contentTypePrefixForKind(assetKind)`.
- Backend `MediaDirectory` / `MediaEndpoints` apply prefix filter so video grids are not empty due to image-only defaults.
