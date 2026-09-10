# Customer tracking — TB-P09-T021

Customer `/v1/customer/orders/{checkoutId}/fulfillments` returns:

- `fulfillments` (existing snapshots)
- `preferredCustomerTrackingReference` / `preferredCustomerTrackingPackageNumber` from the latest active (Created/Dispatched) package with central tracking

FE stamps `preferredTrackingReference` onto each customer snapshot. `FulfillmentShippingInfoBlock` prefers that value over the first member shipment tracking. Member tracking rows remain visible under shipment list.
