# TB-TMAR-HOST-STOREFRONT-AMC-001-R2 — Migrate

## Scope

Evacuate Host residual storefront **settings reads** and **media serving**, relocate thin security adapters.

## Disposition executed

| Item | Before | After |
| --- | --- | --- |
| `GET /checkout-identity-policy` | Host StorefrontEndpoints + CheckoutIdentityGate(DbContext) | Catalog CQRS + Endpoints; Host gate uses `ICatalogCheckoutIdentityPolicyLookup` |
| `GET /appearance` | Host + StoreAppearanceProjector | Catalog `GetStorefrontAppearanceQuery` + Endpoints |
| `GET /media/{assetId}` | Host proxy to MediaAssetServing | Media.Endpoints `MediaStorefrontEndpoints` |
| `CheckoutIdentityGate` | Host/Storefront + CatalogDbContext | Host/Security/Checkout + Catalog Contracts only |
| `HostCheckoutActorPolicyAdapter` | Host/Storefront | Host/Security/Checkout |
| `HostPaymentStorefrontAuthorizer` | Host/Storefront | Host/Security/Payment |

## Behavior parity

- checkout-identity JSON: `policy`, `cartAnonymousAllowed`, `checkoutAuthenticationRequired`
- appearance JSON: storeScope/palette/tokens/tint/updatedAt (incl. sectionSurface* aliases)
- media: TryServeStoredMediaAsync → PlaceholderSvg fallback
- EnsureCheckoutActor still throws `checkout.authentication_required`

## Guards

- `HostStorefrontAmcR2GuardTests`
- Updated: CheckoutIdentity, Media evacuation, Payment architecture, StorefrontPaymentActorOwnership

## Explicit non-goals

- StorefrontComposer / browse BFF (R3)
- DemoCatalog (R4)
- Geography provinces (residual with composer wave)
