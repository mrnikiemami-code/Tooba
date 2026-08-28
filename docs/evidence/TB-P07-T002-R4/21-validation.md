# TB-P07-T002-R4 — Validation

## Frontend
| Gate | Result |
|------|--------|
| typecheck | 0 |
| test:grid | 17 pass (incl. saved-view-state + filter round-trip) |
| lint | 0 errors (img warning only) |
| build | 0 |

## Validation contract
- `toAgFilterModel` ↔ `fromAgFilterModel` round-trip for text/number/date
- `captureColumnLayoutFromApi` / `buildAgColumnApplyState` width+sort restore
- `agFilterModelForSavedView` excludes advanced-only fields (status enum drawer)
- `normalizeSavedViewForPersistence` JSON clone contract

## Saved views UX (AppDataGrid)
| Feature | Status |
|---------|--------|
| Save filters/sorts/pageSize from live queryRef | LIVE |
| Save column order/visibility/widths | LIVE |
| Apply restores AG filter model + column state + server query | LIVE |
| Active view pill indicator | LIVE |
| Delete saved view | LIVE |
| Clear active view on manual filter/sort/search change | LIVE |

## Backend
No changes (R1 engine + R3 Community features preserved).
