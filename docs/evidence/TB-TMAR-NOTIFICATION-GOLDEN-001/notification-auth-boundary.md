# Notification Auth Boundary

## Customer
- Seam: `INotificationCustomerAuthorizer` in Endpoints
- Host adapter: `HostNotificationCustomerAuthorizer`
- Preserved behavior:
  - authenticated session → `UserId`
  - Development/Testing `X-Tooba-Dev-Actor-User-Id`
  - Development/Testing fallback → `StorefrontGuestActorId`
  - production unauthenticated → `null` → `customer.session.required` via `ApiResponseFactory`
- Endpoints/Application do not reference `CurrentAuthenticatedSession`, `StorefrontCheckoutComposer`, or Host namespaces

## Seller
- Seam: `INotificationSellerAuthorizer` in Endpoints
- Host adapter: `HostNotificationSellerAuthorizer` → `SellerPanelAccess.RequireAuthorizedAsync`
- CQRS receives `sellerPartyId` explicitly
- No Host reference from module; seller auth exceptions propagate to platform ProblemDetails (stable codes/status)

## Verdict
Auth boundary = Endpoints seams + Host security adapters only
