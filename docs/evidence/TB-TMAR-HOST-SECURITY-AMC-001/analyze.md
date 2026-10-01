# analyze — TB-TMAR-HOST-SECURITY-AMC-001

## Mode

ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED  
Production code change: **ZERO**  
Implementation SHA unchanged: `7a0d79b407c618d503acd1ef9866c519b4e5f070`  
Host/Admin: **HOST_ADMIN_FULLY_CERTIFIED PRESERVED** (untouched)

## Exact tree (disk)

Recursive production `.cs` count: **19**

| Relative path | Namespace | Type(s) |
| --- | --- | --- |
| AuthSecurityHostOptions.cs | Tooba.Host.Security | AuthSecurityHostOptions, AuthSecurityOptionsValidator |
| SecurityHeadersMiddleware.cs | Tooba.Host.Security | SecurityHeadersMiddleware |
| Checkout/CheckoutIdentityGate.cs | Tooba.Host.Security.Checkout | CheckoutIdentityGate |
| Checkout/HostCheckoutActorPolicyAdapter.cs | Tooba.Host.Security.Checkout | HostCheckoutActorPolicyAdapter |
| Payment/HostPaymentStorefrontAuthorizer.cs | Tooba.Host.Security.Payment | HostPaymentStorefrontAuthorizer |
| Seller/SellerPanelAccess.cs | Tooba.Host.Security.Seller | SellerPanelAccess |
| Seller/SellerSecurityErrorCodes.cs | Tooba.Host.Security.Seller | SellerSecurityErrorCodes |
| Seller/HostSellerPanelAccess.cs | Tooba.Host.Security.Seller | HostSellerPanelAccess |
| Seller/HostCatalogSellerAuthorizer.cs | Tooba.Host.Security.Seller | HostCatalogSellerAuthorizer |
| Seller/HostNotificationSellerAuthorizer.cs | Tooba.Host.Security.Seller | HostNotificationSellerAuthorizer |
| Seller/HostOfferSellerAuthorizer.cs | Tooba.Host.Security.Seller | HostOfferSellerAuthorizer |
| Seller/HostOrderSellerAuthorizer.cs | Tooba.Host.Security.Seller | HostOrderSellerAuthorizer |
| Seller/HostPartySellerAuthorizer.cs | Tooba.Host.Security.Seller | HostPartySellerAuthorizer |
| Seller/HostPromotionSellerAuthorizer.cs | Tooba.Host.Security.Seller | HostPromotionSellerAuthorizer |
| Seller/HostReturnSellerAuthorizer.cs | Tooba.Host.Security.Seller | HostReturnSellerAuthorizer |
| Seller/HostReviewsSellerAuthorizer.cs | Tooba.Host.Security.Seller | HostReviewsSellerAuthorizer |
| Seller/HostSettlementSellerAuthorizer.cs | Tooba.Host.Security.Seller | HostSettlementSellerAuthorizer |
| Seller/HostStorySellerAuthorizer.cs | Tooba.Host.Security.Seller | HostStorySellerAuthorizer |
| Seller/HostSupportSellerAuthorizer.cs | Tooba.Host.Security.Seller | HostSupportSellerAuthorizer |

Top-level folders: `Checkout`, `Payment`, `Seller`  
Root flat files: 2 (justified Host HTTP platform)

## Headline findings

1. Folder remains intentional thin Host security platform (**not HOST_ZERO**).
2. **8** hard-coded Persian `PlatformHttpException` title sites on Seller core/capability adapters (must be hygiene-migrated later; not fixed in Analyze).
3. Historical SoT/guard claim **18 files** is **STALE** — live count is **19** (`HostReviewsSellerAuthorizer` present + DI-registered; missing from `HostSecurityAmcGuardTests` allowlist).
4. Foreign Application/Infrastructure/Domain/DbContext under Security: **ZERO**.
5. Dead Host Security adapters: **ZERO** (all seller/payment/checkout adapters have Program DI).
6. Recommended next: seller error/localization hygiene wave (W1), then guard/SoT reconcile + CERT — **not** blind keep-cert without hygiene.
