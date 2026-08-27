# 17 — Panel boundary proof (TB-P06-T018)

## Summary

Wave 1 is **frontend-only**. No Host module, no migration, no cross-module SQL.

| Boundary rule | Wave 1 compliance |
|---|---|
| No cross-module SQL JOIN | N/A — no SQL changes |
| No foreign module tables/repos | N/A — no backend changes in Wave 1 scope |
| Contracts / gateways / interfaces | Existing customer profile + seller dashboard APIs reused |
| Events / projections | Not introduced |
| New Host APIs | **None** |

## Frontend ownership

| Change area | Module boundary |
|---|---|
| Customer panel shell / settings / dashboard actions | `src/frontend/app/customer-panel/*` |
| Vendor panel shell / settings / dashboard actions | `src/frontend/app/vendor-panel/*` |
| Admin shell nav | `src/frontend/app/admin/*` |
| Evidence pack | `docs/evidence/TB-P06-T018/*` |

## Reused contracts only

- Customer profile: existing `/v1/customer/profile`
- Seller operational read: existing seller dashboard API
- Locale cookie: existing storefront locale preference foundation

## Deferred foundations (would require Host later)

- Notifications module  
- Support/Tickets module  
- Admin settings module  
- Customer wallet / gift-cards  
- Seller coupons / reviews / customers / business profile edit  

Those are explicitly **not** opened in Wave 1, preserving module boundaries.
