# final-media.md — TB-P07-T035

`USER_VISUAL_ACCEPTED=NO`

## Seed media polish

`CatalogDemoMediaFactory` now emits patterned RGB PNGs up to 320×320 (`*-v2.png`), with fixed IDAT chunk writer (no 4KB payload cap). Reset removes `demo-media-*` then reseeds.

## Live proof

- Sample primary asset length ~24KB, decode **320×320**.
- Admin VIEW/EDIT `<img naturalWidth/Height=320>`.
- Exactly 5 media / product, one Primary.

## Policy

Catalog does not store binaries; Media DAM owns bytes. Demo images are generated placeholders (not stock photography). Commercial density improved vs prior solid 48×48 gray tiles.
