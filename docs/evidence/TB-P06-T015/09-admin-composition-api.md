# 09 — Admin composition API (TB-P06-T015)

Base: `/v1/admin/page-composition/home`  
Auth: `AdminPanelAccess` (actor example: `01a036c2-970e-7000-8eb7-94bf5cc2d8db`)

| Method | Path | Purpose |
|---|---|---|
| GET | `/v1/admin/page-composition/home` | Full home composition (incl. hidden) |
| GET | `.../catalog` | Section catalog + schema |
| PUT | `.../reorder` | Ordered section ids |
| PUT | `.../sections/{id}` | Visibility / config / variant |
| POST | `.../sections` | Add catalog type |
| DELETE | `.../sections/{id}` | Remove |
| POST | `.../restore-default` | Reset to default home order |

Public:

| Method | Path | Purpose |
|---|---|---|
| GET | `/v1/storefront/home/composition` | Ordered **visible** sections only |

Host files: `PageCompositionEndpoints.cs`, `PageCompositionPanelComposer.cs`.
