# 15 — Fulfillment authorization (TB-P06-T009)

## Seller endpoints

- Guard: `SellerPanelAccess.RequireAuthorizedAsync(request, session, guard, environment, ct)`
- Returns `(actorUserId, sellerPartyId)`.
- All seller routes scoped: `GetForSellerAsync` returns null if `SellerPartyId` mismatch → 404.
- Mutations verify fulfillment exists for authorized seller before action.

## Admin endpoints

- Guard: `AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, ct)`
- Read-only: `GET /v1/admin/fulfillments`, `GET /v1/admin/fulfillments/{id}`

## Customer endpoint

- Session-based actor resolution; checkout ownership enforced in endpoint (not SpiceDB per-fulfillment).

## Use-case guard

- `IFulfillmentUseCaseGuard.EnsureCanMutateAsync` called on all directory mutations.
- Current implementation: `OpenFulfillmentUseCaseGuard` (no-op; HTTP layer enforces access).

## Error contract

- `PlatformHttpException` → JSON with `title`, `errorCode`, status from exception.
- Domain rejection → 400 `fulfillment.rejected`.
- Missing resource → 404 `fulfillment.missing`.
