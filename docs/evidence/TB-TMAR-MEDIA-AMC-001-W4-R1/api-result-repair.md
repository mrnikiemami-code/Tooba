# TB-TMAR-MEDIA-AMC-001-W4-R1 — API result repair

## Defect
`MediaAdminEndpoints.UploadAsync` returned ad-hoc `Results.Json(new { items = results }, statusCode: 200)`.

## Repair
- Added Application DTO `MediaUploadBatchResponse` (`Assets/Models/`).
- Upload success path now returns `api.From(Result.Success(new MediaUploadBatchResponse(results)))`.
- `ApiResponseFactory.From` emits raw JSON value at HTTP 200 — parity with prior aggregate envelope `{ items: [...] }` and per-item ok/asset or ok/fileName/title/errorCode.

## Not changed
- Route shape, multi-file partial-success loop, binary `Results.File` / placeholder `Results.Text`.
