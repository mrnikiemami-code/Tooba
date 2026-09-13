# TB-P10-T004-R24 — Lifecycle matrix

| Stage | Identity | Cart state | Order state | Payment state | Reservation cycle | Open-unpaid | Churn | Customer action | Admin action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Browse / ATC | Anonymous allowed | Active guest | none | none | none | 0 | 0 | add line | — |
| Shipping/checkout API | Anonymous | Active | none | none | none | 0 | 0 | blocked 401 | — |
| OTP login | Session | guest still Active | pending Orders unchanged | none | none | unchanged | unchanged | `/fa/login` `09111111111`/`123456` | — |
| Cart merge | Authenticated | lines merged server-side | none created | none | none | unchanged | unchanged | `POST /cart/merge` | — |
| Cycle #1 commit | Authenticated | Converted on success | SellerOrder PendingPayment | none yet | Cycle #1 | +1 | +1 immutable event | shipping commit | settings apply to future only |
| Inventory fail | Authenticated | Active intact | none | none | none | unchanged | unchanged | commit 400 `checkout.rejected` | — |
| Open 2/2 | Authenticated | Active intact on block | two open | none | existing | 2 | 2 | third 409 open_unpaid | — |
| Hide | Authenticated | n/a | still PendingPayment | n/a | may expire for hide UX | still counts | unchanged | hide-pending-card | — |
| Cancel | Authenticated | n/a | Cancelled | closed | released | −1 | not refunded | cancel | audit remains |
| Churn max | Authenticated | Active intact | none new | none | none | open may be 0 | 3/3 | 409 reservation_commit | — |
| Payment retry | Authenticated | Converted | same Order | new attempt | same cycle | same | unchanged | payments POST | — |
| Cycle #2 | Authenticated | Converted | same Order | retryable | Cycle #2 | same | still 1 event/order | retry-unpaid | — |
| Manual AwaitingAdmin | Authenticated | Converted | PendingPayment | manual pending | Cycle #1 | counts | one event | providerCode manual | confirm later |
| Paid | Authenticated | Converted | Paid | Succeeded | closed | 0 for that Order | event remains in window | sandbox complete | — |
| Logout | revoked bearer | account cart not leaked | no leak | — | — | n/a | n/a | logout 204 then cart 401 | — |
