# Pricing audit — TB-TMAR-REFBATCH-TP-001

Six projects remain: Domain, Application, Contracts, Infrastructure, Endpoints, Tests.

| Check | Result |
| --- | --- |
| Authored price ownership | `AuthoredPrice` stays in Pricing.Domain. |
| Offer aliases | Seller price write in Offer.Endpoints calls `ISellerOfferPricingGateway.SetPriceAsync`. It does not open Pricing persistence. |
| Cross-module reads | `IPriceLookupGateway` and new `IPriceQueryGateway`. |
| Offer Domain / Application | Pricing.Domain no longer references Offer.Contracts. Channel is `PriceChannel`, mapped to Offer `SalesChannel` at the contract boundary. Stored names are unchanged, so no data migration. |
| OfferDbContext | Absent from Pricing production. |
| Host pricing authority | Host no longer names `PricingDbContext` or calculates prices. Campaign seed writes through `IPriceDirectory` and reads through `IPriceQueryGateway`. |
| Currency | Domain uses `AuthoredCurrency`. Public `CurrencyCode` stays in `Tooba.Pricing.Contracts`. |
| Batch lookup | `ResolvePricesBatchAsync` and `ResolveCampaignPricesBatchAsync` remain single queries. Grid amounts use `ListOfferAmountsAsync`. |
| Seller authorization | `SetPriceAsync` still requires `SellerPartyId` to match the offer from `IOfferLookupGateway`. |
| Endpoints | `MapPricingModule` stays thin. HTTP seller price errors use the global pipeline via `PricingErrorCatalogContributor`. |
| TypeForwardedTo | Removed. |
| Domain → Contracts | Removed. |
| Time / id | `PriceDirectory` uses `IClock` and `IIdGenerator`. |
| MediatR | Not added. Pricing has no module-owned HTTP route. The seller write is an Offer HTTP use case calling the Pricing contract, same shape as inventory. |
| Tracing | `PriceDirectory.FindOfferAsync` uses `IModuleCallTracer` (`Pricing` → `Offer` / `LookupOffer`). No raw `StartActivity`. |

`SetPriceAsync` now selects the base qualifier only, so a campaign row is not rewritten by a seller base-price write.
