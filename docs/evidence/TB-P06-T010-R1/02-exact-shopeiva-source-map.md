# 02 — Exact Shopeiva source map (TB-P06-T010-R1)

Reference root: `SarvNewVerRequirment/reference/shopeiva`

| Tooba file | Shopeiva source | Component | Mapping |
| --- | --- | --- | --- |
| `customer-panel/orders/[checkoutId]/page.tsx` | `components/dashboard/orders/orderDetailModal.jsx` | shipping block | `bg-gray-50 rounded-xl p-4 space-y-2` + MapPin/Phone/Package rows; tracking `font-mono font-bold` |
| `fulfillment/fulfillment-ui.tsx` | `orderDetailModal.jsx` L176-198 | `FulfillmentShippingInfoBlock` | direct port of shipping info DOM/CSS |
| `fulfillment/fulfillment-ui.tsx` | `orderDetailModal.jsx` L132-165 | shipment rows | `hover:bg-gray-100 transition-colors` product-row pattern |
| `vendor-panel/fulfillments/page.tsx` | `vendor/panel/orders/ordersList.jsx` | list density | Tooba uses accepted Offer/Orders DataGrid pattern (T023) — same shell/tokens |
| `vendor-panel/fulfillments/[fulfillmentId]/page.tsx` | `vendor/panel/orders/orderDetail.jsx` | detail + actions | 4-up stat grid, section cards, `transition-colors` buttons |
| `admin/fulfillments/*` | no dedicated Shopeiva Admin | basis = T024 Admin ops + seller order detail cards | explicit no-equivalent; Admin grid/shell from accepted Tooba Admin |

Minor technical deviation (accepted across P05): accent `#2563EB` vs Shopeiva `#E53935`.

Customer route deviation (accepted T022): dedicated `/customer-panel/orders/[checkoutId]` page vs Shopeiva modal — structure inside seller-order card preserved.
