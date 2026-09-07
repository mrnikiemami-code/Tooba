# R1 Runtime — Pre-Dispatch Cancel + Replacement (Scenario E)

| Step | Result |
| --- | --- |
| `cancel_shipment` projected | yes |
| cancel `d8fee687-…` | HTTP 200 |
| old shipment status | **Cancelled** (auditable, not erased) |
| allocations released | quantityAllocated → 0 on previously allocated line |
| replacement create_shipment qty=2 | HTTP 200 |
| replacement shipmentId | `8fdd9430-d547-4490-a5c6-785c80e48cbb` status=Created |

## Scenario D — Unpack blocked when fully allocated

Separate proof: allocate **all** packed qty of line `…ae0b…` (q=2,p=2,a=2) via shipment `eb75734b-…`.

`unpack` qty=1 → HTTP 400  
FA: `بازگشت از بسته‌بندی برای تعداد تخصیص‌یافته یا ارسال‌شده مجاز نیست.`  
State unchanged: packed=2 allocated=2.

Note: unpack of **unallocated remainder** while another qty is allocated remains allowed by T005 domain rules (not a defect).
