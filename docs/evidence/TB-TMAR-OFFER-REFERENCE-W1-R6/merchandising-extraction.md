# merchandising-extraction

`MerchandisingCampaignAdminComposer` uses `IOfferQueryGateway`.

- Add member: `FindOfferAsync` + Active status check
- Candidates: `ListRecentActiveOffersAsync(200)`
- Member enrich: `FindOffersBatchAsync`
- Pricing/Inventory/Catalog/Party remain owner lookups
