# Tooba — Seller Offer & Listing Foundation

Status:

```text
COMPLETE — Architect accepted TB-P03-T002
```

Task:

```text
TB-P03-T002
```

## Purpose

Offer is the module-owned commercial listing of a seller on a Catalog Variant. It is not the descriptive product, not the price list, and not the stock ledger.

## Locked separations

```text
Catalog Product != Seller Offer / Listing
Catalog Variant != Offer
Seller != Identity User
Offer != Price
Offer != Inventory
```

`SellerOffer` has no `Price`, money, stock quantity, or `UserId`. Seller identity is `SellerPartyId` of a Party `Organization`. A Single-Store deployment still stores commercial listing as Offer; price must not move onto `CatalogProduct`.

## Aggregates

| Concept | Owner | Notes |
| --- | --- | --- |
| SellerOffer | Offer | Durable `OfferId`; opaque `CatalogVariantId`; `SellerPartyId`; optional seller-scoped SKU; `SalesChannel`; commercial `OfferStatus` |
| SalesChannel | Offer | Direct, Marketplace, Agency, Corporate, Affiliate, Api — not a free UI string |
| OfferStatus | Offer | Draft / Active / Suspended / Archived. Active is not purchasability |

One Catalog Variant may have many Offers (Seller A/B/C). Preferred uniqueness: at most one non-archived Offer per Seller + Variant + SalesChannel. Seller SKU uniqueness is within the same seller, not a global catalog id.

## Contracts and persistence

`OfferReference`, `IOfferLookupGateway`, `IOfferUseCaseGuard`, and `IOfferDirectory` live in Application. EF stays in Infrastructure/Domain.

Variant existence is validated through `ICatalogLookupGateway`. Seller existence is validated through `IPartyLookupGateway`. Offer Infrastructure must not reference Catalog or Party Infrastructure/persistence.

`OfferDbContext` owns schema `offer` on the resolved Marketplace or Single-Store database. No cross-module FK.

## Events

Outbox integration names:

- `offer.created.v1`
- `offer.activated.v1`
- `offer.suspended.v1`
- `offer.archived.v1`

These are seams for later Pricing, Inventory, Search, and Buy Box. Those modules are not implemented here.

## Out of scope

Pricing, Tax, Inventory, Cart, Order, Payment, seller onboarding workflow, seller portal UI, Buy Box, Search indexing, SEO seller pages, Shopeiva, Data Grid, Design System, commercial UI, TB-P03-T003, P03 Gate.
