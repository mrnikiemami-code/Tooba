# TB-P07-T029 Evidence

## Scope
Real Media DAM + Upload Library wired into Product Media and Category image/icon/banner.

## Architecture
- Media owner: `Tooba.Media` module (schema `media`)
- Catalog binary storage: none (only `MediaAssetId` refs; Category banner column added)
- Storage abstraction: `IMediaObjectStore` / `LocalFileMediaStore`
- Config: `Tooba:Media:LocalRoot=App_Data/media`, `MaxUploadBytes=5000000`

## Validation
- Host tests filter MediaDam|AdminPanelComposition|CatalogCategoryFoundation: **11 passed**
- FE `npm run test:admin`: **101 passed**
- FE `npm run test:category-tree`: **63 passed**
- Migrations apply store-alpha: Catalog `20260830070000_AddCategoryBannerMedia`, Media `20260830060000_InitialMedia`
- Live Host `:5088` health live/ready **200**; FE `:3000` admin **200**

## Live API (see live-verify.log)
- multipart upload PNG → ok asset
- text/plain rejected → `media.type.unsupported`
- library paging returns uploaded asset
- storefront serve returns `image/png`
- product attach **201**, unassign **200**, asset metadata still present (unassign ≠ delete)
- category image/icon/banner assign **200**, clearBanner **200**

## USER_VISUAL_ACCEPTED
NO
