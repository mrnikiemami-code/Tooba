# TB-P09-T021-R1 — Customer tracking discovery

## Root cause
`GET /v1/customer/orders/{checkoutId}/fulfillments` previously required `PlacedByUserId == actor`. Guest storefront checkouts are not owned by an authenticated actor; smoke without guest proof returned 404 even though package tracking preference existed.

## Supported access (reused)
- Authenticated session / Dev actor with matching `PlacedByUserId`
- Dev/Testing seam: `StorefrontGuestActorId` when no actor header (existing storefront convention)
- Guest cart credential: `X-Tooba-Guest-Secret` validated via `ICartQueryGateway.GetCartAsync(checkout.CartId, CartAccess(guestSecret))`

## Projection
`FulfillmentPanelComposer.SelectPreferredCustomerPackage` prefers active Created/Dispatched/Delivered packages (never Cancelled). Endpoint returns preferred reference/number/status alongside fulfillments.
