# TB-P07-T001-R3 — Motion / mobile

## Motion (Shopeiva-native, light)

- Admin shell sidebar / mobile drawer: CSS transitions with `prefers-reduced-motion` respect in `admin-shell.tsx`.
- DataGrid drawer/filter operators: design-system FilterControl localized; row hover remains subtle via existing DS styles.
- Product list / workspace: opacity transitions on primary actions; gallery cards use elevated surfaces.

## Mobile audit (Product / Order / Access Control)

Observed in pack (`screenshots/23-mobile-products.png`, `screenshots/24-mobile-access-control.png` at **390×844**):

- Product grid: hamburger opens drawer; primary «محصول جدید» full-width; filters/columns/export stack vertically; density + saved-view controls remain reachable without horizontal page scroll of the chrome.
- Orders grid: same DataGrid foundation with enum filters and saved views (desktop pack `05-orders.png`).
- Access Control center: searchable user picker (no GUID primary), role members, users tab — stackable sections for mobile.

Desktop pack + index: `screenshots/` + `22-admin-screenshot-pack-index.md`.
