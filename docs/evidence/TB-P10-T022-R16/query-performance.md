# TB-P10-T022-R16 — Query Performance

## Shapes

1. Campaign winner: filter StoreId + PromotionTypeId + Lifecycle + time window; `ORDER BY Priority DESC, StartAt DESC, Id ASC LIMIT 1` (EF `FirstOrDefault`).
2. Members: `WHERE CampaignId = @id ORDER BY SortOrder, Id TAKE fetch` (fetch ≤ 48).
3. Offers: one `WHERE OfferId IN (...)` batch.
4. Prices: one `WHERE OfferId IN (...) AND Market/Channel/Currency/Active/Base` batch; effective window applied in memory.
5. Inventory: existing `GetAvailabilityBatchAsync` single join query.

## Guards

- No full SellerOffer scan
- No per-member price/inventory round-trips
- Member take capped at 48
