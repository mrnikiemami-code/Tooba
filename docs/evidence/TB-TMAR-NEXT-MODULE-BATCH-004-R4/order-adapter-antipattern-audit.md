# Order Adapter Anti-Pattern Audit — TB-TMAR-NEXT-MODULE-BATCH-004-R4

## Defect (introduced in R3)

`AdminOrderFulfillmentOperations.cs` used:

- `PlatformHttpException` with Persian user-facing prose
- `catch (InvalidOperationException)` → silent generic failure
- `MapFulfillmentException` switching on Persian/English prose strings

## Repair

| Item | Result |
|------|--------|
| Public contract | `IAdminOrderFulfillmentOperations` unchanged |
| Expected failures | `AdminOrderFulfillmentOperationOutcome(false, stableCode)` |
| HTTP exceptions | **0** (`PlatformHttpException` absent) |
| Localized prose | **0** in adapter source |
| Prose message switch | **0** (`MapFulfillmentException` deleted) |
| Downstream mapping | Only stable machine codes (`fulfillment.*` / `inventory.*` / `shipping_service.*` / `order.*`) + cancel→dispatch remap |
| Unexpected `InvalidOperationException` | Propagates (not converted to generic failure) |
| Host dependency | None |

## Preserved stable codes

- `order.operation.invalid`
- `order.cancelled.blocks_action`
- `order.operation.denied`
- `fulfillment.pack.requires_processing`
- Downstream e.g. `fulfillment.dispatch.tracking_required`
- Transition: `fulfillment.cancel.already_dispatched` → `fulfillment.dispatch.already_dispatched`
