# Residual Offer debt for R4
- SellerPanelComposer still owns Price/Inventory/Tax/Order/Dashboard enrichment read models
- Offer endpoints still call IOfferSellerPanel for enrichment after ISender (Get/List/Create response shaping)
- Persian XML/docs may remain in untouched Offer Infrastructure Outbox/Migrations and Host non-Offer surfaces
- Catalog.Contracts / Party.Contracts are minimal gates only
