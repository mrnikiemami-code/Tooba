# 07 — Support E2E (TB-P06-T029)

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
| Customer create ticket | **201** · ticketId `01a0453b-707d-7000-b7cf-72e428758f43` |
| Admin reply | **200** |

Host: `POST /v1/customer/support/tickets` → `POST /v1/admin/support/tickets/{ticketId}/replies` with admin dev actor.

## FE URLs

| Surface | URL |
| --- | --- |
| Customer tickets | http://localhost:3000/customer-panel/tickets |
| Admin tickets | http://localhost:3000/admin/tickets |
| Notifications | http://localhost:3000/customer-panel/notifications |
