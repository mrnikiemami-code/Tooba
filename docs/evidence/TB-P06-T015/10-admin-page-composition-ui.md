# 10 — Admin page composition UI (TB-P06-T015)

| Item | Detail |
|---|---|
| Route | `/admin/page-composition` |
| Nav | Admin shell item `page-composition` / «ترکیب صفحهٔ خانه» (`live: true`) |
| Client API | `src/frontend/app/composition/composition-api.ts` |
| UI host | `admin-screens.tsx` (`data-testid="admin-page-composition"`) |
| Capabilities | list sections, reorder, hide/show, restore default; catalog-driven add |

## Captures

- `captures/03-admin-page-composition-desktop.png`
- `captures/06-admin-page-composition-hidden-state.png`
- `captures/08-admin-page-composition-mobile.png`

## Native-fit

Admin UI follows existing Admin DataGrid / panel patterns — not a free-form visual page builder canvas.
