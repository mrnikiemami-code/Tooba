# Count vs quantity discovery — TB-P09-T014-R1

Locks read: `docs/architecture/TOOBA-LOCKS.md` LOCK-QTY-001, LOCK-INVOICE-002.

Header already stores both:

- `SellerOrder.TotalItemCount` (int, line COUNT)
- `SellerOrder.TotalQuantity` (decimal, SUM of product quantities)

Defect: Admin list/detail aliased quantity into `LineCount`.

| Surface | Before | After |
| --- | --- | --- |
| `AdminOrderListItem.LineCount` | `decimal`, fed from `TotalQuantity` / line qty SUM | `int` from `TotalItemCount` |
| Grid filter/sort `lines` | `Sum(TotalQuantity)` | `Sum(TotalItemCount)` |
| `AdminOrderDetailPage.LineCount` | `Sum(line.Quantity)` | header `TotalItemCount` |
| Seller financial `LineCount` | `Sum(line.Quantity)` | header `TotalItemCount` |
| Invoice HTML | تعداد اقلام = count (already); جمع مقدار always shown | count integer; جمع مقدار only when units are shared |
| FE enrich fallback | summed `line.quantity` | `lines.length` |

No Duty calculator added. No T015. No UoM/Product/Offer/rounding change.
