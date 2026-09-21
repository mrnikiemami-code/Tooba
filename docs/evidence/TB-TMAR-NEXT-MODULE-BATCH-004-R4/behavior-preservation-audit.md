# Behavior Preservation Audit — TB-TMAR-NEXT-MODULE-BATCH-004-R4

## Operations covered (unchanged codes)

| Operation | Gate / behavior preserved |
|-----------|---------------------------|
| cancelled checkout blocking | `order.cancelled.blocks_action` for blocked codes |
| permission gate | `order.operation.denied` |
| seller-order linkage | `order.operation.invalid` on mismatch |
| mark_processing | success via `IFulfillmentDirectory.MarkProcessingAsync` |
| mark_packed | `fulfillment.pack.requires_processing` when ReadyToFulfill; pack selections otherwise |
| create_shipment | method enablement via `ShippingMethodRegistry.Enabled`; linkage + create |
| cancel_shipment / assign_tracking / dispatch / deliver | linkage + directory calls |
| shipment/fulfillment linkage | sellerOrderId vs snapshot check |

## Language gate

- Known IDs succeed
- Unknown → `shipping_service.language_invalid`
- Seed mapping: LanguageId / Code / Culture / IsDefault from `ILanguageLookup`

## Out of scope (untouched)

Checkout process semantics, Tax/Pricing, frontend, BATCH-005.
