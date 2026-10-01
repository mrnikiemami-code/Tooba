# seller-failure-semantics — TB-TMAR-HOST-SECURITY-AMC-001-W1

## Change

All expected Seller panel / capability failures now throw `SemanticException(SemanticError(stableCode))`.

| Code | HTTP (catalog) | Sites after W1 |
| --- | --- | --- |
| seller.actor.missing | 401 (Order) | SellerPanelAccess ResolveActor + empty actor/seller |
| seller.identity.missing | 400 (Order) | SellerPanelAccess RequireSellerPartyId |
| seller.authorization.unavailable | 503 (Order) | edition missing + Unavailable decision |
| seller.authorization.denied | 403 (Foundation) | deny decision; Party/Support capability miss |

Hard-coded PlatformHttpException titles removed: **8 → 0**.

## Behavior preserved

- Session actor wins; DevActorHeader Development-only
- SellerPartyHeader = context only
- Allow / Unavailable / Deny fail-closed unchanged
