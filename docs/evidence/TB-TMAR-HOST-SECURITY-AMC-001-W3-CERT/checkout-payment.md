# checkout-payment — TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT

## CheckoutIdentityGate

- Depends on Catalog Contracts checkout identity policy lookup only
- Expected failure: `SemanticException` + `FoundationErrorCodes.CheckoutAuthenticationRequired`
- No InvalidOperation / message-text remap / hard-coded titles
- Fail-closed when authentication required

## HostCheckoutActorPolicyAdapter

- Implements `Tooba.Payment.Contracts.Ports.ICheckoutActorPolicyPort`
- Thin delegation; ZERO Payment.Application/Infrastructure/Domain
- No service locator

## HostPaymentStorefrontAuthorizer

- Active DI → Payment.Endpoints storefront seam
- Thin session/identity adaptation; no payment business policy
- No hard-coded titles / GetRequiredService / foreign Payment layers

Classification: KEEP_AS_THIN_HOST_SECURITY_ADAPTER (both Checkout + Payment)
