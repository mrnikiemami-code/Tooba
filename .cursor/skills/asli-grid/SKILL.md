---
name: asli-grid
description: >-
  Promote Admin AppDataGrid saved view named «اصلی» into code-owned grid defaults,
  then delete that view. Invoke when user says «اصلی‌گرید», «asli-grid», or
  «نمای اصلی را پیش‌فرض کن» and names a list page (e.g. /admin/landing-pages).
---

# اصلی‌گرید (asli-grid)

## When user invokes

User says **اصلی‌گرید** (or `asli-grid`) and points to an Admin list URL/grid.
They have already created a saved view named exactly **اصلی** on that grid.

## Do this only — no inventing

1. Resolve the grid preference key (e.g. `grid.admin.landing-pages` from `ADMIN_*_GRID_VIEW_KEY` / `saved-view-store.ts`).
2. Read `user_preference.ui_preferences` for that key; find the view with `"name":"اصلی"`.
3. Extract from that view only:
   - `layout.order` → column definition order in the screen
   - `layout.widths` → column `width` values
   - `layout.visibility` → hide columns that are false (if any)
   - `sorts` → `defaultQuery.sorts` on `AppDataGrid`
   - `pageSize` → `defaultQuery.pageSize`
4. Apply those as **code defaults** on the list screen (column defs + `defaultQuery`). Do not invent widths/order/sort.
5. Delete the «اصلی» view from the preference payload (`views` without that id; clear `defaultViewId` if it pointed at it). Persist via DB update or admin UI-preferences API.
6. Confirm briefly: what was applied + that «اصلی» was removed.

## Forbidden

- Leaving «اصلی» as a saved user view after promotion
- Changing unrelated grids
- Inventing layout not present in «اصلی»
---
