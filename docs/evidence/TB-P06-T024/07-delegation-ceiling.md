# 07 — Delegation ceiling

Task: TB-P06-T024  
Status: LIVE

## Model

`PlatformSellerCeiling` ∩ Seller-granted permissions  
Entity: `AccessControlDomain.PlatformSellerCeiling`  
Ops: `AccessControlDirectory.SetSellerCeilingAsync` / effective intersect in `GetEffectiveAccessAsync`

## Enforcement

Seller `SetRolePermissionsAsync` → `ValidateGrantsForOwnerAsync`:

1. Permission must exist in catalog
2. Seller: must be `Delegable` (else platform escalation)
3. Seller: must be Enabled on that Seller’s ceiling
4. Stale grants outside ceiling drop from effective access after ceiling revoke + sync

Admin sets ceiling via  
`PUT /v1/admin/sellers/{sellerId}/access-control/ceiling`

Seller reads via  
`GET /v1/seller/access-control/ceiling`

## Proven in tests

`AccessControlFoundationTests.Ceiling_escalation_and_category_scope_policy`:

| Case | Result |
|------|--------|
| Ceiling without `order.handle` → grant attempt | Deny (`ceiling`) |
| Grant `admin.dashboard.view` | Deny (`platform_permission`) |
| Ceiling enables `order.view`/`order.handle` → grant + assign | Allow |
| Ceiling revoke | Effective no longer contains `order.handle` |

UI: Seller matrix shows `disabledByCeiling` badge; select-all skips blocked rows.
