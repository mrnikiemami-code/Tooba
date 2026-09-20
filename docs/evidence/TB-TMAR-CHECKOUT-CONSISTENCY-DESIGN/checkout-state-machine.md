# Checkout process state machine

Separate from SellerOrder business status.

| State | Entry | Next | Timeout | Compensation | Terminal | User-visible |
|---|---|---|---|---|---|---|
| Started | Submit accepted / idempotency miss | Validating | short | none | no | processing |
| Validating | quotes/offer/tax/promo/cart checks | InventoryReserving / Failed | short | none | no | validating |
| InventoryReserving | reserve commands | OrderPersisting / Compensating | hold policy | ReleaseInventory | no | reserving |
| OrderPersisting | order write | CartCommitting / Compensating | short | CancelOrder+Release | no | creating order |
| CartCommitting | convert cart | PaymentPending / Compensating | short | RestoreCart if policy allows | no | committing |
| PaymentPending | after durable commit | PaymentAuthorized / Failed / ManualReview | unpaid expiry | cancel unpaid + release | no | awaiting payment |
| PaymentAuthorized | PSP/wallet success | Completed | n/a | refund path separate | no | paid |
| Completed | fulfillment handoff eligible | — | — | — | yes | placed |
| Compensating | any mid-flight failure | Failed / ManualReview | bounded | step compensations | no | reversing |
| Failed | terminal failure | — | — | — | yes | failed |
| ManualReview | compensation/payment ambiguity | Failed / Completed | ops SLA | ops | no | review |

JSON: checkout-state-machine.json
