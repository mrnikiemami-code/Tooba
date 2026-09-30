# Repair — Security AMC-001-R1

1. Moved ICheckoutActorPolicyPort to Payment.Contracts.Ports; Host adapter + Payment Application consume Contracts.
2. SellerPanelAccess edition from ICurrentEdition (no ToobaEdition.SingleStore hardcode).
3. CheckoutIdentityGate throws SemanticException(FoundationErrorCodes.CheckoutAuthenticationRequired).
4. Path/namespace: AuthSecurityHostOptions + SecurityHeadersMiddleware -> Tooba.Host.Security.
5. Guards/SoT reconciled to USER_REVIEW_HOST_SECURITY_AMC_001_R1_KEEP_THIN_PLATFORM_CERTIFIED.
