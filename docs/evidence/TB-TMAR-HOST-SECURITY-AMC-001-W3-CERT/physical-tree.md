# physical-tree — TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT

Independent disk re-enumeration of `src/backend/Host/Tooba.Host/Security/`.

| Scope | Exact `.cs` count | Files |
| --- | --- | --- |
| Root | 2 | AuthSecurityHostOptions.cs, SecurityHeadersMiddleware.cs |
| Checkout/ | 2 | CheckoutIdentityGate.cs, HostCheckoutActorPolicyAdapter.cs |
| Payment/ | 1 | HostPaymentStorefrontAuthorizer.cs |
| Seller/ | 14 | SellerPanelAccess, SellerSecurityErrorCodes, HostSellerPanelAccess, HostCatalog/Notification/Offer/Order/Party/Promotion/Return/Reviews/Settlement/Story/SupportSellerAuthorizer |
| Whole Security | **19** | — |

Subfolders exact: Checkout, Payment, Seller only.

- HostReviewsSellerAuthorizer.cs: PRESENT
- Stale/duplicate physical copies: ZERO
- TypeForwardedTo / alias shims: ZERO
- Path ↔ namespace: EXACT (`Tooba.Host.Security[.Checkout|.Payment|.Seller]`)
