# 02 — EF migration inventory (TB-P06-T002)

19 module-owned EF DbContexts. Each uses per-schema `__ef_migrations_history` via `ToobaNpgsql`.

| Module | DbContext | Schema | Infrastructure project | Latest migration (representative) | Dev auto-migrate | Production startup |
|---|---|---|---|---|---|---|
| Catalog | `CatalogDbContext` | `catalog` | `Tooba.Catalog.Infrastructure` | `20260826173000_EnsureBrandLogoMediaAssetIdColumn` | Yes (Development bootstrap) | No |
| Offer | `OfferDbContext` | `offer` | `Tooba.Offer.Infrastructure` | `20260823082919_InitialOffer` | Yes | No |
| Pricing | `PricingDbContext` | `pricing` | `Tooba.Pricing.Infrastructure` | `20260823085546_InitialPricing` | Yes | No |
| Inventory | `InventoryDbContext` | `inventory` | `Tooba.Inventory.Infrastructure` | `20260823100153_ReservationExpiry` | Yes | No |
| Tax | `TaxDbContext` | `tax` | `Tooba.Tax.Infrastructure` | `20260823190000_InitialTax` | Yes | No |
| Party | `PartyDbContext` | `party` | `Tooba.Party.Infrastructure` | `20260823062413_InitialParty` | Yes | No |
| Identity | `IdentityDbContext` | `identity` | `Tooba.Identity.Infrastructure` | `20260823064756_SessionCredentialLifecycle` | Yes | No |
| Cart | `CartDbContext` | `cart` | `Tooba.Cart.Infrastructure` | `20260823100150_InitialCart` | Yes | No |
| Order | `OrderDbContext` | `order` | `Tooba.Order.Infrastructure` | `20260823210000_OrderPromotionSnapshots` | Yes | No |
| Payment | `PaymentDbContext` | `payment` | `Tooba.Payment.Infrastructure` | `20260823140000_InitialPayment` | Yes | No |
| Promotion | `PromotionDbContext` | `promotion` | `Tooba.Promotion.Infrastructure` | `20260823210000_InitialPromotion` | Yes | No |
| PlatformProbe | `PlatformProbeDbContext` | `platform_probe` | `Tooba.PlatformProbe.Infrastructure` | `20260823000054_InitialPlatformProbe` | Yes | No |
| Reviews | `ReviewsDbContext` | `reviews` | `Tooba.Reviews.Infrastructure` | `20260825151244_InitialReviews` | Yes | No |
| ProductQnA | `ProductQnADbContext` | `product_qna` | `Tooba.ProductQnA.Infrastructure` | `20260826120000_InitialProductQnA` | Yes | No |
| BulkInquiry | `BulkInquiryDbContext` | `bulk_inquiry` | `Tooba.BulkInquiry.Infrastructure` | `20260826120000_InitialBulkInquiry` | Yes | No |
| Wishlist | `WishlistDbContext` | `wishlist` | `Tooba.Wishlist.Infrastructure` | `20260825162434_InitialWishlist` | Yes | No |
| AddressBook | `AddressBookDbContext` | `address_book` | `Tooba.AddressBook.Infrastructure` | `20260825171858_InitialAddressBook` | Yes | No |
| CustomerProfile | `CustomerProfileDbContext` | `customer_profile` | `Tooba.CustomerProfile.Infrastructure` | `20260825225000_InitialCustomerProfile` | Yes | No |
| Content | `ContentDbContext` | `content` | `Tooba.Content.Infrastructure` | `20260826172101_InitialContent` | Yes | No |

**Separate (not EF business schemas):** MassTransit PostgreSQL SQL transport uses `AddPostgresMigrationHostedService` in Host messaging registration — transport infra only.

**Dev auto-migrate source:** `ProductWorkspaceDevelopmentBootstrap.ApplyAsync` — all 19 contexts, `MigrateAsync()`, gated by `app.Environment.IsDevelopment()` in `Program.cs`.

**Production:** explicit ops step via `Tooba.MigrationRunner` CLI (`status` / `plan` / `apply`).
