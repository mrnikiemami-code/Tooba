# authorization-semantics — TB-TMAR-HOST-SECURITY-AMC-001

## Seller panel (`SellerPanelAccess` / `HostSellerPanelAccess`)

| Branch | Current behavior |
| --- | --- |
| Actor resolve | Session Bearer wins; else Dev header `X-Tooba-Dev-Actor-User-Id` in Development only |
| Seller identity | Header `X-Tooba-Seller-Party-Id` Guid required |
| Allow | AuthorizationDecisionKind.Allow → return (actor, sellerParty) |
| Unavailable | 503 + `seller.authorization.unavailable` via PlatformHttpException + FA title |
| Deny | 403 + `seller.authorization.denied` via PlatformHttpException + FA title |
| Actor missing | 401 + `seller.actor.missing` + FA title |
| Identity missing | 400 + `seller.identity.missing` + FA title |

Fail-open: **none** observed on deny/unavailable.

## Pass-through seller authorizers

Delegate solely to `ISellerPanelAccess.RequireAuthorizedAsync` — inherit panel semantics.

## Capability sellers (Party / Support)

Panel gate first (`ISellerPanelAccess`), then `IPlatformEffectiveAccessReader` effective permissions.

| Adapter | Capability behavior | Deny | Unavailable |
| --- | --- | --- | --- |
| HostPartySellerAuthorizer | RequireView: `seller.settings.view` mandatory; `seller.settings.manage` optional → CanManage bool | 403 + code + FA title | inherits panel |
| HostPartySellerAuthorizer | RequireManage: manage permission mandatory | 403 + code + FA title | inherits panel |
| HostSupportSellerAuthorizer | RequireAuthorizedAsync(permissionId): panel + exact permission grant (GlobalWithinOwner, not DeniedByCeiling) | 403 + code + FA title | inherits panel |

Fail-open on deny: **none**.

## Checkout

GuestAllowed → allow; else authenticated session required; else SemanticException `checkout.authentication_required` (Foundation). No PlatformHttp titles.

## Payment storefront authorizer

Returns authenticated user id or null — no throw titles.
