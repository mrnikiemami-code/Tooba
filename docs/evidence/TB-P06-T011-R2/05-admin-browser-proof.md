# 05 — Admin browser proof (TB-P06-T011-R2)

No direct Shopeiva Admin returns surface.

## Basis

- T024 Admin operational shell (`admin-shell.tsx`, DataGrid)
- Shopeiva vendor order detail card density (stat/grid patterns)

## Captures

| Capture | Route | File |
| --- | --- | --- |
| Returns grid desktop | `/admin/returns` | `captures/08-tooba-admin-returns-list-desktop.png` |
| Returns grid mobile 390×844 | `/admin/returns` | `captures/14-tooba-admin-returns-mobile.png` |

Admin actor from dev context: `01a036c2-970e-7000-8eb7-94bf5cc2d8db`.

Grid shows live Host read (empty state when no rows) — no fabricated refund amounts or statuses.
