# 16 — Data Grid consistency (TB-P06-T029)

## Rule

Use approved Data Grid where operational tabular pages require typed filters/sort/visibility/pagination. Do **not** retrofit grids where Shopeiva card/list is the locked pattern (storefront cards, dashboard summary rows).

## Commercial operational tables (current)

| Surface | Pattern | Notes |
| --- | --- | --- |
| Customer orders list | Panel list / table | LIVE; seeded order detail opens |
| Seller orders / returns / products | Vendor operational tables | LIVE with `sellerPartyId` |
| Admin orders / tickets / catalog | Admin tables | LIVE |
| Access control role/permission matrices | Shared ACC UI (T024) | Matrix + editors — not a foreign enterprise grid rewrite |
| Storefront listing | Shopeiva product cards + filters | Correct locked pattern — **not** Data Grid |

## Observations this gate

- No mandate to replace card/list shells with grids on storefront or panel dashboards.
- Operational pages remain usable with existing pagination/filter bindings from prior commercial tasks.
- No new grid-standard violation elevated to COMMERCIAL_BLOCKER.

## Verdict

Grid vs card usage consistent with Shopeiva lock + prior ACCEPTED operational UIs. No retrofit campaign in T029.
