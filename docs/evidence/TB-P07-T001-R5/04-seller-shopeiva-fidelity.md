# TB-P07-T001-R5 — Seller Panel Shopeiva visual fidelity

UI/UX only. Accent: Shopeiva `#E53935` → Tooba `#2563EB`. Geometry/CSS/motion otherwise matched from Original Shopeiva vendor panel.

## Source mapping

| Tooba | Shopeiva reference |
| --- | --- |
| `vendor-shell.tsx` | `src/app/(vendor)/vendor-panel/layout.jsx` |
| `vendor-panel/page.tsx` | `src/components/vendor/panel/dashboard/dashboard.jsx` |
| `analytics/page.tsx` | `src/components/vendor/panel/analytics/analytics.jsx` |
| `settings/page.tsx` | `src/components/vendor/panel/settings/settings.jsx` |
| products/orders list headers | `productsList.jsx` / `ordersList.jsx` |

## Matched (geometry / chrome)

### Shell (`vendor-shell.tsx`)
- Header height `65px`, Store badge, title **«پنل فروشنده»** (not seller name as primary).
- Award row **«فروشنده ویژه»** (md+).
- Seller label + logout; Tooba Actor/Seller context select retained (dev Host boundary).
- Sidebar: `transition-all duration-300 ease-in-out`, open `w-64 translate-x-0`, closed `w-0 -translate-x-full opacity-0`.
- Mobile drawer: backdrop blur, `slide-in-from-right duration-300`, footer logout.
- Loading: Shopeiva-like skeleton (pulse cards/nav) instead of plain text.
- Nav order closer to Shopeiva core (dashboard → products → orders → analytics → coupons → reviews → wallet → tickets), then Tooba-only live routes, then **کنترل دسترسی** immediately before **تنظیمات**.
- Deferred still: customers, gift-cards (deep-link only).

### Dashboard
- Welcome gradient band + Hand + Award chip (Shopeiva layout; blue accent).
- 4 KPI card grid (`grid-cols-2 md:grid-cols-4`, icon gradient, hover scale).
- Chart + target row (`lg:grid-cols-3`).
- Product lists row, search keywords shell, recent orders table, quick actions `grid-cols-3 md:grid-cols-6`.

### Analytics
- Icon header, period dropdown chrome, 4 KPI tiles, 2×2 chart shells.

### Settings
- Card + `from-[#2563EB]/5` gradient header + tab strip (store live; other tabs disabled chrome only).

### Access Control
- Wrapped in Vendor settings-like card/gradient chrome; AC center uses `div` shell to avoid nested `main`.

### Products / Orders lists
- Shopeiva list headers: icon tile + «مدیریت محصولات/سفارشات» + counts; CTA blue accent; white `rounded-2xl` list card.

### Motion
- `globals.css`: `animate-in slide-in-from-right` (+ duration utilities) for vendor/admin drawers.

## Host bindings (honest)

| Surface | Host data | Empty / zero shell |
| --- | --- | --- |
| KPI سفارشات | `openOrders + paidOrders` | — |
| KPI محصولات | `activeOffers` | — |
| KPI کل فروش | none | `—` (no invented revenue or %) |
| KPI مشتریان | none | `۰` |
| Change % on KPIs | none | always `—` (no invented sales %) |
| Chart / target / conversion / satisfaction | none | empty bars / 0% bar / `—` |
| محصولات پرفروش | Offer list titles/price/stock | no sales rank or fake revenue trend |
| محصولات پربازدید / پرسرچ | none | explicit empty copy |
| سفارشات اخیر | `loadSellerOrders` | empty table message |
| Quick actions | live routes only | no customers link |

## Intentional unauthorized differences

1. **Color** — approved blue `#2563EB` instead of Shopeiva red.
2. **No invented metrics** — Shopeiva demo uses fake sales %, charts, targets; Tooba shows empty shells.
3. **Nav extras** — notifications, stories, fulfillments, returns, access-control (Tooba live); customers/gift-cards deferred.
4. **Dev context select** — Actor+Seller picker required for Host authz; not in Shopeiva.
5. **Settings tabs** — only store tab wired; profile/notifications/appearance are disabled chrome (no fake toggles).
6. **Export on analytics** — disabled until Host export exists.
7. **Products CTA** — «محصول جدید» → `/products/new` (Offer create), not canonical Catalog Product invent.

## Out of scope this evidence file

Admin polish, screenshot pack, commit/push, Bridge Result — handled by parent Worker turn / other evidence files.
