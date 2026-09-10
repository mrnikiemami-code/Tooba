# R1 Constraint Failure — Runtime

Architecture note: Storefront eligibility is **Store-enabled methods for the whole cart** (not a separate per-SellerParty method catalog). Seller-specific readiness already affects earliest delivery via `max(prep)`. Supported failure path for “method unavailable on this checkout”:

1. Multi-seller cart active with tipax present in projection
2. Admin `POST /v1/admin/shipping-services/{tipaxId}/deactivate` (supported config)
3. Projection omits all `tipax*` methods for that multi-seller cart
4. Forged selection `tipax:express` → **400** `shipping.method.unavailable`
5. Restore via Admin `PUT` `isActive=true` — tipax returns to projection

| Field | Value |
|---|---|
| tipax service id | `3dc3d04d-0d74-4475-96fd-85a168fd01da` |
| After deactivate codes | `post:express,post:standard,snapp_courier,store_courier,in_person` |
| Reject code | `shipping.method.unavailable` |
| Restored | tipax codes present again |

No production-data corruption; tipax restored after proof.

Raw step: `constraint-failure` in `r1-runtime-raw.json`.
