# TB-P10-T004-R13 — Browser navigation

| Case | Behavior |
| --- | --- |
| Refresh `/fa/payment` after rotation | committed proof remains; checkout GET authorized |
| Home tab AddToCart | `ensureStorefrontCart` creates/uses Active Cart ≠ converted source |
| Return to Payment tab | same committed proof; no new Order |
| Back to `/cart` | `loadStorefrontCart` hides Converted; empty or new Active |
| Result poll | `shouldPollStorefrontPayment` + inFlight; no new interval storm |
| Duplicate submit | shipping/checkout idempotency keys unchanged |

No duplicate Order/Payment from navigation effects.
