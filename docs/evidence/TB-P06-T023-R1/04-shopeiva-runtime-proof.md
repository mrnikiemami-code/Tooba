# 04 — Shopeiva runtime proof

- Original Shopeiva running at `http://127.0.0.1:3001` (HTTP 200)
- Comparable notification route: `http://127.0.0.1:3001/user-panel/notifications` → **200**
- Vendor dedicated notifications route: `http://127.0.0.1:3001/vendor-panel/notifications` → **404** (absent in source app). Vendor panel root `http://127.0.0.1:3001/vendor-panel` → **200** captured for runtime proof.
- Capture: `captures/03-shopeiva-user-notifications.png`, `captures/06-shopeiva-vendor-panel-root.png`
