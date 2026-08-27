# 02 — Shopeiva notification source map (TB-P06-T023)

Reference root: `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva\`

## Customer inbox (PRIMARY)

| Piece | Path |
|---|---|
| Component | `src/components/dashboard/notifications/notifications.jsx` |
| Page | `src/app/user-panel/notifications/page.jsx` |
| Sidebar Bell | `src/components/dashboard/sidebar/sidebar.jsx` → `/user-panel/notifications` |

### Visual/behavior contract (lock)

- Header: Bell icon + title «اعلان‌ها» + unread badge count
- Actions: mark-all-read, filter chips (all/unread/read/order/offer/ticket)
- Cards: typed icon/color, unread pulse/border, title+desc, date/time, mark-read + dismiss
- Empty state when filtered list empty
- Toast on mark-all / delete (Shopeiva uses react-toastify)
- Mock data in source — Tooba must replace with Host APIs only

## Storefront header dropdown

| Piece | Path |
|---|---|
| Header Bell dropdown | `src/components/common/Header/Header.jsx` |

**T023 scope:** bind panel inbox first; storefront header dropdown may stay unbound / non-fake (no mock unread). Prefer DEFER fake header list.

## Vendor

| Piece | Path | Notes |
|---|---|---|
| Settings toggles | `src/components/vendor/panel/settings/settings.jsx` | prefs only, not inbox |
| Vendor inbox route | **none** | Reuse customer `notifications.jsx` geometry under `/vendor-panel/notifications` |

## CSS/JS

- Tailwind utility classes on cards/filters (emerald/blue/amber/rose/…)
- lucide-react icons
- No separate CSS module — class strings are the lock
