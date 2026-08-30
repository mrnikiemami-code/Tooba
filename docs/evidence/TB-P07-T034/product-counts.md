# Product counts — TB-P07-T034

## Live seed (`POST /v1/admin/catalog/demo/reset-and-seed`)

| Metric | Count |
|--------|------:|
| Roots | 15 |
| L2 | 28 |
| L3 | 73 |
| Brands | 22 |
| Tags | 36 |
| AttributeDefinitions | 41 |
| Attribute options | 78 |
| Category bindings | 190 |
| Facets | 117 |
| Category media | 247 |
| MegaMenu | 99 |
| **Products** | **283** |

Range target: 219–365 — **PASS**.

Deterministic per-leaf count: 3 / 4 / 5 via stable hash of L3 full key (not uniform).

Slug prefix: `demo-prod-{l3Key}-{n}`.

Lifecycle: all Draft; Published = 0 (enforced by seeder; never calls Publish).
