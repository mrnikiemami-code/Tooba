# TB-P04-T007 — Live data map

| UI field | Source | Not |
| --- | --- | --- |
| Product title | Catalog localized name | fixture JSON |
| Slug / canonical | Catalog `SlugSeam` | invented storefront ids |
| Category | Catalog category assignment + localized name | hardcoded menu JSON |
| Brand | Catalog brand localized name | Product.Brand string bag |
| Description | Catalog localized `description` when present | CMS |
| Media | Catalog `MediaAssetId` + Host presentation SVG | Product image URL as domain truth |
| Primary amount | Pricing `AuthoredPrice.Amount` on selected Offer | Product.Price |
| Currency / market | Pricing record | locale guess |
| Availability | Inventory positions `OnHand - Reserved` summed per Offer | Product.Stock |
| Seller name | Party display name for Offer.SellerPartyId | raw PartyId as primary label |
| Seller SKU | Offer.SellerSku | Catalog SKU as seller identity |
| Other sellers | Other Active Offers on the same product variants | comparison marketplace feature |
| Tax label | Tax offer classification display name | calculated invoice tax (deferred UX) |
| Promotion badge | none on current seed; would require `IPromotionEvaluator` | fake discount sticker |
| Search results | Host listing filter on live titles/sellers | Fuse demo |
| Cart add | disabled; next boundary is Cart HTTP | Shopeiva local cart |
