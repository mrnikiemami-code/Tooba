# 09 — Level-3 Navigation Proof

Task: `TB-P05-T016`

Verified live against Host `http://127.0.0.1:5088`.

| Route level | Pattern | Example |
| --- | --- | --- |
| L1 | `/products?categoryId={rootId}` | family root listing |
| L2 | `/products?categoryId={childId}` | second-level listing |
| L3 | `/products?categoryId={leafId}` | leaf listing |

All links are generated from live Catalog `categoryId` values in `storefront-header.tsx`. No hardcoded demo-only URLs.

Probe: `_api-probe.json` (`sampleThirdLevelLink`).

Sub-tree filtering uses existing `StorefrontComposer.DescendantCategoryIds` (BFS over flat category list).
