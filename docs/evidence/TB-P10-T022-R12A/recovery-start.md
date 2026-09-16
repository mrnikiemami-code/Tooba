# Recovery Start — TB-P10-T022-R12A

## Preflight

| Check | Value |
|---|---|
| branch | `main` |
| HEAD | `9baccae6c0a0a2d9f98a4ca3e786ea0b37906ead` |
| origin/main | `9baccae6c0a0a2d9f98a4ca3e786ea0b37906ead` |
| HEAD == origin/main | YES |
| Expected previous HEAD | `9baccae6c0a0a2d9f98a4ca3e786ea0b37906ead` |
| git status tracked | clean (untracked `.tmp-*` / evidence leftovers preserved) |
| Last Architect-accepted | TB-P10-T022-R11 (task: ACCEPTED) |
| Last Implementation (pre) | TB-P10-T022-R11 |
| Current Task | TB-P10-T022-R12A |
| Locks preserved | LOCK-SF-001…341 |
| Appearance | USER_VISUAL_ACCEPTED=YES |
| Builder | USER_VISUAL_ACCEPTED=NO |
| RECOVERY_CONFLICT | none |

## Audit snapshot (pre-implementation)

| Surface | Finding |
|---|---|
| StoreTemplate rows | Fashion only (`key=fashion`) via `FashionTemplateCatalogSeed` |
| Fashion seed | `Admin/FashionTemplateCatalogSeed.cs` — 8 roots × 3 levels, 15 products, brands, banner, R6 parity |
| Template repositories | Existing `Template*` entities / `CatalogDbContext` sets — schema parity OK |
| Preview route pattern | `/template-preview/fashion` (+ `/full`) + Host `GET /v1/storefront/template-catalog/fashion/preview` |
| Selector registry | `INDUSTRY_TEMPLATE_SEEDS` has 10 industries; live iframe only for Fashion |
| Composition seeds | Batch A recipes already present (`auto-parts`, `building-supplies`, `tools-hardware`) |
| Languages | Template translations fa-IR; configured-language fallback elsewhere |
| Media conventions | Fashion: `/images/fashion-template/{1..8}.jpg` — Batch A must use isolated namespaces |
| Unrelated work | Untracked `.tmp-*` preserved; not staged |

## Batch A scope (this task only)

1. `auto-parts` — لوازم یدکی خودرو
2. `building-materials` — لوازم ساختمانی (rename registry key from `building-supplies`)
3. `tools-hardware` — ابزار و یراق

Do **not** start R12B / T023.
