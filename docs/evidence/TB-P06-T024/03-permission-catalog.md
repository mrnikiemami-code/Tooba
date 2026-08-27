# 03 — Permission Catalog

Task: TB-P06-T024  
Status: LIVE

## Source

`src/backend/Modules/AccessControl/Tooba.AccessControl.Application/PermissionCatalog.cs`

## Claims

| Field | Value |
|-------|-------|
| Count | 43 canonical `PermissionDefinition` entries |
| Shape | `PermissionId`, `Module`, `DisplayNameKey`, `DescriptionKey`, `Delegable`, `ScopeKinds` |
| IDs | Semantic (e.g. `order.handle`), not endpoint names |
| Lookup | `Find` / `Require` / `IsDelegable` |

## Modules (representative)

Admin, Product, Order, Seller, Payment, Promotion, Review, Story, Content, PageComposition, Fulfillment, Return, Refund, Settlement, AccessControl

## ScopeKinds per permission

- Default: `GlobalWithinOwner` only
- Order view/handle/detail/fulfill/cancel/refund: + `Category`
- Product view/edit/publish: + `Product`, `Brand`, `Category`

## Delegable vs platform-only

- Non-delegable examples: `admin.dashboard.view`, `seller.approve`, `payment.reconcile`, `story.approve`
- Delegable examples: `order.*` (most), `product.*`, `accesscontrol.view|manage`, `fulfillment.*`

## API surface

`GET /v1/admin/access-control/permissions`  
`GET /v1/seller/access-control/permissions` (ceiling-aware flags)
