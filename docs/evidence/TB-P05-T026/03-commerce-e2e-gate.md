# 03 — Commerce E2E gate (TB-P05-T026)

Live flow via Next rewrite → Host (guest cart).

## Path

Home/Listing → PDP `demo-game-2` → Offer `01a03826-b318-7000-b6b6-aa85026be261` → Cart → Checkout (guest address) → Payment sandbox success → Confirmation

## Verified fields

| Field | Value |
|---|---|
| OfferId | 01a03826-b318-7000-b6b6-aa85026be261 |
| CartId | 01a03ef2-4d30-7000-bb7e-bfc91ba645be |
| CheckoutId | 01a03ef2-4d7c-7000-a47a-deee181523cd |
| PaymentId | 576cd36d-2258-461b-b154-9fccc36f109c |
| Payable | 1918400 |
| Tax | 158400 |
| Checkout status (pre-pay) | null |
| Payment state (pre-pay) | PendingPayment |
| Payment after sandbox | {"status":"Succeeded","paymentState":null} |
| Confirmation status | null |
| Confirmation payment | Paid |
| Shipping recipient snapshot | خریدار آزمایشی گیت |

## Assertions

- Pricing/inventory/tax owned by Host (not frontend invention)
- Guest shipping snapshot immutable on confirmation
- Sandbox payment completed through Host verify path
- Result: **PASS**

```json
{
  "offerId": "01a03826-b318-7000-b6b6-aa85026be261",
  "cartId": "01a03ef2-4d30-7000-bb7e-bfc91ba645be",
  "checkoutId": "01a03ef2-4d7c-7000-a47a-deee181523cd",
  "paymentId": "576cd36d-2258-461b-b154-9fccc36f109c",
  "attemptId": "f3a10a5a-c899-4741-824e-da64edb20ea0",
  "payable": 1918400,
  "tax": 158400,
  "checkoutStatusBeforePay": null,
  "paymentStateBeforePay": "PendingPayment",
  "paymentAfter": {
    "status": "Succeeded",
    "paymentState": null
  },
  "confirmation": {
    "status": null,
    "paymentState": "Paid",
    "recipientName": "خریدار آزمایشی گیت",
    "payableAmount": 1918400,
    "taxAmount": 158400
  },
  "redirectUrl": "/payment/sandbox?paymentId=576cd36d-2258-461b-b154-9fccc36f109c&attemptId=f3a10a5a-c899-4741-824e-da64edb20ea0&ref=fake-576cd36d2258461bb1549fccc36f109c&checkoutId=01a03ef2-4d7c-7000-a47a-deee181523cd"
}
```
