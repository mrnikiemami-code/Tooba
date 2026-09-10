# Order Ownership — TB-P10-T004-R1

- Owner A `GET /v1/customer/orders/{id}` → 200 (Bearer; no guest secret required on customer path)
- Customer B → 404
- Unauthenticated → 404
