# TB-P10-T004-R10 — Timeout policy

| Logical setting | Keys | Default | Range |
| --- | --- | --- | --- |
| Cart persistence | `Cart:PersistenceHours` then store `CartPersistenceHours` | 168 hours | 1–2160 |
| Online unpaid / Order hold | `Payment:Gateway:OnlinePaymentHoldHours` + store + method | 2 hours | 1–720 |
| Manual initial | `ManualPaymentInitialHoldHours` + store + method | 2 hours | 1–720 |
| Manual review | `ManualPaymentReviewHoldHours` + store + method | 24 hours | 1–720 |

Resolver: `CommerceHoldPolicy`. Checkout initial hold remains `max(online, manual initial)`. Unpaid timeout uses the method actually initiated (`manual` vs online/fake).

No magic TTL in worker/UI. Cart persistence never creates inventory holds.
