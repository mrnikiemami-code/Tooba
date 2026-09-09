# Action matrix — TB-P09-T017

Runtime:

| State | Whole-order actions |
| --- | --- |
| Waiting manual payment | confirm_deposit, reject_deposit, cancel |
| ReadyToFulfill | unconfirm_deposit, cancel; processing stays in اقلام و ارسال |
| Packed + Created+tracking | one cancel |
| Dispatched/Delivered | cancel absent; POST `order.cancel.forbidden` |
| Cancelled paid | restore_cancelled_order only |
| Multi-seller | one cancel after FE/BE whole-order collapse; fulfillment codes not in kebab |

Invalid actions are omitted, not shown disabled.
