# Offer trace topology final — TB-TMAR-FND-OBSERR-001-R4

Seller routes in `OfferSellerEndpoints` are thin `ISender` or owner-gateway calls. They do not start activities.

## List / Get

HTTP server span (ASP.NET) → MediatR query span `mediatr.Offer.ListSellerOffersQuery` or `GetOfferQuery` → `OfferReadModelComposer` module-call spans to Catalog, Pricing, and Inventory via `AddOfferModuleCallTracing`.

`OfferTraceTopologyTests.List_read_path_emits_catalog_pricing_inventory_module_spans` asserts one span per target, not an N+1 fan-out.

## Create

HTTP → `mediatr.Offer.CreateOfferCommand` → traced Catalog validation and Party validation → Offer persistence in `Offer.Infrastructure` only.

`Create_path_emits_catalog_and_party_module_spans` covers the module spans.

## Price alias

`POST|PUT /offers/{offerId}/price` calls `ISellerOfferPricingGateway.SetPriceAsync`, then `GetOfferQuery`. Pricing remains the price owner.

## Inventory alias

`POST|PUT /offers/{offerId}/inventory` calls `ISellerOfferInventoryGateway.SetInventoryAsync`, then `GetOfferQuery`. Inventory remains the stock owner.

Span names use type and module names. They do not embed offer ids or SKUs.

Verdict: PASS.
