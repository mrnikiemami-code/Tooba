# Upload UX — TB-P07-T029-R1

## Application-level upload state
Media Library upload tab (`MediaLibraryDialog`) now shows per-file rows with:

| State | fa | en |
| --- | --- | --- |
| queued | در صف | Queued |
| uploading | در حال بارگذاری | Uploading |
| succeeded | موفق | Succeeded |
| failed | ناموفق | Failed |

## Progress
- Transport: `XMLHttpRequest` via `uploadAdminMediaFileWithProgress`
- When `lengthComputable`: determinate % bar (`data-progress-mode=determinate`)
- Otherwise: indeterminate pulse bar (`data-progress-mode=indeterminate`) — **no fake percentage**

## Extra UX
- Overall uploading locks file input / confirm / close (`data-uploading=true`) to block duplicate submit
- Failed rows: localized fa + en messages from centralized `admin-error-map`
- Retry button (`admin-media-upload-retry`)

## Markers
- `data-testid=admin-media-upload-rows`
- `data-testid=admin-media-upload-progress`
- `data-upload-state={queued|uploading|succeeded|failed}`
