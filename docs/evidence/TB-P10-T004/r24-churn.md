# TB-P10-T004-R24 — Churn

`CheckoutReservationCommit` unique per checkout (`ux_checkout_reservation_commits_checkout`). Window query `customer_id + occurred_at` (`ix_checkout_reservation_commits_customer_occurred`) plus StoreId filter. Defaults 30 minutes / 3.

| Case | Result |
| --- | --- |
| Cycle #1 | A-cycle1-event exactly one row |
| Cancel | G-cancel-no-refund history kept |
| Hide | no delete |
| Payment success | event remains; slot frees via Order Paid |
| Payment retry | I-retry-no-churn 25→25 |
| Cycle #2 | J-cycle2-no-new-commit events=1 |
| Max | H-churn-blocked 409 `checkout.reservation_commit_limit_reached` nextAvailableAt |
| Inventory rollback | F commits 22→22 |
| Age-out | script aged rows `-2 hours` then new commits succeeded |

Offer-level `MaxReservationCommitsPerCustomerPerOfferInWindow` not implemented (future only).
