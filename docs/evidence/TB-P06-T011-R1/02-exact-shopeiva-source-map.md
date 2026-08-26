# 02 — Exact Shopeiva source map (TB-P06-T011-R1)

Reference root: `../SarvNewVerRequirment/reference/shopeiva`

| Tooba file | Shopeiva source | Component | Mapping |
| --- | --- | --- | --- |
| `returns/return-ui.tsx` → `ReturnFormModal` | `dashboard/orders/returnFormModal.jsx` | customer return request | `fixed inset-0 z-[9999] bg-black/60 backdrop-blur-sm`; sticky header + Package icon tile; amber eligibility `bg-amber-50 border-amber-200`; reason `<select>` from `returnReasons`; description min 10 chars; success step with CheckCircle |
| `returns/return-ui.tsx` → `ReturnReviewModal` | `vendor/panel/orders/returnDetailModal.jsx` | seller approve/reject | sticky header; 2-col date/customer grid; reason + description sections; two-step approve/reject (second click executes); reject reason textarea required |
| `returns/return-ui.tsx` → `ReturnDetailCard` | `returnDetailModal.jsx` read sections | detail card | status badge, date/id grid, reason + description blocks, item list `divide-y` |
| `customer-panel/orders/[checkoutId]/page.tsx` | `returnFormModal` entry point | trigger | "درخواست مرجوعی" when fulfillment `Delivered`; passes `orderReference` + line labels |
| `vendor-panel/returns/[returnRequestId]/page.tsx` | `returnDetailModal` entry | seller detail | `ReturnDetailCard` + `ReturnReviewModal` for `Requested` status |

Minor accepted deviation: accent `#2563EB` vs Shopeiva `#E53935`; no dark-mode tokens (Tooba customer/seller surfaces).

Customer route deviation (accepted T022): dedicated order detail page vs Shopeiva modal shell — inner modal structure preserved.

## Backend map (restock)

| Tooba | Contract | Behavior |
| --- | --- | --- |
| `ReturnDirectory` (on refund success) | `IReturnInventoryGateway` | calls restock per return line with reservation |
| `ReturnInventoryGateway` | `IInventoryReturnGateway` | module boundary adapter |
| `InventoryReturnGateway` | schema `inventory` | `Consumed` → `AdjustAsync(Increase)`; `Held` → `ReleaseAsync`; idempotent via `return_restock_inbox` |
