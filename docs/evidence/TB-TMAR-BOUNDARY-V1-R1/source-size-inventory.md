# Source-size inventory — TB-TMAR-BOUNDARY-V1-R1

Scan: repository-wide hand-written sources (`.cs`, `.ts`, `.tsx`, `.js`, `.jsx`, `.mjs`, `.cjs`, `.sql`, `.ps1`, …)

Excluded: `node_modules`, `bin`, `obj`, `dist`, `tmp`, build, lockfiles, EF Migrations/snapshots, `wwwroot/lib`, auto-generated markers, `docs/evidence`, minified assets, ephemeral `.tmp-*` helpers.

## Totals

| Metric | Count |
|---|---|
| Files scanned | 1751 |
| CRITICAL_GOD_FILE | 15 |
| OVERSIZED_LEGACY | 40 |
| WATCH | 88 |
| NORMAL | 1604 |

## By language (included)

cs 808 · tsx 333 · ts 381 · mjs 193 · cjs 13 · js 15 · ps1 3 · sql 1

## Classification thresholds

Backend C#: NORMAL ≤500 · WATCH >500 · OVERSIZED_LEGACY >800 · CRITICAL_GOD_FILE >1500

Frontend TS/TSX/JS: NORMAL ≤500 · WATCH >500 · OVERSIZED_LEGACY >800 · CRITICAL_GOD_FILE >1200

## Machine-readable

`docs/evidence/TB-TMAR-BOUNDARY-V1-R1/source-size-inventory.json`

Refreshed by TB-TMAR-FE-BASELINE (architecture baseline; no oversized growth).

## Top physical LOC (verified)

| Path | LOC | Class | Area |
|---|---:|---|---|
| CatalogDirectory.cs | 5124 | CRITICAL | Catalog/Infrastructure |
| AdminOrderOperationsComposer.cs | 2468 | CRITICAL | Host |
| CatalogDomain.cs | 2305 | CRITICAL | Catalog/Domain |
| category-admin-screen.tsx | 2215 | CRITICAL | Frontend |
| ProductWorkspaceComposer.cs | 1910 | CRITICAL | Host |
| StorefrontComposer.cs | 1579 | CRITICAL | Host |
| product-workspace-screen.tsx | 1570 | CRITICAL | Frontend |
| content-article-admin-screen.tsx | 1528 | CRITICAL | Frontend |
| admin-landing-page-composer.tsx | 1440 | CRITICAL | Frontend |
| admin-api.ts | 1326 | CRITICAL | Frontend |
| StorefrontEndpoints.cs | 1242 | OVERSIZED_LEGACY | Host |

Independent-review examples verified present (not blindly trusted): CatalogDirectory ~5k+, CatalogDomain ~2k+, AdminOrderOperationsComposer ~2k+, StorefrontEndpoints ~1.2k+, large admin FE screens.
