# 23 — Sale tests (TB-P06-T021)

## Backend minimum (task X)

| Concern | Coverage | Location |
|---|---|---|
| Sale eligibility (authoring → purchasable chain) | Documented + compose filters; write path tests | `08`, `SellerOfferSaleWriteTests`, Storefront compose |
| Seller ownership | Own create/price/inventory allow | `SellerOfferSaleWriteTests` |
| Foreign seller deny | Foreign Offer mutate → 404 / no qty change | `SellerOfferSaleWriteTests` |
| Authoritative price resolution | Pricing write via `IPriceDirectory`; no Product.Price | `06`, tests |
| Inventory availability | Inventory Set via `IInventoryDirectory`; no Product.Stock | `07`, tests |
| Cart / checkout / payment / order / fulfillment | Prior foundation suites remain green | `CartFoundationTests`, Order/Payment/Fulfillment Host tests |
| Promotion coupon on checkout | T020 `PromotionPanelTests` | Unchanged |

## Frontend

| Concern | Expectation |
|---|---|
| Typecheck / lint / unit | Green (Worker records in `24`) |
| Seller panel | Create/edit Offer flows call live APIs (`seller-api.ts`) |
| Critical storefront | If shared Home/PDP touched for behavior only: `npm run test:critical-storefront` — visual ACCEPT still separate |

## Explicit non-tests this wave

- Advanced multi-axis variant matrix (deferred)
- Production PSP bank integration
- Direct DB seed as sole merchandise path (superseded by HTTP/UI)

## Command placeholders

Fill exact pass counts in `24-final-validation.md` after Worker run:

```text
dotnet test … SellerOfferSaleWriteTests / full slnx
npm run typecheck | lint | test | build
```
