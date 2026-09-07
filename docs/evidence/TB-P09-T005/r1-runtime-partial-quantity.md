# R1 Runtime — Partial Quantity (Scenario A)

Checkout: `01a07a63-adea-7000-b184-277160114a48`  
Seller: فروشگاه توبا مارکت / `01a07a63-ae04-7000-a7f5-7f3ea6bd47e2`  
Fulfillment: `0d651beb-b8e2-49c9-8e62-ae22929cd9f6`

| Field | Value |
| --- | --- |
| orderLineId | `01a07a63-ae0e-7000-9acf-8176b7c333bf` |
| ordered qty | 5 |
| packed before | 0 |
| pack selection | 2 |
| packed after | 2 |
| remaining eligible | 3 |
| fulfillmentStatus | Packed |

Later: remaining 3 packed via second `mark_packed` selection → packed=5 (A “remaining can be packed later”).

API: `POST /v1/admin/orders/{checkout}/operations` with `selections[{orderLineId,quantity}]`.
