# Shipment modal shell — TB-P09-T004

- `AdminCreateShipmentModal` uses design-system `Dialog`
- Shows seller, selected/remaining lines, carrier select
- Partial line selection disables submit with T005 deferred message
- Whole remaining qty + carrierDisplayName wires existing `create_shipment`
- No window.prompt/alert/confirm in ops menu or modal
