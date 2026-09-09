# R1 Partial-Status Projection — TB-P09-T020-R1

Projection: shipped > 0 && shipped < ordered → `PartialDispatched` (ارسال جزئی). Persisted enum may remain `Dispatched`.

`AdminFulfillmentQueueFilters.ComposeOperationalStatus(FulfillmentSnapshot)`:

- Cancelled / Failed → persisted status
- remaining fulfillable qty → `PartialDispatched`
- else persisted status

`AdminPanelComposer.LineOperationalStatus` no longer short-circuits the whole unit to `Dispatched`. Remainder line is `PartialDispatched`; fully shipped line is `Dispatched` / `InTransit` / `Delivered`.

Runtime checkout `01a0864a-f248-7000-92d9-426915151d95`: after KG dispatch 0.50 of 1.25, `kgStatus` and `kgLineStatus` = `PartialDispatched`. After remainder 0.75 dispatch, `kgStatus` = `Dispatched` (not `Delivered`).
