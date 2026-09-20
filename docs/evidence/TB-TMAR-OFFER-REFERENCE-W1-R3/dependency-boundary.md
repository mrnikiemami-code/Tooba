# Dependency boundary

- Added `Tooba.Catalog.Contracts.ICatalogVariantLookup`.
- Added `Tooba.Party.Contracts.IPartyLookup`.
- Owning infrastructure directories implement and register those contracts.
- Offer Application references only the foreign Contracts projects.
- Offer Infrastructure has no Catalog.Application or Party.Application reference.
