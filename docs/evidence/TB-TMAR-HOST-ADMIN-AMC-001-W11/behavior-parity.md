# Behavior parity — W11

Preserved for five Admin routes:

- locale default `fa-IR`
- editor shape, localized labels, axes ordering, selected options
- OfferCount enrichment
- preview combination counts/actions/message/warning + ReferencedByOffers
- apply created/unchanged/deactivated counts + OfferCount
- readiness shape
- MaxVariantCombinations = 200
- archive not hard-delete; resurrect archived matching as Draft
- single-default; archived cannot be default
- Admin authorization + actor binding + tenant isolation via Catalog module seams

Legacy Host Seller `PUT .../variant-axes` still uses `ICatalogDirectory` wrapper (out of W11 Admin scope); DTO relocated Seller-local.

Category-change Host routes unchanged.
