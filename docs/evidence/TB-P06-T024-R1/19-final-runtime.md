# 19 — Final runtime

Task: TB-P06-T024-R1

## Services (keep running)

| Process | Port | Notes |
|---------|------|-------|
| Tooba Host | 5088 | Restart after backend migration/code changes |
| Tooba Frontend | 3000 | Next.js dev |
| Original Shopeiva | 3001 | Visual reference |
| Bridge | 17321 | Pipeline transport (not required for local UI proof) |

## Health checks

```text
GET http://localhost:5088/health/live   → 200
GET http://localhost:5088/health/ready → 200
```

Run after Host restart.

## Runtime verification checklist

- [ ] Admin `/admin/access-control` — Category ScopeEditor loads real categories
- [ ] Admin `/admin/sellers/{id}/access-control` — ceiling Category scope
- [ ] Seller `/vendor-panel/access-control` — role + assignment + effective preview with Persian category name
- [ ] Scoped employee Seller Orders — Mobile visible, Books hidden
- [ ] Books order detail URL — denied
- [ ] Mobile fulfillment action — allowed (if order fully Mobile)
- [ ] Nav items hide when `*.view` absent (test with restricted dev actor)

## USER-PREVIEW URLs (after validation green)

| Surface | URL |
|---------|-----|
| Admin Access Control | `http://localhost:3000/admin/access-control` |
| Admin Seller Access | `http://localhost:3000/admin/sellers/{sellerId}/access-control` |
| Seller Access Control | `http://localhost:3000/vendor-panel/access-control` |
| Seller Roles | ACC → Roles tab (same page) |
| Seller Users | ACC → Users tab |
| Restricted Seller Orders | `http://localhost:3000/vendor-panel/orders` |
| Mobile Order detail | `http://localhost:3000/vendor-panel/orders/{orderId}` |
| Shopeiva reference | `http://localhost:3001` (vendor/settings patterns) |

## Browser proof status

Screenshots **not** attached — proof via integration tests + UI wiring documented in `15-browser-proof.md`. Live capture deferred to this runtime session.

## Migrations required

Apply on Host startup / migration runner:

- `20260827180000_AddOrderLineCategoryIdSnapshot` (Order)
- `20260827181000_AddSellerCeilingScope` (AccessControl)

## Runtime log placeholder

If runtime checks executed during Worker session, append timestamp + result here or add `runtime-check.log` alongside this file.

**Status at evidence authoring:** runtime browser verification **PENDING** — Host/FE assumed available per dev workflow.
