# 04 — Seller fidelity proof (TB-P06-T010-R1)

## Source basis

- List: `ordersList.jsx` → Tooba seller orders DataGrid pattern (T023) reused for fulfillments list.
- Detail/actions: `orderDetail.jsx` stat grid + action row + shipping sections.

## Repair applied

- Detail header rebuilt with 4-up stat tiles (`text-[10px]` labels) matching Shopeiva orderDetail density.
- Action buttons use `rounded-xl` + `transition-colors` + disabled opacity (removed raw `dispatch` English label → «ارسال»).
- Carrier default removed (`""` + placeholder) — no fake carrier prefill.
- Shipment rows use hover transition from Shopeiva product rows.

## Captures

- List: `15-tooba-seller-fulfillments-list.png` vs `12-original-shopeiva-seller-orders.png`
- Detail basis: `13-original-shopeiva-seller-order-detail.png`
- Hover: `18-tooba-seller-fulfillments-hover.png`
- Mobile: `19-tooba-seller-fulfillments-mobile-390x844.png`
