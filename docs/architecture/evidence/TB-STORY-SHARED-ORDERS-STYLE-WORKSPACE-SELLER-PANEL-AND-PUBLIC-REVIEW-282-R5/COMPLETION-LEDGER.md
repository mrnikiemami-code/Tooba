# COMPLETION-LEDGER — TB-282-R5

| Item | Method / proof | Status |
| --- | --- | --- |
| A F1 per-slide title | `CtaProductTitle` + `ICatalogAdminProductTitleIdLookup` Enrich; FE no `selectedProductLabel` fallback; R5 mount two-slide + cancel | DONE |
| A unavailable explicit | null title → `productUnknown`; mount fact | DONE |
| B F2 update_item PUT | mounted admin+seller one PUT; media/version/key retained | DONE |
| B retention empty/non-JSON/wrong-story | executor finalize + mount fact; envelope retained | DONE |
| B precise negative control | temp empty-200 success → same fact fails → restore green | DONE |
| B grid/saved-view | `story-column-order.test.ts` + R5 A remount | DONE |
| B 390/1440 geometry | CDP Emulation overflowX=0 on served `/fa/admin/stories` | DONE |
| C real Catalog query | `CatalogPublishedProductSearch282R5Tests` on real lookup (not FakePublishedSearch) | DONE |
| C retain R4 seller isolation | `StoryProductOptions282R4` 2/2 | DONE |
| D serve / tsc / build | Host republish + Next BUILD_ID Spxf…; routes 200; tsc 0 | DONE |
| ZERO migration / live write | no MigrationRunner; no Story/Media POST | DONE |
| Remaining | critical-storefront bg-white unrelated; live owner walkthrough pending | OPEN (disclosed) |
