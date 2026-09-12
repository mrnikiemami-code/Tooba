# TB-P10-T004-R5 — Review Hold Policy

| Phase | Expiry |
| --- | --- |
| Cart / checkout | Cart HoldTtl (30 minutes) |
| Manual payment review | `ManualPaymentReviewHoldHours` (default 24h, clamp 1..720) |
| Paid order | `ExpiresAt = null` (durable) |
| Rejected / expired review | Released; never resurrected |

Config: `Payment:Gateway:ManualPaymentReviewHoldHours` in Host appsettings.
