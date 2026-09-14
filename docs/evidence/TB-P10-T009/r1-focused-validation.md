# TB-P10-T009-R1 — Focused validation

- Host `StoreAppearance*` rebuild skipped: live Host :5088 holds `Tooba.Host` binaries. Live PUT invalid skin `custom-html` = HTTP 400. Live GET appearance after restore = tooba-blue / LightOnly / classic. T009 Host suite already on tree (16/16).
- FE appearance + skin + geometry: 12 pass
- `npm run test:storefront`: 67 pass
- `npm run test:critical-storefront`: 18 pass
- recovery staleness: 4 pass
- `git diff --check`: clean
