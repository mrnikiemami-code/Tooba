# 03 — Navigation / URL integrity (HTTP)

| Surface | URL | HTTP |
| --- | --- | --- |
| home | $(home http://localhost:3000/fa[1]) | 200 |
| listing | $(listing http://localhost:3000/fa/products[1]) | 200 |
| blogs | $(blogs http://localhost:3000/fa/blogs[1]) | 200 |
| cart | $(cart http://localhost:3000/fa/cart[1]) | 200 |
| checkout | $(checkout http://localhost:3000/fa/checkout[1]) | 200 |
| customer | $(customer http://localhost:3000/customer-panel[1]) | 200 |
| customer-orders | $(customer-orders http://localhost:3000/customer-panel/orders/01a0453b-6829-7000-8c77-32cfb5f5d409[1]) | 200 |
| customer-wallet | $(customer-wallet http://localhost:3000/customer-panel/wallet[1]) | 200 |
| customer-tickets | $(customer-tickets http://localhost:3000/customer-panel/tickets[1]) | 200 |
| vendor | $(vendor http://localhost:3000/vendor-panel[1]) | 200 |
| vendor-orders | $(vendor-orders http://localhost:3000/vendor-panel/orders?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5[1]) | 200 |
| vendor-acl | $(vendor-acl http://localhost:3000/vendor-panel/access-control?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5[1]) | 200 |
| admin | $(admin http://localhost:3000/admin[1]) | 200 |
| admin-orders | $(admin-orders http://localhost:3000/admin/orders[1]) | 200 |
| admin-acl | $(admin-acl http://localhost:3000/admin/access-control[1]) | 200 |
| admin-tickets | $(admin-tickets http://localhost:3000/admin/tickets[1]) | 200 |
