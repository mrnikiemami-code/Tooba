# file-dispositions — TB-TMAR-HOST-SECURITY-AMC-001

| File | Disposition | Rationale |
| --- | --- | --- |
| AuthSecurityHostOptions.cs | KEEP_AS_GLOBAL_HOST_SECURITY_PLATFORM | Host HTTP/auth options + validator; no module policy |
| SecurityHeadersMiddleware.cs | KEEP_AS_GLOBAL_HOST_SECURITY_PLATFORM | Host response security headers middleware |
| Checkout/CheckoutIdentityGate.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Catalog.Contracts policy + session; SemanticException code-based |
| Checkout/HostCheckoutActorPolicyAdapter.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Payment.Contracts port → gate |
| Payment/HostPaymentStorefrontAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Session→userId only; no titles |
| Seller/HostSellerPanelAccess.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Sole `ISellerPanelAccess` DI impl |
| Seller/SellerPanelAccess.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Internal helper; **hygiene debt** (FA PlatformHttp titles) |
| Seller/SellerSecurityErrorCodes.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Shared seller security machine codes |
| Seller/HostCatalogSellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Pass-through panel gate |
| Seller/HostNotificationSellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Pass-through |
| Seller/HostOfferSellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Pass-through |
| Seller/HostOrderSellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Maps PlatformHttp→SemanticError code (no title invent) |
| Seller/HostPartySellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Capability via IPlatformEffectiveAccessReader; **hygiene debt** FA title |
| Seller/HostPromotionSellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Pass-through |
| Seller/HostReturnSellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Pass-through |
| Seller/HostReviewsSellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Pass-through; missing from stale Security guard allowlist |
| Seller/HostSettlementSellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Pass-through |
| Seller/HostStorySellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Pass-through |
| Seller/HostSupportSellerAuthorizer.cs | KEEP_AS_THIN_HOST_SECURITY_ADAPTER | Capability; **hygiene debt** FA title |

Dead / MOVE_TO_MODULE / MUST_SPLIT / BLOCKED: **none** in this analyze.
