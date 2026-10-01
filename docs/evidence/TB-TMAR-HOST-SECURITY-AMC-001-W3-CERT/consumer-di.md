# consumer-di — TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT

## Active DI (Program.cs)

| Registration | Implementation | Role |
| --- | --- | --- |
| ISellerPanelAccess | HostSellerPanelAccess | Sole panel DI authority |
| ICheckoutActorPolicyPort (Payment.Contracts) | HostCheckoutActorPolicyAdapter | Thin checkout actor |
| IPaymentStorefrontAuthorizer | HostPaymentStorefrontAuthorizer | Thin payment storefront |
| ICatalogSellerAuthorizer | HostCatalogSellerAuthorizer | Pass-through |
| INotificationSellerAuthorizer | HostNotificationSellerAuthorizer | Pass-through |
| IOfferSellerAuthorizer | HostOfferSellerAuthorizer | Pass-through |
| IOrderSellerAuthorizer | HostOrderSellerAuthorizer | SemanticException → SemanticError |
| IPartySellerAuthorizer | HostPartySellerAuthorizer | Capability |
| IPromotionSellerAuthorizer | HostPromotionSellerAuthorizer | Pass-through |
| IReturnSellerAuthorizer | HostReturnSellerAuthorizer | Pass-through |
| IReviewsSellerAuthorizer | HostReviewsSellerAuthorizer | Pass-through (active) |
| ISettlementSellerAuthorizer | HostSettlementSellerAuthorizer | Pass-through |
| IStorySellerAuthorizer | HostStorySellerAuthorizer | Pass-through |
| ISupportSellerAuthorizer | HostSupportSellerAuthorizer | Capability |

Active adapter/DI count (panel + checkout + payment + 11 seller authorizers) = **14**.

- SellerPanelAccess: helper only (not duplicate DI)
- Dead / unregistered orphan adapters: **0**
- AuthSecurityHostOptions + SecurityHeadersMiddleware: platform (options + middleware pipeline)
