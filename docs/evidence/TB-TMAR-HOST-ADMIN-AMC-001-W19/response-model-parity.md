# W19 — Response model parity

## Authority

`Tooba.ProductWorkspace.Application.Composition.Models` is authoritative for aggregate GET JSON.

Host write routes / Host composer temporary GetAsync compile against the same types (single evolving shape).

## Preserved top-level ProductWorkspaceView fields

ProductId, Title, Status, Kind, BrandName, CategoryNames, Attributes, Variants, Media, Offers, Prices, TaxClassifications, Stock, Seo, Publication, Activity, Audit, Permissions, CatalogUpdatedAt, ReadinessWarnings, UnsupportedMutations, PrimaryCategoryId, CategoryPath, Slug, ShortDescription, Translations, IsPrimaryCategoryAssignable, BrandId, CategoryAssignments, UnitOfMeasureId, QuantityDecimalPlaces, QuantityStep, UnitCode, UnitDisplayName, Units.

## Nested parity

Variant OfferCount/LocationCount, media primary-first ordering, category role labels, readiness checks, commercial warnings, PurchasableHint, UnsupportedMutations list — preserved by handler composition tests and Host residual composer for writes.
