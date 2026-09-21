# host-leak-scan

## Production Host (Tooba.Host)
- InventoryDbContext: 0 hits
- PromotionDbContext: 0 hits
- Uses IInventoryQueryGateway, IInventorySchemaMigrator, IPromotionSchemaMigrator
- Merchandising admin uses IMerchandisingCampaignDirectory/Query (transport composition)

## Host.Tests
Still constructs InventoryDbContext/PromotionDbContext in integration helpers — justified, not production authority.