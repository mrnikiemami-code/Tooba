# Offer boundary — W11

## Rules

- Catalog → Offer.Application = ZERO
- Catalog → Offer.Infrastructure = ZERO
- Catalog → Offer.Domain = ZERO
- Catalog.Endpoints → Offer.Contracts = ZERO (no direct injection)
- No Catalog DB join to Offer

## Lawful path

```
Handler → IVariantOfferLookup (Application port)
       → VariantOfferLookupAdapter (Infrastructure)
       → IOfferLookupGateway (Offer.Contracts)
```

## Preserved enrichment

- Editor: `OfferCount` on variant list items
- Preview: `ReferencedByOffers` on combinations with existing variant ids
- Apply: `OfferCount` on returned variants

Host Attribute file no longer references Offer.
