# Live Primary Category Migration (TB-P07-T036-R1)

Source: `live-r1-proof.mjs` → `live-r1-proof.json` (section A: **10/10 pass**)

## Sequence
1. Created temporary Draft product (Primary = پاوربانک L3).
2. Snapshot: Primary, Additional, attributes, variants editor, readiness.
3. `POST …/category-change-preview` to یخچال L3 — **non-mutating** (attribute value count + Primary unchanged).
4. Cancel path = no further mutation after preview.
5. `PUT …/primary-category` confirm — Primary switched transactionally; orphans left active schema; readiness blocked for newly required missing values.
6. Deleted temp product (deterministic restore / cleanup).

## APIs
- `POST /v1/admin/products`
- `POST /v1/admin/catalog/products/{id}/category-change-preview`
- `PUT /v1/admin/catalog/products/{id}/primary-category`
- attribute GET/PUT + readiness + delete product
