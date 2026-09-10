# R1 Delivery Minimum — Runtime

Formula (unchanged from T002):

```text
minimumDeliveryDate = today(UTC)
  + max(sellerPreparationDays across distinct SellerPartyIds)
  + methodLeadDays(selected method)
```

Runtime (multi-seller, `post:express`):

| Input | Value |
|---|---|
| Seller A prep | 1 |
| Seller B prep | 3 |
| `maxSellerPreparationDays` | **3** |
| Method lead | **2** (`post:express`) |
| Today (UTC) | 2026-09-10 |
| Expected minimum | **2026-09-15** |

Proofs on Host `:5088`:

| Case | Request date | Result |
|---|---|---|
| Forged earlier | `2026-09-14` | **400** `shipping.delivery.too_early` |
| Exact minimum | `2026-09-15` | **200** accepted |
| Later | `2026-09-17` | **200** accepted |

UI date list starts at `2026-09-15` (no earlier card). Not unit-test-only.

Raw step: `delivery-minimum` in `r1-runtime-raw.json`.
