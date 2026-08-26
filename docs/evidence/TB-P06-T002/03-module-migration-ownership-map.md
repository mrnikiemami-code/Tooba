# 03 — Module migration ownership map (TB-P06-T002)

Each module owns its schema exclusively. Migration runner processes module DbContexts in deterministic order; no cross-module SQL.

| Order | Module | DbContext | PostgreSQL schema | Owning assembly |
|---|---|---|---|---|
| 1 | Catalog | `CatalogDbContext` | `catalog` | `Tooba.Catalog.Infrastructure` |
| 2 | Offer | `OfferDbContext` | `offer` | `Tooba.Offer.Infrastructure` |
| 3 | Pricing | `PricingDbContext` | `pricing` | `Tooba.Pricing.Infrastructure` |
| 4 | Inventory | `InventoryDbContext` | `inventory` | `Tooba.Inventory.Infrastructure` |
| 5 | Tax | `TaxDbContext` | `tax` | `Tooba.Tax.Infrastructure` |
| 6 | Party | `PartyDbContext` | `party` | `Tooba.Party.Infrastructure` |
| 7 | Identity | `IdentityDbContext` | `identity` | `Tooba.Identity.Infrastructure` |
| 8 | Cart | `CartDbContext` | `cart` | `Tooba.Cart.Infrastructure` |
| 9 | Order | `OrderDbContext` | `order` | `Tooba.Order.Infrastructure` |
| 10 | Payment | `PaymentDbContext` | `payment` | `Tooba.Payment.Infrastructure` |
| 11 | Promotion | `PromotionDbContext` | `promotion` | `Tooba.Promotion.Infrastructure` |
| 12 | PlatformProbe | `PlatformProbeDbContext` | `platform_probe` | `Tooba.PlatformProbe.Infrastructure` |
| 13 | Reviews | `ReviewsDbContext` | `reviews` | `Tooba.Reviews.Infrastructure` |
| 14 | ProductQnA | `ProductQnADbContext` | `product_qna` | `Tooba.ProductQnA.Infrastructure` |
| 15 | BulkInquiry | `BulkInquiryDbContext` | `bulk_inquiry` | `Tooba.BulkInquiry.Infrastructure` |
| 16 | Wishlist | `WishlistDbContext` | `wishlist` | `Tooba.Wishlist.Infrastructure` |
| 17 | AddressBook | `AddressBookDbContext` | `address_book` | `Tooba.AddressBook.Infrastructure` |
| 18 | CustomerProfile | `CustomerProfileDbContext` | `customer_profile` | `Tooba.CustomerProfile.Infrastructure` |
| 19 | Content | `ContentDbContext` | `content` | `Tooba.Content.Infrastructure` |

**Forbidden:** mega-DbContext migrations, cross-module schema mutation, foreign-module table changes.

**Registry source:** `ModuleMigrationRegistry.cs` — aligned with `ProductWorkspaceDevelopmentBootstrap` order.
