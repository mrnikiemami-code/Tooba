# 26 — User Preview URLs (TB-P06-T029)

Concrete URLs from `commercial-demo.json` plus panel routes required by task AA. No placeholders.

## Dev identities / context

| Role | Id |
| --- | --- |
| Customer | `aaaaaaaa-aaaa-4aaa-8aaa-000000000009` |
| Seller user | `01a03628-3f68-7000-844d-99f1cadb54b0` |
| Seller party | `01a030d1-40cb-7000-8abe-6d31739956c5` |
| Admin actor | `01a036c2-970e-7000-8eb7-94bf5cc2d8db` |

Demo checkout: `01a0453b-6829-7000-8c77-32cfb5f5d409` · return: `4497a586-db39-4134-a90e-7b10a3eedde0` · ticket: `01a0453b-707d-7000-b7cf-72e428758f43`

## Storefront

| Surface | URL |
| --- | --- |
| Home | http://localhost:3000/fa |
| Listing | http://localhost:3000/fa/products |
| Cart | http://localhost:3000/fa/cart |
| Checkout | http://localhost:3000/fa/checkout |
| Blog | http://localhost:3000/fa/blogs |
| Seeded article | http://localhost:3000/fa/blogs/guide-online-shopping |

PDP: open any LIVE offer from Listing (sale E2E used Host cart/checkout APIs for a seeded offer in this run).

## Customer

| Surface | URL |
| --- | --- |
| Dashboard | http://localhost:3000/customer-panel |
| Seeded Order | http://localhost:3000/customer-panel/orders/01a0453b-6829-7000-8c77-32cfb5f5d409 |
| Wallet | http://localhost:3000/customer-panel/wallet |
| Tickets | http://localhost:3000/customer-panel/tickets |
| Notifications | http://localhost:3000/customer-panel/notifications |
| Settings | http://localhost:3000/customer-panel/settings |

## Seller

| Surface | URL |
| --- | --- |
| Dashboard | http://localhost:3000/vendor-panel |
| Products | http://localhost:3000/vendor-panel/products |
| Orders | http://localhost:3000/vendor-panel/orders?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5 |
| Return | http://localhost:3000/vendor-panel/returns/4497a586-db39-4134-a90e-7b10a3eedde0?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5 |
| Access Control | http://localhost:3000/vendor-panel/access-control?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5 |
| Tickets | http://localhost:3000/vendor-panel/tickets |
| Settings | http://localhost:3000/vendor-panel/settings?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5 |

## Admin

| Surface | URL |
| --- | --- |
| Dashboard | http://localhost:3000/admin |
| Orders | http://localhost:3000/admin/orders |
| Access Control | http://localhost:3000/admin/access-control |
| Tickets | http://localhost:3000/admin/tickets |
| Gift Cards | http://localhost:3000/admin/gift-cards |
| Settings | http://localhost:3000/admin/settings |
| Content | http://localhost:3000/admin/content |

## Original Shopeiva (comparison)

| Surface | URL |
| --- | --- |
| Home | http://127.0.0.1:3001/ |
| Checkout / payment | http://127.0.0.1:3001/payment |
| User Panel | http://127.0.0.1:3001/user-panel |
| Vendor Panel | http://127.0.0.1:3001/vendor-panel |
| User settings (ref) | http://127.0.0.1:3001/user-panel/settings |
| Vendor settings (ref) | http://127.0.0.1:3001/vendor-panel/settings |
