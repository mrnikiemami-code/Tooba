# Runtime — TB-P10-T022-R6

Host :5088 + FE :3000 after migrate `20260915180000_AddTemplateCatalogParity` + structural seed.

| Check | Result |
|---|---|
| A StoreTemplate fashion | PASS |
| B 8 top-level TemplateCategory | PASS — purity.templateTopLevelCategoryCount=8 |
| C Fashion counts | PASS — 15 products, 6 brands, BannerShowcase 2 |
| D Structural parity seed | PASS — attrs=2, tags=2, mega=8, variants=15, history=15, slugHist=8, bindings=8, facets=8 |
| E Category media | PASS — root `imageUrl`=/images/fashion-template/{1..8}.jpg; DOM `data-category-media=template` ×8 |
| F `/template-preview/fashion` | PASS |
| G–K Sample-only purity | PASS — isPure=true; operational*Hits=0; FE `data-template-purity=pure` |
| L Desktop/Mobile preview | PASS — screenshots |

API probe: `.tmp-t022r6-preview.json` (local).
Screenshots: `docs/evidence/TB-P10-T022-R6/screenshots/`.
