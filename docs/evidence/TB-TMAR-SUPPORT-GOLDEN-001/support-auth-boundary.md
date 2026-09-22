# Support Auth Boundary

## Endpoint interfaces (neutral)
- `ISupportCustomerAuthorizer.TryResolveActor` → `Guid?`
- `ISupportSellerAuthorizer.RequireAuthorizedAsync(ctx, permissionId, ct)` → `(ActorUserId, SellerPartyId)`
- `ISupportAdminAuthorizer.RequireAuthorizedAsync(ctx, permissionId, ct)` → `Guid`

## Host adapters (pure auth plumbing)
| Adapter | Path |
|---------|------|
| HostSupportCustomerAuthorizer | `Host/Customer/HostSupportCustomerAuthorizer.cs` |
| HostSupportSellerAuthorizer | `Host/Seller/HostSupportSellerAuthorizer.cs` |
| HostSupportAdminAuthorizer | `Host/Admin/HostSupportAdminAuthorizer.cs` |

## Capabilities preserved
- Seller: `support.view`, `support.create`, `support.reply`
- Admin: `support.view`, `support.manage`
- Admin **Unavailable fail-open** preserved (compatibility comment kept)

## Boundaries
- Endpoints do not reference AccessControl.Application/Domain
- Authorizers do not call `ISupportDirectory` / DbContext / business logic

## Verdict
**Support-Auth-Boundary: ENDPOINT_INTERFACES + HOST_ADAPTERS**
