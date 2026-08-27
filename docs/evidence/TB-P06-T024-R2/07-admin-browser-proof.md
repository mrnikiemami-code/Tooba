# 07 — Admin browser proof

Task: TB-P06-T024-R2

## Captures

| File | URL |
|------|-----|
| `captures/01-admin-access-control.png` | `http://127.0.0.1:3000/admin/access-control` |
| `captures/02-admin-seller-access-control.png` | `http://127.0.0.1:3000/admin/sellers/01a030d1-40cb-7000-8abe-6d31739956c5/access-control` |

## Observed

- Access Control Center hydrated (Roles / Users tabs, permission matrix, scope type controls).
- Admin Seller ACC opens concrete seller فروشگاه آرمان (no placeholder IDs).
- Script: `scripts/capture-t024-r2-browser-proof.mjs` + `browser-proof.json`.
