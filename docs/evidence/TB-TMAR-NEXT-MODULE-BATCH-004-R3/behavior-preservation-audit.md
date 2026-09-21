# Behavior Preservation Audit — R3

## Order fulfillment ops

| Concern | Preserved |
|---|---|
| Cancelled checkout blocks listed codes | `order.cancelled.blocks_action` |
| Permission gate (legacy-admin + order.handle/fulfillment.manage) | `order.operation.denied` |
| mark_processing → `IFulfillmentDirectory.MarkProcessingAsync` | yes |
| mark_packed ReadyToFulfill → `fulfillment.pack.requires_processing` | yes |
| mark_packed packs all packable selections | yes |
| create_shipment method enablement + ResolveLabel | yes |
| assign_tracking requires reference | yes |
| dispatch/deliver require shipmentId | yes |
| IOE mapping e.g. tracking_required | `fulfillment.dispatch.tracking_required` |
| TryExecute outcome shape | success true/null; failure false+code |

## Shipping methods tree

| Concern | Preserved |
|---|---|
| Enabled filter + sort | yes |
| Language fallback (requested → first translation → code) | yes |
| Empty catalog → registry fallback | yes |
| Default colors/options for post/tipax | yes |
| Inactive options filtered | yes |
| Shape: code/labelFa/name/providerKind/iconKey/colorKey/options | yes |
| Route `/v1/admin/shipping-methods` | unchanged |
