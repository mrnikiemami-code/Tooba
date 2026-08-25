# 01 — Level-3 Source Audit

Task: `TB-P05-T016`

## Shopeiva reference

Primary source (external purchased bundle):

`SarvNewVerRequirment/reference/shopeiva/src/components/common/Header/Header.jsx`

Static fixture in repo: `src/frontend/public/jsons/menuCategories.json`

| Level | Shopeiva representation | Tooba binding |
| --- | --- | --- |
| L1 | `categories[]` rail with icon | published Catalog roots (`parentCategoryId = null`) |
| L2 | `subcategories[].name` headings | published children of selected root |
| L3 | `subcategories[].items[]` string leaves | published grandchildren keyed by real `categoryId` |

## Mobile

Shopeiva mobile drawer: L1 accordion → L2 grid. Task requires L1 → L2 → L3; Tooba extends nested accordion under each L2 without changing drawer shell geometry.

## Deviations fixed in T016

| Gap before T016 | Fix |
| --- | --- |
| Catalog seed stopped at L2 (8 + 24) | L3 leaves derived from demo product names (72 leaves) |
| Desktop UI already rendered descendants but list empty | populated Catalog hierarchy |
| Mobile stopped at L2 grid | nested L3 accordion under L2 |
| No fake frontend-only hierarchy | flat `/v1/storefront/categories` unchanged; client projection only |
