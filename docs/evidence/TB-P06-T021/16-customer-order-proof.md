# 16 — Customer order proof (TB-P06-T021)

## Routes

| Surface | URL / API |
|---|---|
| Orders list | `http://127.0.0.1:3000/customer-panel/orders` |
| Order detail | `http://127.0.0.1:3000/customer-panel/orders/{checkoutId}` |
| Host | `GET /v1/customer/orders`, `GET /v1/customer/orders/{checkoutId}` (+ fulfillments under order) |

FE: `customer-panel/orders/**` — payment state badges from Host (`PendingPayment` / `Paid` / …).

## Required behaviors

| Capability | Status | Notes |
|---|---|---|
| See paid Order | LIVE | List + detail after sandbox Succeeded |
| Order details | LIVE | Lines, sellers, amounts, payment state |
| Shipment / tracking / status | LIVE | Customer fulfillments loaded on detail |
| Own Order only | LIVE | Customer scope; foreign checkout denied |
| No fake status | LIVE | Host payment/fulfillment states only |

## Non-sale deferred (not blockers)

Wallet / tickets / notifications / gift-cards remain deferred with honest nav (prior waves).

## Verdict

```text
CUSTOMER_ORDER_TRACKING = LIVE
```
