# 06 — Return / refund E2E (TB-P06-T029)

Host APIs only · `directDbMutation: false` · artifact: `commercial-demo.json`

## Identity

| Role | Id |
| --- | --- |
| Customer | `aaaaaaaa-aaaa-4aaa-8aaa-000000000009` |
| Seller user | `01a03628-3f68-7000-844d-99f1cadb54b0` |
| Seller party | `01a030d1-40cb-7000-8abe-6d31739956c5` |
| Admin actor | `01a036c2-970e-7000-8eb7-94bf5cc2d8db` |

## Recorded results

| Step | Status / id |
| --- | --- |
| Destination | **Wallet** (`destination` / `refundDestination`) |
| Create return | 200 · returnRequestId `4497a586-db39-4134-a90e-7b10a3eedde0` |
| Seller approve | **200** |
| Approve retry (idempotency / already-approved) | **400** |
| Line | `01a0453b-6859-7000-89a8-f6194cd4e71d` on delivered fulfillment |

Wallet balances on same run: before pay **1316500** → after journey (incl. Wallet refund credit) **1285000**.

## FE URLs

| Surface | URL |
| --- | --- |
| Seller return | http://localhost:3000/vendor-panel/returns/4497a586-db39-4134-a90e-7b10a3eedde0?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5 |
| Customer order | http://localhost:3000/customer-panel/orders/01a0453b-6829-7000-8c77-32cfb5f5d409 |
| Wallet | http://localhost:3000/customer-panel/wallet |
