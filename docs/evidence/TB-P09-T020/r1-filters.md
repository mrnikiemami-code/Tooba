# R1 Filters — TB-P09-T020-R1

`ApplyQueueFilterAsync` is quantity-aware via `_db.Items` (`FulfillmentUnit.Items` is EF-ignored).

| Filter | Rule |
| --- | --- |
| ReadyToProcess / Pack / Ship | include remainder qty even when persisted Status is `Dispatched` |
| InTransit | `Dispatched`/`InTransit` **and** no remaining (`QuantityOrdered > QuantityShipped` excluded) |
| NeedsAction | includes remaining qty |
| Status `PartialDispatched` | remaining IDs |
| Status `Dispatched`/`InTransit` | fully shipped only |

Runtime: remainder row in `needs_action` (`needRowStatus: PartialDispatched`); not in `in_transit` (`inTransit: false`).
