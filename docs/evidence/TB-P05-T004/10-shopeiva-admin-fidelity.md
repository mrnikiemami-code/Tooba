# TB-P05-T004 — Shopeiva Admin fidelity

Canonical sources: Shopeiva Vendor Panel and the accepted T001 adaptation in `docs/evidence/TB-P05-T001/repair/shopeiva-vendor-fidelity.md`.

Approved adaptation: Shopeiva primary red → Tooba blue; gold topbar, horizontal navigation, cards, toolbars, density, and responsive drawer remain recognizable.

| Surface | Shopeiva source pattern | Tooba route | Structure preserved | Data binding replaced | Minimal deviation / reason |
| --- | --- | --- | --- | --- | --- |
| Shell | Vendor contact sheet / T001 shell | `/admin/*` layout | gold topbar, identity badge, horizontal nav, mobile drawer | Admin dev actor and active route | Admin labels replace seller labels |
| Dashboard | Vendor dashboard | `/admin` | KPI cards, quick navigation, section-card rhythm | live product, offer, order, seller and customer counts | charts omitted because reliable analytics do not exist |
| Products | Vendor products list | `/admin/products` | page header, toolbar/card container | Tooba Product composition | approved Data Grid inside Shopeiva card; no Product.Price/Stock |
| Product detail | Existing Tooba functional workspace under vendor shell | `/admin/products/[productId]` | shell and operational card language | authenticated Product Workspace API | T005 interaction retained only as functional work |
| Orders | Vendor orders list | `/admin/orders` | summary cards, toolbar, status chips | live Admin order composition | Data Grid supplies sorting/filtering/pagination |
| Order detail | Vendor order detail | `/admin/orders/[checkoutId]` | back link, header card, metadata, lines, shipping/customer blocks | live checkout and seller-order snapshots | no unsupported timeline invented |
| Sellers | Vendor customers-list chrome | `/admin/sellers` | stats/header, card, searchable grid | Party + Offer + Order composition | Shopeiva has no separate seller-admin page |
| Customers | Vendor customers list | `/admin/customers` | stats/header, card, customer rows | checkout-derived customer activity | no CRM fields invented |
| Authorization denied | Vendor panel error card | Admin routes | contained operator error state | Host 401/403 | exposes no internal authorization details |
| Mobile | Vendor mobile drawer and stacked cards | core Admin routes at 390×844 | hamburger, drawer, reachable nav/actions | same live APIs | Data Grid narrow mode replaces squeezed desktop table |

Acceptance principle:

```text
If Shopeiva has the pattern,
do not invent a replacement.
```

