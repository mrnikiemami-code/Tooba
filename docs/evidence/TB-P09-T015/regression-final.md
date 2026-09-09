# Orders / fulfillment regression — TB-P09-T015

Focused, not a full P09 replay.

Preserved:

- Exact selected line + quantity only
- Partial pack keeps remaining exact (0.50 packed, 0.75 remain)
- No sibling auto-expand
- StartProcessing before Pack
- Shipment eligibility uses packed decimal qty
- Mixed-status bulk incompatibility
- Row capability actions
- Cancelled-order precedence
- Grid mutation refresh
- Return quantity / deadline projection

Frontend:

- `admin-order-items-shipping.test.ts` (decimal qty input, pack_selected, capability 0.75)
- `admin-order-line-actions.ts` `selectableQuantityMax` no longer floors to 1

Backend:

- `FulfillmentLineQuantityOpsTests` / `FulfillmentLineQuantityOperationsTests`
- `QuantityDecimalRegressionTests` pack 0.50 / 0.75
- `SellerPanelCompositionTests`
- `CustomerPanelCompositionTests` (count vs quantity)
