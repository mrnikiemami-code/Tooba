# TB-P04-T010 — duplicate initiation

Two `POST /v1/storefront/checkout/{checkoutId}/payments` with the same guest cart and the same `idempotencyKey` returned one Payment.

| Field | Result |
| --- | --- |
| CheckoutId | `01a035ce-401f-7000-b62c-1f3d3cae9d7f` |
| PaymentId (1st = 2nd) | `d46138d7-24d1-4d47-8e5f-37dd0697fa7c` |
| AttemptId | same on replay |
| RedirectUrl | same on replay |
| Amount / Currency | `1951100` / `IRR` |

UI stores `sessionStorage` key `tooba.storefront.paymentIdempotency.{checkoutId}` so double-click reuses the same key.
