# 09 — Resource scope foundation

Task: TB-P06-T024  
Status: LIVE foundation  
Claim: `RESOURCE_SCOPED_AUTHORIZATION_FOUNDATION_LIVE`  
Not claimed: `ALL_RESOURCE_SCOPES_FULLY_INTEGRATED`

## Typed ScopeKind

`AccessScopeKind` in `AccessControlDomain.cs`:

| Kind | Status |
|------|--------|
| GlobalWithinOwner | LIVE |
| Category | LIVE (policy + SpiceDB `category#handle_orders`) |
| Product | Enum + catalog metadata; runtime module integration deferred |
| Brand | Enum + catalog metadata; deferred |
| Warehouse | Enum reserved; deferred |
| Store | Enum reserved; deferred |
| OrderSegment | Enum reserved; deferred |

## Grant shape

`RolePermission`: `PermissionId` + `ScopeKind` + optional `ScopeResourceId`  
No free-form SQL / policy expression strings.

## Catalog coupling

`PermissionDefinition.ScopeKinds` declares which kinds a permission may use  
(e.g. `order.handle` → GlobalWithinOwner \| Category).

## Sync behavior

- GlobalWithinOwner → `permission#granted` tuple
- Category (+ order.* subset) → `category#handler` tuple
- Other kinds: stored in PG for future projection; not fully wired into every module query
