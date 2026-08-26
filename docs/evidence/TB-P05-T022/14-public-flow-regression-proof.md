# 14 — Public Flow Regression Proof

```
cd src/frontend
npm run test:critical-storefront
```

Expected/actual: **PASS** (Home / PDP / Listing guards).

Customer-panel edits do not touch:

- `storefront-home` / home guards
- `storefront-pdp` / pdp guards
- `storefront-listing` / listing guards
- `storefront-cart` / `storefront-checkout` (T021)

Touched: `customer-panel/*` shell, dashboard, tickets/settings pages, capability shell.
