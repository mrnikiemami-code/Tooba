# TB-P04-T005 domain composition map

```text
Workspace UI
  -> Host ProductWorkspaceComposer
     -> CatalogDbContext (product, variant, media refs, localized text)
     -> OfferDbContext (seller offers by catalog variant ids)
     -> PricingDbContext (authored prices by offer ids)
     -> TaxDbContext (offer classification + category)
     -> InventoryDbContext (positions + locations)
```

No mega-endpoint SQL join. No Product.Price. No Product.Stock.
