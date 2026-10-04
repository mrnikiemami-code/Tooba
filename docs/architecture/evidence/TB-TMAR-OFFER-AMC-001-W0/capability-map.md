# Offer capability map — W0

Capabilities are business responsibility axes, not invented folder names. Discovered from the existing Endpoints audience map, Contracts/Domain aggregates and ports.

| Capability | Evidence on disk | Layer touchpoints |
| --- | --- | --- |
| **Offers** (seller listing authoring + lifecycle) | `SellerOffer` aggregate; 9 commands; 2 queries; `OfferReference`; `OfferSellerEndpoints` | Domain, Contracts, Application, Infrastructure, Endpoints |
| ↳ Listing authoring | `CreateOfferCommand`, `UpdateOfferCommand` | Application |
| ↳ Lifecycle | `ActivateOfferCommand`, `SuspendOfferCommand`, `ArchiveOfferCommand` | Application |
| ↳ Return-policy governance | `SetReturnPolicyCommand`, `ReturnPolicyResolver` | Application (+ Contracts port) |
| ↳ Order-quantity limits | `SetOrderQuantityLimitsCommand` | Application |
| ↳ Delegated price write | `SetOfferPriceCommand` → `ISellerOfferPricingGateway` | Application (Pricing.Contracts) |
| ↳ Delegated inventory write | `SetOfferInventoryCommand` → `ISellerOfferInventoryGateway` | Application (Inventory.Contracts) |
| ↳ Read projection | `GetOfferQuery`, `ListSellerOffersQuery`, `OfferReadModelComposer` | Application |
| **Selection policy** | `IPrimaryOfferSelectionPolicy` (Contracts) + `PrimaryOfferSelectionPolicy` (Application); consumed by Catalog storefront composition | Contracts, Application |
| **Cross-module lookup boundary** | `IOfferLookupGateway`, `IOfferQueryGateway`, `IOfferSellerProductIdLookup`, `IOfferDevelopmentSeedGateway`, `IOfferSchemaMigrator` | Contracts, Infrastructure |
| **Return-policy boundary contract** | `ReturnPolicyOptions`, `OfferReturnPolicyChoices`, `ResolvedReturnPolicy`, `IReturnPolicyResolver` | Contracts |
| **Persistence / schema** | `OfferDbContext` (schema `offer`), `SellerOfferConfiguration`, 3 migrations | Infrastructure |

## Audience map (Endpoints)

| Audience | Folder | Status |
| --- | --- | --- |
| Seller | `Endpoints/Seller/` | PRESENT (6 routes) |
| Admin | — | not applicable (no Admin Offer route exists) |
| Storefront | — | not applicable (storefront reads Offer via `IOfferQueryGateway` from Catalog composition) |

`Endpoints/Admin` and `Endpoints/Storefront` do not exist and must **not** be created as empty ceremony.

## Contracts-boundary capabilities

`Contracts/Dtos` (6), `Contracts/Errors` (1), `Contracts/Ports` (7 → 8 after the `ReturnPolicy` split). `Contracts/Ports` holds only module-boundary semantics; `ReturnPolicyResolver` (implementation) is the single exception and is the W1 cohesion fix.
