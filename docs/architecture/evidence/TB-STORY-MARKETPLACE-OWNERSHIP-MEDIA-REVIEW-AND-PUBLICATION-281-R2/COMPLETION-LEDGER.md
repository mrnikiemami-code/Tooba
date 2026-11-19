# TB-281-R2 COMPLETION LEDGER

Expected base: `ae947c863867437c13008029971a77eeb90edd41` (docs-only issuance `73202686f`).

| Remaining requirement | Current source | Implementation | Proving test | Final status |
| --- | --- | --- | --- | --- |
| Media seller upload atomicity | `MediaDirectory.Story.UploadForSellerAsync` | Single SaveChanges for asset+binding+occurrence after store; detach attempt; store delete only | Source review + prior compile; recording-store suite | PARTIAL — algorithm landed; dedicated recording-store fact suite not completed |
| Admin-for-shop media upload CQRS | Media seller endpoints direct port | `UploadSellerStoryMediaCommand` + admin `POST /upload-for-shop` via ISender + Party Active gate | Endpoint/source | LANDED (DI/transport) |
| Cover bind all shop paths | Seller item bind only | `BindCoverAsync` on seller create/update + admin-for-shop create | Covered by create path + media refuse | LANDED for create/update shop paths |
| CTA product ownership | CtaTarget raw | `IStoryProductTargetPort` + Catalog card adapter; refuse foreign product | `StoryMarketplace281R2DirectoryTests.Foreign_product_cta_refuses_before_create` | LANDED (controlled port) |
| Occurrence/version all routes | Partial | Seller create/add + stop/return occurrence-before-version; item ExpectedVersion | R2 stop/create occurrence facts | PARTIAL — admin composites/item full matrix not completed |
| return_to_draft after stop | Policy Active/Scheduled only | Disabled+Approved included; stop→read→return tested | `Stop_then_read_offers_return_to_draft…` | LANDED |
| Seller server query | List only | `POST /v1/seller/stories/query` + Directory `SellerQueryAsync` | `Seller_query_pages_and_counts_owner_shop_only` | LANDED |
| Admin shop-name filter | SellerPartyId only | Party projections for Story shop ids; `shopDisplayName`/`shopName` | Source in `AdminStoryGridQueryEngine` | LANDED (source; no dedicated PG fact) |
| Public shop identity | SellerPartyId on card | Batch DisplayName enrich; ShopPublicId stays null (no fabricated Guid) | Source EnrichPublicAsync | PARTIAL — name only; no canonical public id port |
| G1–G7 PG rollback + recording store | Focused InMemory | R2 InMemory Directory 4/4 + R1 retained 3/3 + AMSC 10/10 | Focused Host tests 17/17 | PARTIAL — PG rollback-only + Media recording store open |
| Host no-seed refresh | pricing.overlap seed | `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1` in Program.cs | Composition opt-out present | See schema-and-runtime-proof |
| ui-contract-handoff complete | Partial R1 | Updated handoff samples for query/upload/occurrence | Evidence file | PARTIAL samples |
