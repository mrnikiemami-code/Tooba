# Error / concurrency — TB-P09-T022

Localized stable codes (FA surface, no raw exception dump):

| Condition | Code |
| --- | --- |
| &lt;2 distinct sellers | `fulfillment.package.requires_multi_seller` |
| Shipment not ready | `fulfillment.package.shipment_not_eligible` |
| Already in active package | `fulfillment.package.shipment_already_member` |
| Mixed checkout | `fulfillment.package.mixed_checkout` |
| Cancel after dispatch | `fulfillment.package.cancel_after_dispatch` |
| Direct member op while locked | `fulfillment.shipment.locked_by_consolidated_package` |
| Unauthorized customer | 404 |

Runtime I: sequential second create with same shipments while first active must reject.
