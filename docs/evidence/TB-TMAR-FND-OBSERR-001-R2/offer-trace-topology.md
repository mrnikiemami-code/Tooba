# Offer trace topology — TB-TMAR-FND-OBSERR-001-R2

## Instrumentation placement

Decorators under `Tooba.Offer.Infrastructure/Adapters/Tracing/` registered by `AddOfferModuleCallTracing()` after all modules:

| Gateway | Source → Target | Operation examples |
| --- | --- | --- |
| `ICatalogVariantLookup` | Offer → Catalog | LookupVariant |
| `ICatalogOfferReadGateway` | Offer → Catalog | LookupPresentations |
| `IPartyLookup` | Offer → Party | LookupParty |
| `IPriceLookupGateway` | Offer → Pricing | LookupPrice / LookupPricesBatch |
| `ISellerOfferInventoryGateway` | Offer → Inventory | LookupAvailability |

## Expected shapes

**List/Get**

```text
HTTP
→ mediatr.Offer.ListSellerOffersQuery / GetOfferQuery
→ module.Offer.Catalog.LookupPresentations
→ module.Offer.Pricing.LookupPricesBatch (or LookupPrice)
→ module.Offer.Inventory.LookupAvailability
```

**Create**

```text
HTTP
→ mediatr.Offer.CreateOfferCommand
→ module.Offer.Catalog.LookupVariant
→ module.Offer.Party.LookupParty
→ (persistence / Detail enrichment may add Catalog/Pricing/Inventory)
```

## Proof

`Tooba.Offer.Tests.Observability.OfferTraceTopologyTests` with `ActivityListener` asserts Catalog/Pricing/Inventory on list and Catalog/Party on create; one span per logical call.
