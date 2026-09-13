# R20 browser network

- Cart load: `pendingFetches=1`, `cartFetches=2` (page session). No interval fetch.
- Countdown is local only.
- Guest cart create 500 (`PostgresException`) was missing reservation columns on `store_hold_policy_settings`; applying authored migrations fixed POST `/v1/storefront/cart` → 200.
- No repeated 401 on these guest/admin-dev paths.
- No new media 404 from pending thumbs (placeholder media IDs).
- Double-click retry: button `disabled` while busy.
- No preload/CSS hack added.
