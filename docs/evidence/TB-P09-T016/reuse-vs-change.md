# Reuse vs change — TB-P09-T016

| Area | Decision |
| --- | --- |
| Policy / domain cancel | Reuse T009 |
| Composer CancelAsync / projection | Reuse; confirm + human FA updated |
| Fulfillment abort / restore | Reuse; `HasDispatchedQuantity` aligned to unit status |
| Inventory release | Reuse `ReleaseAsync` |
| Payment close / refund | Reuse existing workflow |
| Settlement neutralize | Reuse existing debit |
| Restore gates | Reuse T009 / T009-R1 |
| Whole-order cancel UI | Reuse one `لغو سفارش` + Dialog |
| Grid/detail refresh | Reuse reloadToken / refresh |
| Seller/line partial cancel | Out of scope — not added |
