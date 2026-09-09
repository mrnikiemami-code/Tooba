# R2 Regression — TB-P09-T020-R2

Preserved:

- T016 cancel-after-dispatch blocked
- T020 remainder pack after partial dispatch
- T009/T009-R1 restore gates (refund/payout)
- Cancelled shipment not resurrected
- Inventory Held-only consume invariant

Changed expectation: after Cancel→Restore, Fulfillment reservation_id matches OrderLine replacement Held id.
