# 05 — Seller fulfillment detail + mutations

Route: `/vendor-panel/fulfillments/{fulfillmentId}`

File: `src/frontend/app/vendor-panel/fulfillments/[fulfillmentId]/page.tsx`

Mutations wired to Host POST endpoints:

- processing
- packed
- create shipment (remaining quantities)
- assign tracking
- dispatch
- deliver

Uses existing vendor panel surface tokens (`rounded-2xl`, `border-border`, `bg-surface-elevated`).
