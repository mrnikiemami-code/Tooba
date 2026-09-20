# Orders admin risk — TB-TMAR-FE-ADMIN-W6 (READ-ONLY)

## Files (non-exhaustive)

- `admin-screens.tsx` — AdminOrdersScreen + order columns
- `admin-order-detail-screen.tsx` — detail/payment/seller tabs
- `admin-order-operations.ts` / menu / scope — `executeAdminOrderOperation` mutations
- `admin-order-items-shipping-panel.tsx` — fulfillment pack/ship/cancel
- `admin-api.ts` — load/map/query orders + enrich detail
- related: supply, status-cards, completeness, reservation cycle

## Coupling

- Payment confirm/restore/unconfirm and reservation retry UX
- Fulfillment lifecycle (processing/packed/ship/cancel shipment)
- Returns queue cross-links
- CheckoutId-centric Host APIs

## Characterization gaps

Grid/detail/ops have focused tests, but no complete extraction characterization proving seam isolation from payment/fulfillment semantics.

## Classification

Orders-Admin-Risk: **NEEDS_BACKEND/WORKFLOW_BOUNDARY_FIRST**

Not LOW_RISK_FEATURE_MIGRATION. Do not migrate in next ADMIN wave.
