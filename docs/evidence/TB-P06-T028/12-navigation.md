# 12 — Navigation / notifications / preview

Task: TB-P06-T028 (frontend)

## Customer navigation (unchanged live)

- `/customer-panel/wallet` — ledger (includes debit/refund labels)
- `/customer-panel/notifications` — inbox
- `/customer-panel/orders/{checkoutId}` — order detail / return entry
- `/checkout` → `/order/confirmation?checkoutId=` → `/payment/result?paymentId=&checkoutId=`

## Notifications

`notification-inbox.tsx` `visualFor`:

- Any `type` containing `wallet`, plus known keys
  `wallet.payment.succeeded`, `wallet.refund.credited`,
  `wallet.gift_card.redeemed`, `wallet.admin_adjustment`
- Renders Wallet icon + violet styling
- Host title/body copy used when present; generic fallback still OK

## Preview steps (Dev)

1. Identity: customer actor `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`
2. Ensure wallet balance covers a small order (Host seed / gift redeem)
3. Add to cart → `http://localhost:3000/checkout` → submit
4. `http://localhost:3000/order/confirmation?checkoutId=<id>` — select کیف پول if shown → pay (no sandbox)
5. Result: `http://localhost:3000/payment/result?paymentId=…&checkoutId=…` then wallet ledger debit label
6. Return flow: open return form → destination Wallet → approve with destination
7. Notifications: `http://localhost:3000/customer-panel/notifications`
8. Shopeiva compare: `http://127.0.0.1:3001` payment methods / return policy wallet destination

## Mixed tender

Status for UI: **DEFERRED** — not selectable; deferred note only.
