# Action scope

- `filterOperationsForScope("whole-order"|"detail")` excludes `mark_processing`, `mark_packed`, `pack_selected`, `unpack`, shipment/return codes.
- Detail header menu now `scope="whole-order"`.
- Seller toolbar consumes backend projection: StartProcessing, Pack All, Pack Selected, Unpack, CreateShipment.
- Line kebab uses only actions with matching `orderLineId`.
