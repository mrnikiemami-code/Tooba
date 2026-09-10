# TB-P09-T022-R1 — customer fulfillments / FE routes

## Host probe

```http
GET /v1/customer/orders/{checkoutId}/fulfillments
Host: alpha.localhost
X-Tooba-Guest-Secret: {guestSecret}
```

Fixture order `01a0898a-0d7d-7000-b5ed-7f5d85a29241` → **HTTP 200** with `guestSecret` from `r1-fixture.json`.

- Seller fulfillments + shipments as recorded under `r1-fixture.json` → `customerFulfillmentsProbe`.
- After Admin create in R1 visual smoke: package `MP-01A0898C6B73` present; customer projection uses central tracking when package is active (T021-R1 rules preserved).

## FE customer order surface

| Surface | Path | HTTP |
| --- | --- | --- |
| Order detail + shipping | `http://127.0.0.1:3000/fa/customer-panel/orders/01a0898a-0d7d-7000-b5ed-7f5d85a29241` | **200** |

Implementation:

- Page: `src/frontend/app/customer-panel/orders/[checkoutId]/page.tsx`
- Loads order via `loadCustomerOrderDetail`, fulfillments via `loadCustomerFulfillments`
- BFF: `GET /api/customer/orders/{checkoutId}/fulfillments` (forwards `X-Tooba-Guest-Secret` from cart session)

Guest browser view needs the cart session `guestSecret` from `r1-fixture.json` (same secret used for the Host probe).

Admin package work for the same fixture:

```text
http://127.0.0.1:3000/fa/admin/orders/01a0898a-0d7d-7000-b5ed-7f5d85a29241
```

## Checks

- No FE 500 on supported customer-panel route when Next is Ready
- Host guest fulfillments 200 with fixture guest secret
- `USER_VISUAL_ACCEPTED=NO`
