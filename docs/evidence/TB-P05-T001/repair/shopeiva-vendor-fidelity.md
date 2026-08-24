# TB-P05-T001 REPAIR — Shopeiva vendor fidelity

Canonical reference: `docs/evidence/TB-P04-T006/visual-atlas/02-vendor-contact-sheet.png` and `visual-atlas/vendor/B01…B06`.

Approved adaptation: Shopeiva primary red → Tooba blue (`--color-primary`).

| Surface | Shopeiva source | Tooba route | Preserved | Minimal adaptation | Deviation reason |
| --- | --- | --- | --- | --- | --- |
| Dashboard | B01 | `/vendor-panel` | gold topbar, KPI cards, panel density, breadcrumb | blue accent; real counts only | no fake charts/goals |
| Shell | contact sheet header/nav | layout | gold bar, seller badge, horizontal nav | blue active pills | no Shopeiva red CTA |
| Products | B02 | `/vendor-panel/products` | toolbar+card container | Tooba Data Grid inside card | demo export buttons still present on grid (secondary) |
| Product detail | B04 | `/vendor-panel/products/[offerId]` | card sections, form controls | Catalog RO / Offer seam | price/stock display read-only from Offer/Inventory |
| Orders | B05 | `/vendor-panel/orders` | panel/toolbar language | Data Grid | Persian payment chips |
| Order detail | B06 | `/vendor-panel/orders/[id]` | cards + line list | seller-owned lines only | — |
| Mobile | M05 / 390×844 | products/orders | hamburger + stacked cards | overflow-x on grid | intentional responsive; not squeezed desktop table only |

Screenshots in this folder: `01`–`10`.
