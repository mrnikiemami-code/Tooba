# 05 — Seller fulfillment E2E (TB-P06-T029)

Host APIs only · `directDbMutation: false` · same commercial order as `04-storefront-sale-e2e.md` · artifact: `commercial-demo.json`

## Identity

| Role | Id |
| --- | --- |
| Customer | `aaaaaaaa-aaaa-4aaa-8aaa-000000000009` |
| Seller user | `01a03628-3f68-7000-844d-99f1cadb54b0` |
| Seller party | `01a030d1-40cb-7000-8abe-6d31739956c5` |
| Admin actor | `01a036c2-970e-7000-8eb7-94bf5cc2d8db` |

## Proven on order

| Field | Value |
| --- | --- |
| Checkout / order | `01a0453b-6829-7000-8c77-32cfb5f5d409` |
| Fulfillment id | `9fb87e6f-6d55-4ad5-8d95-dafc082b26d6` |
| Order line id | `01a0453b-6859-7000-89a8-f6194cd4e71d` |
| Shipment id | `b479d1d7-678f-4e07-98dd-953bf37c1f17` |
| Deliver | **true** (`fulfillDeliver`) |

Seller Host path (dev actor + `X-Tooba-Seller-Party-Id`): processing → packed → create shipment → tracking → dispatch → deliver.

## FE URLs

| Surface | URL |
| --- | --- |
| Seller orders | http://localhost:3000/vendor-panel/orders?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5 |
| Customer order | http://localhost:3000/customer-panel/orders/01a0453b-6829-7000-8c77-32cfb5f5d409 |
| Notifications | http://localhost:3000/customer-panel/notifications |
