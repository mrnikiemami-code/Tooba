# Settings

| Key | Meaning |
| --- | --- |
| `Cart:PersistenceHours` (default 168) | Abandoned cart retention. Not a reservation TTL. |
| `Payment:Gateway:OnlinePaymentHoldHours` | Order hold after online commit/start |
| `Payment:Gateway:ManualPaymentInitialHoldHours` | Order hold waiting for evidence |
| `Payment:Gateway:ManualPaymentReviewHoldHours` | R5 review hold |
| `Payment:Gateway:OrderSupplyHoldOverrides.*` | Store override |
| `Payment:Gateway:CartHoldMinutes` | Legacy; unused for reservation start |

Precedence: store override > platform Payment:Gateway / Cart section. Initial order hold = max(online, manual initial) via `CheckoutReservationHoldPolicy`.
