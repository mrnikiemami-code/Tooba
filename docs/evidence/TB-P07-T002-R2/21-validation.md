# TB-P07-T002-R2 — Validation

## Frontend
| Gate | Result |
|------|--------|
| typecheck | 0 |
| lint | 0 (img warning pre-existing) |
| test:grid | 11 pass |
| npm test | all green |
| build | 0 |

## Backend
No backend changes in R2.

## UX acceptance closed
| Gap | Fix |
|-----|-----|
| AG column filters client-only | `onFilterChanged` → `fromAgFilterModel` → server reload |
| No applied filter chips | `app-grid-filter-chips` with per-chip clear |
| No clear-all | toolbar clear filters button |
| Status text filter | `agSetColumnFilter` Published/Draft/Archived |
| Host contract | `toHostGridQuery` status single-value → `equals` |

## Live
- Host :5088 unchanged (R1 query engine live)
- `/admin/products` uses server filters end-to-end
