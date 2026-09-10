# R1 Persistence / Handoff — Multi-Seller

Cart `01a08c19-2599-7000-b43d-907a228e9a2a` (`sellerCount=2`, `maxPrep=3`).

| Step | Result |
|---|---|
| Save selection (`post:express`, min date, note `r1-handoff`) | **200** |
| Re-project / refresh draft | method + date + note retained |
| `POST /v1/storefront/shipping/commit` | **200** |
| Checkout id | `01a08c19-2975-7000-8a86-4ca0a327dcab` |
| Snapshotted shipping amount | **200000** |
| Method label | پست — پیشتاز |
| FE `/fa/payment` | **200** (template handoff; Payment not implemented — T003 not started) |

No payment methods added. Authoritative shipping state carried into checkout.

Raw step: `handoff` in `r1-runtime-raw.json`.
