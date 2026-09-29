# Analyze — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001

## Parent

`TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001`, wave 2 of 2.
Wave 1 (`...-CATALOG-SEED-REHOME-001`) is accepted at commit `e16781dc1899456aec20824afc01e19f53c9a70b`
and is preserved as-is by this task.

## Findings

`src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs` (175 LoC) held
exactly one remaining responsibility: **from-Host schema migration orchestration**.

Evidence extracted from the deleted file (recorded before deletion):

| # | Line | Foreign type named by Host | Ownership |
|---|------|----------------------------|-----------|
| 1 | 95 | `CatalogDbContext` | Catalog |
| 2 | 96 | `IOfferSchemaMigrator` (Offer.Contracts) | Offer |
| 3 | 97 | `IPricingSchemaMigrator` (Pricing.Contracts) | Pricing |
| 4 | 98 | `IInventorySchemaMigrator` (Inventory.Contracts) | Inventory |
| 5 | 99 | `ITaxSchemaMigrator` (Tax.Contracts) | Tax |
| 6 | 100 | `PartyDbContext` | Party |
| 7 | 101 | `IdentityDbContext` | Identity |
| 8 | 102 | `CartDbContext` | Cart |
| 9 | 103 | `OrderDbContext` | Order |
| 10 | 104 | `PaymentDbContext` | Payment |
| 11 | 105 | `FulfillmentDbContext` | Fulfillment |
| 12 | 106 | `IPromotionSchemaMigrator` (Promotion.Contracts) | Promotion |
| 13 | 113 | `PlatformProbeDbContext` | PlatformProbe |
| 14 | 114 | `ReviewsDbContext` | Reviews |
| 15 | 115 | `ProductQnADbContext` | ProductQnA |
| 16 | 116 | `BulkInquiryDbContext` | BulkInquiry |
| 17 | 117 | `WishlistDbContext` | Wishlist |
| 18 | 118 | `AddressBookDbContext` | AddressBook |
| 19 | 119 | `CustomerProfileDbContext` | CustomerProfile |
| 20 | 120 | `UserPreferenceDbContext` | UserPreference |
| 21 | 121 | `OperatorProfileDbContext` | OperatorProfile |
| 22 | 122 | `ContentDbContext` | Content |
| 23 | 123 | `MediaDbContext` | Media |
| 24 | 124 | `PageCompositionDbContext` | PageComposition |
| 25 | 125 | `StoryDbContext` | Story |
| 26 | 126 | `NotificationDbContext` | Notification |
| 27 | 127 | `AccessControlDbContext` | AccessControl |
| 28 | 128 | `Tooba.Support.Infrastructure.Persistence.SupportDbContext` | Support |

Ordering was a hard-coded linear list: Catalog → Offer → Pricing → Inventory → Tax → Party →
Identity → Cart → Order → Payment → Fulfillment → Promotion → PlatformProbe → Reviews →
ProductQnA → BulkInquiry → Wishlist → AddressBook → CustomerProfile → UserPreference →
OperatorProfile → Content → Media → PageComposition → Story → Notification → AccessControl →
Support.

Two module-owned steps were interleaved inside that list and are **not** plain context migrates:

1. after Promotion migrate (lines 108-111): `IMerchandisingCampaignDirectory.EnsureAmazingTypeSeededAsync`
   and `MerchandisingCampaignDevelopmentSeed.EnsureAsync` — explicitly documented as legacy-independent
   (`RunLegacyBootstraps=false` must still seed AMAZING campaigns, LOCK-SF-402);
2. in both seed branches (lines 152 and 172): the same `MerchandisingCampaignDevelopmentSeed.EnsureAsync`.

A third module-owned step lived in `Program.cs` (guarded by `RunLegacyBootstraps`):
`CatalogAttributeSchemaDevelopmentSeed.ApplyAsync`.

## Blockers

- Host cannot legally name 28 foreign `DbContext` types or foreign persistence namespaces.
- Ordering was Host-owned, so module migration was not module-owned.
- Nothing prevented adding a 29th foreign context without any module owning it.

## Required neutral seam

A neutral platform seam in `BuildingBlocks/Tooba.Persistence` that:

- carries **stable identity** (`string Module`) and **explicit order** (`int Order`);
- is registered by each module from **its own** Infrastructure composition root;
- keeps the five accepted special migrator contracts untouched;
- lets Host run migrations in a deterministic order without naming any module type.
