# TB-TMAR-ARCH-BASELINE — Current State Snapshot

## Module topology (verified)

| Layer | Count | Notes |
|---|---:|---|
| *.Domain | 31 | under `src/backend/Modules/*` |
| *.Application | 31 | contracts/interfaces live here today |
| *.Infrastructure | 32 | includes PlatformProbe |
| *.Contracts (per-module) | **0** | no `Tooba.*.Contracts.csproj` |
| Tooba.ModuleContracts | 1 | composition `IToobaModule` only |
| Tooba.Host | 1 | `src/backend/Host/Tooba.Host` |
| BuildingBlocks | Tooba.BuildingBlocks, Tooba.Persistence | shared |

## Persistence

- Per-module DbContext + schema + Migrations: **YES** (32 Schema constants found)
- Global business DbContext (`ToobaDbContext`/`AppDbContext`): **forbidden + tested** (`ArchitectureBoundaryTests`)
- Cross-schema FK: **none observed** (FKs within own schema only)
- Cross-module SQL JOIN: **not the pattern**; Host composes separate queries in memory
- Architecture tests: `src/backend/Host/Tooba.Host.Tests/ArchitectureBoundaryTests.cs`

### Schemas

```
﻿src/backend/Modules\Wishlist\Tooba.Wishlist.Infrastructure\Persistence\WishlistDbContext.cs:12:    public const string Schema = "wishlist";
src/backend/Modules\Notification\Tooba.Notification.Infrastructure\Persistence\NotificationDbContext.cs:14:    public const string Schema = "notification";
src/backend/Modules\Party\Tooba.Party.Infrastructure\Persistence\PartyDbContext.cs:17:    public const string Schema = "party";
src/backend/Modules\PageComposition\Tooba.PageComposition.Infrastructure\Persistence\PageCompositionDbContext.cs:12:    public const string Schema = "page_composition";
src/backend/Modules\Cart\Tooba.Cart.Infrastructure\Persistence\CartDbContext.cs:16:    public const string Schema = "cart";
src/backend/Modules\Localization\Tooba.Localization.Infrastructure\Persistence\LocalizationDbContext.cs:11:    public const string Schema = "localization";
src/backend/Modules\Identity\Tooba.Identity.Infrastructure\Persistence\IdentityDbContext.cs:16:    public const string Schema = "identity";
src/backend/Modules\OperatorProfile\Tooba.OperatorProfile.Infrastructure\Persistence\OperatorProfileDbContext.cs:12:    public const string Schema = "operator_profile";
src/backend/Modules\Media\Tooba.Media.Infrastructure\Persistence\MediaDbContext.cs:12:    public const string Schema = "media";
src/backend/Modules\AddressBook\Tooba.AddressBook.Infrastructure\Persistence\AddressBookDbContext.cs:12:    public const string Schema = "address_book";
src/backend/Modules\BulkInquiry\Tooba.BulkInquiry.Infrastructure\Persistence\BulkInquiryDbContext.cs:12:    public const string Schema = "bulk_inquiry";
src/backend/Modules\Reviews\Tooba.Reviews.Infrastructure\Persistence\ReviewsDbContext.cs:12:    public const string Schema = "reviews";
src/backend/Modules\Inventory\Tooba.Inventory.Infrastructure\Persistence\InventoryDbContext.cs:16:    public const string Schema = "inventory";
src/backend/Modules\Fulfillment\Tooba.Fulfillment.Infrastructure\Persistence\FulfillmentDbContext.cs:16:    public const string Schema = "fulfillment";
src/backend/Modules\Offer\Tooba.Offer.Infrastructure\Persistence\OfferDbContext.cs:16:    public const string Schema = "offer";
src/backend/Modules\AccessControl\Tooba.AccessControl.Infrastructure\Persistence\AccessControlDbContext.cs:14:    public const string Schema = "access_control";
src/backend/Modules\Content\Tooba.Content.Infrastructure\Persistence\ContentDbContext.cs:12:    public const string Schema = "content";
src/backend/Modules\Returns\Tooba.Returns.Infrastructure\Persistence\ReturnsDbContext.cs:16:    public const string Schema = "returns";
src/backend/Modules\CustomerProfile\Tooba.CustomerProfile.Infrastructure\Persistence\CustomerProfileDbContext.cs:12:    public const string Schema = "customer_profile";
src/backend/Modules\PlatformProbe\Tooba.PlatformProbe.Infrastructure\Persistence\PlatformProbeDbContext.cs:54:    public const string Schema = "platform_probe";
src/backend/Modules\Wallet\Tooba.Wallet.Infrastructure\Persistence\WalletDbContext.cs:12:    public const string Schema = "wallet";
src/backend/Modules\Payment\Tooba.Payment.Infrastructure\Persistence\PaymentDbContext.cs:16:    public const string Schema = "payment";
src/backend/Modules\Pricing\Tooba.Pricing.Infrastructure\Persistence\PricingDbContext.cs:16:    public const string Schema = "pricing";
src/backend/Modules\Story\Tooba.Story.Infrastructure\Persistence\StoryDbContext.cs:13:    public const string Schema = "story";
src/backend/Modules\Support\Tooba.Support.Infrastructure\Persistence\SupportDbContext.cs:12:    public const string Schema = "support";
src/backend/Modules\Tax\Tooba.Tax.Infrastructure\Persistence\TaxDbContext.cs:16:    public const string Schema = "tax";
src/backend/Modules\ProductQnA\Tooba.ProductQnA.Infrastructure\Persistence\ProductQnADbContext.cs:12:    public const string Schema = "product_qna";
src/backend/Modules\Settlement\Tooba.Settlement.Infrastructure\Persistence\SettlementDbContext.cs:16:    public const string Schema = "settlement";
src/backend/Modules\Catalog\Tooba.Catalog.Infrastructure\Persistence\CatalogDbContext.cs:16:    public const string Schema = "catalog";
src/backend/Modules\UserPreference\Tooba.UserPreference.Infrastructure\Persistence\UserPreferenceDbContext.cs:13:    public const string Schema = "user_preference";
src/backend/Modules\Promotion\Tooba.Promotion.Infrastructure\Persistence\PromotionDbContext.cs:17:    public const string Schema = "promotion";
src/backend/Modules\Order\Tooba.Order.Infrastructure\Persistence\OrderDbContext.cs:16:    public const string Schema = "order";

```

