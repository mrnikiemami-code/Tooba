# TB-P07-T001-R5 — Motion / mobile

## Motion (Shopeiva-native, light)

- **Admin shell** (`admin-shell.tsx`): sticky header + sidebar; mobile drawer uses `animate-in slide-in-from-right` with `transition-all duration-300`; `prefers-reduced-motion` respected.
- **Vendor shell** (`vendor-shell.tsx`): Shopeiva-matched sidebar collapse (`w-64` ↔ `w-0`), mobile backdrop blur drawer, loading skeleton pulse — blue accent `#2563EB` instead of Shopeiva red.
- **DataGrid** (`DataGrid.tsx`): filter drawer slides in; localized «فیلترها» operators; toolbar controls stack on narrow widths (see mobile products pack).
- **Product workspace**: section tabs (including «رسانه») use WorkspaceShell transitions; gallery cards elevated surfaces.

## Mobile audit (390×844)

Observed in pack (`screenshots/mobile/`):

| Surface | File | Notes |
| --- | --- | --- |
| Admin products | `admin-products.png` | Hamburger opens drawer; «محصول جدید» full-width; filters/columns/export stack vertically; card-style rows |
| Admin access-control | `admin-access-control.png` | User picker + role sections stack; no horizontal chrome scroll |
| Vendor dashboard | `vendor-dashboard.png` | Welcome gradient + KPI grid `grid-cols-2`; sidebar hidden until menu tap |
| Vendor products | `vendor-products.png` | Shopeiva list header + offer cards; CTA reachable |

Desktop packs: `screenshots/admin/`, `screenshots/seller/`, `screenshots/shopeiva/` — full index in `10-screenshot-pack-index.md`.
