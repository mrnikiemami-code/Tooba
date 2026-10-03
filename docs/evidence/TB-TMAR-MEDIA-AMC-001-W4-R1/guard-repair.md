# TB-TMAR-MEDIA-AMC-001-W4-R1 — Guard repair

## Changes
- `MediaModuleAmcW3CqrsGuardTests`:
  - rejects any `Results.Json` in Admin endpoints (not only `Results.Json(new { title`)
  - requires `MediaUploadBatchResponse` + `api.From`
  - new `Media_json_api_endpoints_have_zero_direct_Results_Json` for Admin/Storefront/Module; serving may keep `Results.File` / `Results.Text` only
- `MediaModuleAmcW4CertGuardTests`: asserts `structureLock.certifiedModules` contains Media exactly once
