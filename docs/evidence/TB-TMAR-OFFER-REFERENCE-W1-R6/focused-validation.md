# focused-validation

## Offer.Tests

Full suite: **Passed 35 / Failed 0**

Includes new `OfferQueryGatewayTests` + strengthened architecture persistence guards.

## Host.Tests (focused)

Filter: OrderOfferContracts / StorefrontDemoCatalogSeed.Demo_seed / OfferFoundation / MerchandisingCampaignAdmin

**Passed 9 / Failed 0**

Updated for R6 boundary:

- `AdminPanelCompositionTests.Composer_reads_module_contexts_separately...` (expects `IOfferQueryGateway`)
- `OfferDirectoryTestHelper.OfferTestSender` (CreateOffer → `SellerOfferDetailPage`)
- Host/Host.Tests `OfferGlobalUsings`: `OfferStatus` aliases Contracts DTO

Note: `Every_admin_product_handler_invokes_server_authorization` expects 31 auth calls vs current 32 in `ProductWorkspaceEndpoints.cs` — **pre-existing characterization drift**, file not touched by R6 Offer persistence extraction.

## Build

- `Tooba.Host`: succeeded
- `Tooba.MigrationRunner`: succeeded
- `Tooba.Offer.Infrastructure`: succeeded
