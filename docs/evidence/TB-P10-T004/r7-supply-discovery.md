| Operation | Requires guaranteed supply? | Current (pre-R7) | Reacquire allowed? | Canonical target |
| --- | --- | --- | --- | --- |
| Confirm deposit | yes | CommitOrReacquire in OrderPaymentBridge | yes | EnsurePaidDurable |
| Late confirm | yes | Reserve+commit | yes | EnsurePaidDurable |
| Manual evidence | yes (review hold) | Promote / reacquire review TTL | yes | EnsureReviewHold |
| R6 recover | yes | RecoverAsync Reserve | yes (A/B) | Ensure* via policy |
| Restore cancelled | yes if restore | CheckoutDirectory Reserve | yes if restore policy | EnsurePaidDurable |
| Dispatch/consume | yes (existing hold) | ConsumeAsync | no new unless existing | EnsureFulfillmentSupply later |
| CheckOnly Admin | no mutation | none | no | GetOrderSupplyStatus |
| Cancelled/refunded/fulfilled | no | N/A | no | NotApplicable |
