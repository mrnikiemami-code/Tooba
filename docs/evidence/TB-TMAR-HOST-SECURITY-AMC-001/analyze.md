# Analyze — Host/Security AMC-001

## Inventory

18 production files under `Host/Tooba.Host/Security` (~565 LOC):
- Root: `AuthSecurityHostOptions`, `SecurityHeadersMiddleware`
- Checkout: `CheckoutIdentityGate`, `HostCheckoutActorPolicyAdapter`
- Payment: `HostPaymentStorefrontAuthorizer`
- Seller: panel gate + 11 thin module authorizer adapters + error codes (13 files)

## Ownership verdict

| Slice | Disposition |
| --- | --- |
| Entire `Host/Security` | **KEEP_THIN_PLATFORM_SECURITY_BOUNDARY** (not HOST_ZERO) |
| Seller | Canonical R1A Host seller security platform boundary |
| Checkout / Payment | Thin Host adapters locked by Storefront R2 / Payment audit |
| Headers / options | Host HTTP platform — must stay |

## Coupling

- Seller: ZERO foreign Application/Domain/Infrastructure/Persistence
- Checkout gate: Catalog **Contracts** only
- Payment authorizer: Endpoints port + Host session
- Checkout actor adapter: Payment.Application **Ports** only (thin)

## Must evacuate

None under current TMAR locks.
