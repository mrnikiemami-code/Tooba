# Evidence — TB-P06-T021

**Sellable Product Flow / Commercial E2E Closure**

| Field | Value |
|---|---|
| Task-ID | `TB-P06-T021` |
| Phase | P06 |
| Channel | `tooba-main` |
| Commit message target | `feat close sellable product flow [TB-P06-T021]` |
| Architect status (SoT) | `AWAITING_ARCHITECT_ACCEPT` |
| May report readiness | `SELLABLE_PRODUCT_FLOW_LIVE` |
| Must NOT report | `PRODUCTION_GO_LIVE_READY`, `USER_VISUAL_ACCEPTED` |

## Prior Architect ledger

| Task | Status |
|---|---|
| TB-P06-T018 | ACCEPTED |
| TB-P06-T019 | SUPERSEDED_BY_ARCHITECT_RESCOPE |
| TB-P06-T019-R1 | ACCEPTED |
| TB-P06-T020 | ACCEPTED |
| TB-P06-T021 | AWAITING_ARCHITECT_ACCEPT |

## Capability flags

```text
PRODUCT_SALE_FLOW = LIVE (demo)
SELLER_PRODUCT_MANAGEMENT = LIVE (Admin Catalog + Seller Offer)
SELLER_OFFER_MANAGEMENT = LIVE
PRICING_PATH = LIVE
INVENTORY_PATH = LIVE
CART_CHECKOUT_PAYMENT = LIVE
SELLER_ORDER_FULFILLMENT = LIVE
CUSTOMER_ORDER_TRACKING = LIVE
ADVANCED_VARIANT_ARCHITECTURE = DEFERRED
VISUAL_CONTRACT = SHOPEIVA_LOCKED
SELLABLE_DEMO = YES
PRODUCTION_GO_LIVE_READY = NO
```

## Ownership model

```text
Admin Catalog Product (+ default Variant)
→ Seller Offer on catalogVariantId
→ Seller price via Pricing
→ Seller stock via Inventory
→ Storefront compose → Cart → Checkout → Sandbox Payment → Paid
→ Seller fulfillment → Customer tracking → Admin inspect
```

No `Product.Price` / `Product.Stock`. No advanced Variant redesign.

## Files

| # | File | Topic |
|---|---|---|
| 01 | `01-runtime-before-sale-flow.md` | Pre-work triad health |
| 02 | `02-current-sale-capability-matrix.md` | Pre-fix capability audit |
| 03 | `03-sale-blockers.md` | Pre-fix blockers B1–B5 |
| 04 | `04-seller-product-ownership-and-ui.md` | Ownership + vendor routes |
| 05 | `05-seller-offer-flow.md` | Seller Offer create/patch LIVE |
| 06 | `06-real-pricing-path.md` | Pricing write LIVE |
| 07 | `07-real-inventory-path.md` | Inventory write LIVE |
| 08 | `08-storefront-sale-eligibility.md` | Purchasable gate |
| 09 | `09-seller-product-shopeiva-map.md` | Shopeiva vendor map |
| 10 | `10-pdp-live-sale-proof.md` | PDP sale surface LIVE |
| 11 | `11-product-discovery-proof.md` | Listing/search discovery |
| 12 | `12-cart-proof.md` | Cart LIVE |
| 13 | `13-checkout-proof.md` | Checkout LIVE |
| 14 | `14-payment-proof.md` | Sandbox payment LIVE |
| 15 | `15-seller-order-fulfillment-proof.md` | Seller fulfill LIVE |
| 16 | `16-customer-order-proof.md` | Customer order LIVE |
| 17 | `17-admin-sale-operation-proof.md` | Admin inspect LIVE |
| 18 | `18-current-variant-boundary.md` | `ADVANCED_VARIANT_DEFERRED` |
| 19 | `19-real-product-sale-e2e.md` | E2E placeholder steps |
| 20 | `20-browser-evidence.md` | Capture placeholders |
| 21 | `21-visual-regression-audit.md` | Visual lock audit |
| 22 | `22-sale-authorization-isolation.md` | Own/foreign isolation |
| 23 | `23-sale-tests.md` | Test map |
| 24 | `24-final-validation.md` | Validation placeholders |
| 25 | `25-user-preview.md` | Exact preview URLs |
| 26 | `26-commercial-readiness.md` | SELLABLE_DEMO vs PRODUCTION |

## Live entry points

- Seller Offers: `/vendor-panel/products`, `/vendor-panel/products/new`
- Storefront: `/fa/products`, `/fa/products/{slug}`, `/fa/cart`, `/fa/checkout`
- Payment: `/fa/payment/sandbox`, `/fa/payment/result`
- Customer orders: `/customer-panel/orders`
- Admin: `/admin/products`, `/admin/orders`, `/admin/fulfillments`
