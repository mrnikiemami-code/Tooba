# Dashboard audit — TB-P11-T001

`AdminDashboardScreen` (`admin-screens.tsx`) + `AdminPanelComposer.GetDashboardAsync`.

- Metrics are live Host counts (published products, active offers, seller-order statuses, distinct sellers, distinct checkout users). Copy forbids fabricated revenue charts.
- No date ranges / time-series.
- Permission: nav can hide via `admin.dashboard.view`; API still `tenant#view`. Deep-link `/admin` serves metrics to any panel actor. No per-tile capability filtering.
- Loading: ellipsis `…` until fetch; error/denied handled; zeros render as ۰.
- Performance: loads all `SellerOrders.Status` then counts in CLR (not SQL GROUP BY).
- Marketplace tiles (فروشنده) always shown; no Single-Store hide.
- Runtime screenshot `admin-dashboard-current.png` shows operational cards with pending `…` values during first paint — matches weak loading UX.

No new dashboard built in T001.
