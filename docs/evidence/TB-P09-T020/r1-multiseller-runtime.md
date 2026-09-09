# R1 Multi-Seller Runtime — TB-P09-T020-R1

Host `:5088` `Host: alpha.localhost`. Script `docs/evidence/TB-P09-T020/_r1_runtime.mjs`. Raw `r1-runtime-raw.json` `ok: true`.

Checkout `01a0864a-f248-7000-92d9-426915151d95`: KG offer + Arman listing offer; `sellerCount: 2`; `distinctSellers: true`.

| Step | Result |
| --- | --- |
| Probe B_OFFER fixture | 404 `cart.missing` (fixture, documented) |
| Add listing Arman | 200 |
| KG first dispatch 0.50 | `PartialDispatched`; pack after; cancel 400 |
| Seller B after A dispatch | `ReadyToFulfill`; separate fulfillment (`differentFulfillment: true`) |
| Pack/ship B | 200; B `Packed` while KG remainder still open |
| Remainder + B dispatch | KG `Dispatched` (not `Delivered`); B dispatched independently |
| Deliver A+B + return 0.25 | 200 |
| T016 cancelled paid `01a084cc-8be3-7000-bbcf-9fb2cfb1cc2c` | no `request_return` |

No cross-seller shipment. A dispatch does not mark B dispatched.
