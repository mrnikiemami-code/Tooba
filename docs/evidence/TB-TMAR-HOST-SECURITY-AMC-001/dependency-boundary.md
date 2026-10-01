# dependency-boundary — TB-TMAR-HOST-SECURITY-AMC-001

Scan of all 19 Security `.cs` files:

| Dependency class | State |
| --- | --- |
| Foreign Application | ZERO |
| Foreign Infrastructure | ZERO |
| Foreign Domain | ZERO |
| Foreign DbContext / persistence / joins | ZERO |
| RequestServices / service locator | ZERO |

## Allowed dependencies observed

| File | Dependency | Layer | Allowed? |
| --- | --- | --- | --- |
| CheckoutIdentityGate | Catalog.Contracts.Checkout | Contracts | YES |
| CheckoutIdentityGate | BuildingBlocks session/errors | BB | YES |
| HostCheckoutActorPolicyAdapter | Payment.Contracts.Ports | Contracts | YES |
| HostPaymentStorefrontAuthorizer | Payment.Endpoints.Storefront interface | Endpoints seam | YES |
| Host*SellerAuthorizer | module Endpoints.Seller interfaces | Endpoints seam | YES |
| HostSellerPanelAccess / SellerPanelAccess | BuildingBlocks Security / Authorization | BB | YES |
| HostParty/Support seller | IPlatformEffectiveAccessReader | BB seam | YES |

## Business authority

Host Security owns HTTP middleware/options, session/tenant/seller panel composition, thin auth adapters.  
**No** business commands, domain rules, persistence, or module workflow ownership found.