## Cross-module dependencies (csproj)

- Application → foreign Application: **16** edges (contracts-in-Application)
- Application → foreign Infrastructure: **0**
- Infrastructure → foreign Infrastructure: **0**
- Domain → Infra/Host/Application: **0**

### Application→Application edges

- Tooba.Cart.Application -> Tooba.Catalog.Application
- Tooba.Cart.Application -> Tooba.Offer.Application
- Tooba.Cart.Application -> Tooba.Pricing.Application
- Tooba.Cart.Application -> Tooba.Inventory.Application
- Tooba.Inventory.Application -> Tooba.Offer.Application
- Tooba.Inventory.Application -> Tooba.Catalog.Application
- Tooba.Order.Application -> Tooba.Cart.Application
- Tooba.Order.Application -> Tooba.Offer.Application
- Tooba.Order.Application -> Tooba.Pricing.Application
- Tooba.Order.Application -> Tooba.Inventory.Application
- Tooba.Order.Application -> Tooba.Tax.Application
- Tooba.Order.Application -> Tooba.Promotion.Application
- Tooba.Pricing.Application -> Tooba.Offer.Application
- Tooba.Promotion.Application -> Tooba.Offer.Application
- Tooba.Promotion.Application -> Tooba.Pricing.Application
- Tooba.Promotion.Application -> Tooba.Inventory.Application

## Communication patterns

| Pattern | Present | Notes |
|---|---|---|
| Sync Directory/Gateway | YES | CartDirectory, CheckoutDirectory, PriceDirectory, … |
| Domain Events | YES | e.g. Order `IHasDomainEvents` |
| Outbox | YES | per-module `outbox_messages` |
| Integration Events | YES | Outbox translation |
| MassTransit SQL transport | YES | schema `transport` (foundation docs) |
| Transaction boundary | YES | e.g. CheckoutDirectory `TransactionScope` |

## Host coupling snapshot

- Host C# files audited: **216**
- Files with DbContext usage: **81**
- Files with SaveChanges / write signals: **undefined**
