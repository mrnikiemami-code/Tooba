# Runtime smoke — TB-P09-T021-R1

Script: `_r1_runtime.mjs` · Raw: `r1-runtime-raw.json`

| Case | Result |
| --- | --- |
| Guest secret + active package primary | PASS 200 / CENTRAL-R1-A2 |
| Rebuild replaces cancelled primary | PASS MP-01A08967794E |
| GuestActor alone | PASS 404 |
| Wrong secret | PASS 404 |
| Unowned actor | PASS 404 |
| Dispatch/Deliver coherent | PASS Delivered |
| Owned actor (`PlacedByUserId`) | PASS 200 / CENTRAL-R1-B |
| No package | PASS preferred null |

Endpoint: `GET /v1/customer/orders/{checkoutId}/fulfillments` with `X-Tooba-Guest-Secret` and/or owned `X-Tooba-Dev-Actor-User-Id`.
