# R1 Status Discovery — TB-P09-T020-R1

Persisted `FulfillmentUnit.Status` stays `Dispatched` after any dispatch. `ApplyShipmentDispatched` no longer jumps to `Delivered` when remaining quantity reaches zero; `Delivered` is only via `ApplyShipmentDelivered`.

T020 left the aggregate as `Dispatched` while remainder packed. Admin Order Detail and Fulfillment Queue now project composed operational status from remaining vs shipped quantity.

| Surface | Source | After first 0.50 of 1.25 |
| --- | --- | --- |
| Domain aggregate | `FulfillmentStatus` | `Dispatched` |
| Order Detail seller / line | `ComposeOperationalStatus` / `LineOperationalStatus` | `PartialDispatched` |
| Fulfillment Queue row | `AdminFulfillmentQueueFilters.ComposeOperationalStatus` | `PartialDispatched` |
| FE label | `formatAdminStatus` / `formatFulfillmentStatus` | ارسال جزئی |

No new persisted enum value. Cancelled/Failed stay persisted status.
