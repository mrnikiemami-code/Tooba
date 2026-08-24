# TB-P04-T009 repair — duplicate submit

Live Host `http://localhost:5088` via Next rewrite. Guest cart, no secrets recorded.

| Field | Value |
| --- | --- |
| CartId | `01a0351e-8647-7000-b788-c4ac61869ec5` |
| Cart version at replay | 4 |
| IdempotencyKey | `d246ad5e-ecf8-4514-bd76-8e684c4d8052` (session key only; not a payment token) |
| First POST `/v1/storefront/checkout` | HTTP 200, CheckoutId `01a03523-e0e8-7000-86bb-d46c294ce935`, `PendingPayment` |
| Second POST same CartId + same IdempotencyKey | HTTP 200, CheckoutId `01a03523-e0e8-7000-86bb-d46c294ce935`, `PendingPayment` |

Same CheckoutId both times. One CartId → one CheckoutGroup. Orders remain `PendingPayment`. No paid success.
