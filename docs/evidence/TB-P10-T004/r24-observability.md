# TB-P10-T004-R24 — Observability

| Signal | Where |
| --- | --- |
| `checkout.authentication_required` | Host 401 + FA «برای ادامه فرایند خرید وارد حساب خود شوید.» |
| `checkout.open_unpaid_limit_reached` | 409 + `CheckoutAbuseBlockEvent` kind `open_unpaid` |
| `checkout.reservation_commit_limit_reached` | 409 + nextAvailableAt + block event `reservation_commit` |
| customer cancel | cancel 200 + SellerOrder Cancelled |
| pending hide | hide-pending-card 200/409 |
| `CheckoutReservationCommit` | `order.checkout_reservation_commits` |
| reservation cycles | `order.reservation_cycles` |

UI localizes. Backend/audit keep stable codes.
