# R1 projection rules — TB-P09-T021-R1

`FulfillmentPanelComposer.SelectPreferredCustomerPackage`:

1. Candidates: status ∈ {Created, Dispatched, Delivered}
2. Never Cancelled as primary
3. Newest `UpdatedAt` / `CreatedAt` wins (rebuild → new package)
4. Prefer candidate with non-empty `TrackingReference`; else still return package number for Created preparing state

Envelope fields:

- `preferredCustomerTrackingReference`
- `preferredCustomerTrackingPackageNumber`
- `preferredCustomerPackageStatus`

Member shipment tracking/history preserved on fulfillment snapshots. No package → preferred fields null; member tracking unchanged.
