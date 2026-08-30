# TB-P07-T029 Evidence

## Scope
Real Media DAM + Upload Library wired into Product Media and Category image/icon/banner.

## R1 repair (TB-P07-T029-R1)
- Application-level upload state/progress UX (`upload-ux.md`)
- Full backend + frontend validation (`full-validation.md`)
- `git diff --check` PASS
- Live Host/FE/Shopeiva kept alive; FE restarted to load HEAD

## Architecture
- Media owner: `Tooba.Media` module (schema `media`)
- Catalog binary storage: none (only `MediaAssetId` refs; Category banner column added)
- Storage abstraction: `IMediaObjectStore` / `LocalFileMediaStore`
- Config: `Tooba:Media:LocalRoot=App_Data/media`, `MaxUploadBytes=5000000`
- Thumbnails: originals served (no derived thumbnail generation)

## USER_VISUAL_ACCEPTED
NO
