# R1 — Seller-settled return runtime (tooba_alpha)

Edition **SingleStore**: Settlement `PaymentSucceeded`/`RefundSucceeded` handlers are Marketplace-gated. Accrual/adjust used application APIs (`ISettlementDirectory.AccrueFromPaymentAsync` / `AdjustFromRefundAsync`) — no SQL mutation of business results.

## Scenario order

| Field | Value |
|-------|-------|
| sellerOrderId | `01a0451c-fac1-7000-9371-f9d9ebb05855` |
| checkoutId | `01a0451c-fabf-7000-99f0-30d410a58638` |
| paymentId | `88ddad77-9a7f-4e51-8ed9-12e45ecbf791` |
| fulfillmentId | `0bbc49f9-c09d-481b-95be-69241b6e12be` |
| shipmentId | `5d057d5f-87a2-47e3-b3c9-a7f6644b9174` |

1. Host ops: mark_processing → packed → create_shipment → assign_tracking → dispatch → deliver.
2. Return eligibility: `eligible=true` (window open).
3. Accrue credit via app path (opt-in `TOOBA_R1_LIVE=1` smoke): Credit `63aca581-f470-417f-8042-1cd231062d33` gross `381500` commission `38150` net `343350`.
4. Eligibility still `eligible` after settlement credit (settlement does not gate returns).
5. Host `request_return` → return `fbdc0eec-ede0-4deb-9735-ecf1f95b9724` refundAmount `350000`.
6. Host `approve_return` → status `Completed`, refund attempt Succeeded.
7. `AdjustFromRefundAsync` (+ retry): Debit `8e7e6288-b2ec-46a8-a479-8bbda1f0d0bd` net `315000`; credit row unchanged.
8. Seller payable net sum: `28350` (= `343350 - 315000`).
