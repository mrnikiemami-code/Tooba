# consumers-di — TB-TMAR-HOST-SECURITY-AMC-001

## Global

| Type | Interface / role | DI | Production consumers |
| --- | --- | --- | --- |
| AuthSecurityHostOptions | Host options | `AddOptions`+Bind+`IValidateOptions` | Program body-size/CORS/rate-limit; SecurityHeadersMiddleware; AuthenticationRateLimitThrottleSeam |
| SecurityHeadersMiddleware | middleware | `app.UseMiddleware` | HTTP pipeline |

## Checkout / Payment

| Type | Interface | DI | Module seam | Consumers |
| --- | --- | --- | --- | --- |
| CheckoutIdentityGate | concrete | factory in Program | Catalog.Contracts checkout policy | HostCheckoutActorPolicyAdapter; HostOrderStorefrontCheckoutIdentityGate (Order Host wrapper) |
| HostCheckoutActorPolicyAdapter | ICheckoutActorPolicyPort | AddScoped | Payment.Contracts.Ports | Payment orchestration |
| HostPaymentStorefrontAuthorizer | IPaymentStorefrontAuthorizer | AddScoped | Payment.Endpoints.Storefront | Payment storefront endpoints |

## Seller matrix

| File | Interface | DI | Module seam | Pass-through vs capability | Codes | Hard-coded text | Disposition |
| --- | --- | --- | --- | --- | --- | --- | --- |
| HostSellerPanelAccess | ISellerPanelAccess | **sole** AddScoped | BuildingBlocks | panel composition | via helper | no (delegates) | KEEP_THIN |
| SellerPanelAccess | helper (no DI) | none | BuildingBlocks auth | panel authority helper | seller.actor/identity/authorization.* | YES (6 sites) | KEEP_THIN |
| SellerSecurityErrorCodes | constants | none | Host Security | machine codes | 4 codes | XML docs only | KEEP_THIN |
| HostCatalogSellerAuthorizer | ICatalogSellerAuthorizer | AddScoped | Catalog.Endpoints.Seller | pass-through | inherits | no | KEEP_THIN |
| HostNotificationSellerAuthorizer | INotificationSellerAuthorizer | AddScoped | Notification.Endpoints.Seller | pass-through | inherits | no | KEEP_THIN |
| HostOfferSellerAuthorizer | IOfferSellerAuthorizer | AddScoped | Offer.Endpoints.Seller | pass-through | inherits | no | KEEP_THIN |
| HostOrderSellerAuthorizer | IOrderSellerAuthorizer | AddScoped | Order.Endpoints.Seller | pass-through→SemanticError | inherits codes | no title invent | KEEP_THIN |
| HostPartySellerAuthorizer | IPartySellerAuthorizer | AddScoped | Party.Endpoints.Seller | capability | seller.authorization.denied | YES (1) | KEEP_THIN |
| HostPromotionSellerAuthorizer | IPromotionSellerAuthorizer | AddScoped | Promotion.Endpoints.Seller | pass-through | inherits | no | KEEP_THIN |
| HostReturnSellerAuthorizer | IReturnSellerAuthorizer | AddScoped | Returns.Endpoints.Seller | pass-through | inherits | no | KEEP_THIN |
| HostReviewsSellerAuthorizer | IReviewsSellerAuthorizer | AddScoped | Reviews.Endpoints.Seller | pass-through | inherits | no | KEEP_THIN |
| HostSettlementSellerAuthorizer | ISettlementSellerAuthorizer | AddScoped | Settlement.Endpoints.Seller | pass-through | inherits | no | KEEP_THIN |
| HostStorySellerAuthorizer | IStorySellerAuthorizer | AddScoped | Story.Endpoints.Seller | pass-through | inherits | no | KEEP_THIN |
| HostSupportSellerAuthorizer | ISupportSellerAuthorizer | AddScoped | Support.Endpoints.Seller | capability | seller.authorization.denied | YES (1) | KEEP_THIN |

Canonical panel seam: **HostSellerPanelAccess** is the single `ISellerPanelAccess` DI registration. SellerPanelAccess is an implementation helper, not a duplicate DI authority.

Active registered edge adapters (seller authorizers + payment + checkout actor + panel DI): **14**  
Dead Host Security adapters: **0**

## Test-only references

HostSecurityAmcGuardTests, HostSellerAmc*GuardTests, SellerPanelAuthorizationTests, SettingsFoundationTests, ReviewsFoundationTests — not production consumers.
