# R4 Runtime — Decimal qty 1.25 (Scenario C)

Host runtime proof via `_r4_runtime.mjs` / `_r4_runtime_raw.json`.

## IDs

| Field | Value |
| --- | --- |
| checkoutId | `01a08b4b-19a3-7000-8e6b-7195437bf1e2` |
| cart / paid / released | `01a08b4b-191d-7000-8c3a-5a881609100e` |
| restored / consumed | `01a08b4c-a98b-7000-97f0-f71515506f43` |
| originalExpiresAt | `2026-09-10 12:30:39.75826+00` |
| dispatch | 200 |

## Quantity chain (exact)

`1.250000` → pay → expiry → cancel → restore → dispatch:

```text
["1.250000","1.250000","1.250000","1.250000","1.250000","1.250000"]
```

| Phase | at (UTC) | status | ExpiresAt | qty |
| --- | --- | --- | --- | --- |
| beforePay | 2026-09-10T12:29:09.900Z | Held | cart TTL | 1.250000 |
| afterPay | 2026-09-10T12:29:10.257Z | Held | **null** | 1.250000 |
| afterExpiry | 2026-09-10T12:30:50.997Z | Held | null | 1.250000 |
| afterCancel | 2026-09-10T12:30:51.986Z | Released | null | 1.250000 |
| afterRestore | 2026-09-10T12:30:53.299Z | Held | **null** | 1.250000 |
| afterDispatch | 2026-09-10T12:30:55.253Z | Consumed | null | 1.250000 |

No truncation. OrderLine/Fulfillment aligned on restore and consume.

## Verdict

**PASS**
