# TB-P07-T041-R1 — Module query matrix

| List | Owning persistence | Filter/sort layer | Page materialize | Cross-module enrich |
|------|--------------------|-------------------|------------------|---------------------|
| Orders | OrderDbContext (Checkout + SellerOrders) | EF on Checkout aggregates | Include page SellerOrders/Lines | None for list |
| Sellers | Offer + Party + Order contexts | Party scalars SQL; offer/order metrics via ID sets (product-grid pattern) | Party page + count maps | No SQL JOIN across schemas |
| Customers | OrderDbContext group-by PlacedByUserId | EF aggregates | Latest checkout snapshot for page users | None |
| Fulfillments | FulfillmentDbContext | EF on FulfillmentUnit (+ shipment count) | Batch items/shipments for page | None |
| Returns | ReturnsDbContext | EF on ReturnRequest (+ item count) | Batch items/attempts for page | None |
| Payouts | SettlementDbContext queue Pending\|Failed | EF on PayoutRequest | Batch attempts for page | None (SellerPartyId text) |
| Content | ContentDbContext | EF on ContentArticle | Map page rows | None |
| Reviews | ReviewsDbContext Pending | EF scalars; product title via Catalog ID set | `GetProductTitlesAsync` page only | Catalog gateway/batch |
| Stories | StoryDbContext | EF on Story (+ item count); optional reviewStatus | Attach items for page | None |

Products remain on existing `AdminProductGridQueryEngine` (unchanged).
