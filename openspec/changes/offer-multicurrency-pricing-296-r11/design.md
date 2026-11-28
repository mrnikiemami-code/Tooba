# Design — offer-multicurrency-pricing-296-r11

## Ownership
- Pricing remains the sole owner of authored price truth, currency precision policy and the authoritative system currency reference.
- Offer remains the owner of the offer aggregate and the seller/admin workspace surface. Offer reads/writes prices only through Pricing Contracts.
- Host supplies commercial configuration (`Pricing:Currencies`) as platform composition; it contains no business policy or handlers.
- No new parallel price path: `SetSellerOfferPrice`/`ISellerOfferPricingGateway` remain the single write seam; `IPriceLookupGateway`/new read seam remain the only read seams.

## Authoritative system currency reference
- `Pricing:Currencies` control-plane configuration lists the active system currencies: ISO code, English name, native name, decimal places, sort order.
- Pricing binds it in `PricingModule` (`PricingCurrenciesOptions`) and exposes `ICurrencyReferenceReader` from Infrastructure.
- `AuthoredCurrency`/`CurrencyCode` remain the ISO/scale kernel; the configured reference must be a superset of the store default currency.
- Unknown/inactive/display-unit currencies fail closed with the existing `pricing.currency.invalid` or `pricing.currency.display_unit` typed errors. No FX conversion is introduced.

## Store, market and currency
- Market stays the trusted store market from `ICurrentStoreCommerceContext`; it is not client-supplied and is not shown as a column.
- A price row is keyed by offer + store market + channel + currency + base qualifier, matching the existing unique index. Multiple currencies therefore produce multiple current base rows for one offer.
- The store default currency remains the primary amount for lists/quote/checkout. Additional currencies are authored prices visible in the Offer pricing tab.

## Contracts and read model
- Pricing.Contracts: `SystemCurrencyView`, `ICurrencyReferenceReader.ListActiveAsync`, `SellerOfferPriceView`, and `ISellerOfferPricingGateway.ListBasePricesAsync(offerId)`.
- Offer.Contracts: `OfferCurrencyOption` and `OfferPriceView`; `SellerOfferDetailPage` gains `Prices`.
- Offer.Application: a `GetOfferCurrenciesQuery` maps the Pricing reference into the Offer DTO; the read-model composer lists current base prices per currency and keeps `Amount`/`Currency`/`PriceRevision` as the primary store-default values for backward compatibility.
- Pricing write (`SetPriceAsync`): validates the requested currency against the active system reference; uses the store market (client `market` is ignored/no longer required); keeps advisory lock, expected-revision CAS, durable facts and outbox unchanged.
- Per-currency revision: `SellerOfferPriceView.Revision` uses the existing `priceId:updatedTicks/10` revision so the UI can CAS the selected currency row.

## Frontend
- `OfferWorkspaceDetail.prices` lists authored base prices; `currencies` comes from the owning read endpoint through the adapter.
- Pricing tab: a table of all current prices (currency, amount, status) and one editor with a currency selector + amount. Selecting a currency loads that row's amount/revision; saving CASes the selected currency revision. IRR shows an integer input and no decimals.
- The shared workspace keeps a single `draft.currency`/`draft.amount`; dirty/version checks compare the selected currency row. No duplicated admin/seller workflow.
- All new labels are added to `messages.ts` (fa/en) and `messages-ru.ts` (ru); the language registry/route supplies direction and locale.

## Verification
- Backend: Pricing domain/infrastructure tests (currency reference, multi-currency write, IRR integer, unknown currency rejection), Offer read-model/command tests.
- Frontend: model/format tests, mounted workspace regression.
- Connected: real Host 5088 + tooba_alpha + Playwright: add IRR and USD prices to one offer, reload, independent SQL/GET shows two base rows and per-currency revisions; IRR integer; RTL/locales; existing Regressions.
