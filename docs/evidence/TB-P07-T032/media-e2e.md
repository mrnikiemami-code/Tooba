# Media E2E — TB-P07-T032

## Flow
upload → MediaAsset → library refresh → select → assign → preview → reload persists
(Product media panel + Category image/icon/banner).

## Persian UI
- Removed English implementation copy from Media Library dialog (no `lang=en` status dual-labels, no «Retry» English, no «Upload in progress» bilingual).
- Status labels Persian-only via `mediaUploadStateLabel(..., "fa")`.
- Toast on Product attach: «رسانه به محصول اضافه شد.»

## Architecture
- Real Media DAM; Catalog stores references only (no binary in Catalog).
