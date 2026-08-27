# 15 — Seller order / fulfillment proof (TB-P06-T021)

## Routes

| Surface | URL / API |
|---|---|
| Seller orders list | `http://127.0.0.1:3000/vendor-panel/orders` |
| Seller order detail | `http://127.0.0.1:3000/vendor-panel/orders/{sellerOrderId}` |
| Seller fulfillments | `http://127.0.0.1:3000/vendor-panel/fulfillments` |
| Seller fulfillment detail | `http://127.0.0.1:3000/vendor-panel/fulfillments/{fulfillmentId}` |
| Host orders | `GET /v1/seller/orders`, `GET /v1/seller/orders/{sellerOrderId}` |
| Host fulfillment | Seller fulfillment endpoints under Fulfillment module (`FulfillmentEndpoints.cs`) |

## After Paid

| Capability | Status | Notes |
|---|---|---|
| See own order / lines | LIVE | Scoped by authenticated `SellerPartyId` |
| Foreign seller order data | DENIED | Composer isolation — other seller’s private orders not listed |
| Start fulfillment / processing | LIVE | Auto unit on payment + seller processing/packed |
| Shipment + tracking | LIVE | Create shipment, set tracking, dispatch/deliver |
| Fake status | FORBIDDEN | Status from Host fulfillment state machine only |

## Tie to T021 sale E2E

Seller A’s Offer (created with price+stock) → customer Paid checkout → Seller A sees `sellerOrderId` for own lines only → fulfillment/tracking updates visible to customer (`16`).

## Verdict

```text
SELLER_ORDER_FULFILLMENT = LIVE
```
