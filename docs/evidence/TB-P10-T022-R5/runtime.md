# Runtime — TB-P10-T022-R5

Host :5088 + FE :3000 after migrate/seed.

| Check | Result |
|---|---|
| A StoreTemplate fashion | PASS — key=`fashion`, name=`پوشاک`, id=`019022a5-…f001` |
| B 8 top-level TemplateCategory | PASS — purity.templateTopLevelCategoryCount=8 |
| C L2/L3 under each root | PASS — unit test + 56 total category rows |
| D 15 TemplateProducts | PASS |
| E Brands | PASS — 6 |
| F Banners | PASS — BannerShowcase items=2 |
| G Product media | PASS — 15 primary media refs + local `/images/fashion-template` |
| H `/template-preview/fashion` | PASS — loads Template Catalog |
| I–L Sample-only categories/products/brands/banners | PASS — API purity.isPure=true; operational*Hits=0 |
| M Desktop/Tablet/Mobile | PASS — screenshots + Admin iframe viewport contract retained |
| N No store mixing | PASS — `data-template-purity=pure` |

API probe artifact: `.tmp-t022r5-preview.json` (local; not committed).
Screenshots: `docs/evidence/TB-P10-T022-R5/screenshots/`.
