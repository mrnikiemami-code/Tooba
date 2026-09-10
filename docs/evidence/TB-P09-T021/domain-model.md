# Domain model

## Status
Created → Dispatched → Delivered  
Created → Cancelled

## Aggregate
- `ConsolidatedPackage` (CheckoutId, PackageNumber MP-…, shipping method, central tracking, note, CreatedBy, timestamps)
- `ConsolidatedPackageMember` (ShipmentId, SellerPartyId snapshot, FulfillmentId, JoinedAt, ReleasedAt)

## Rules
- Same CheckoutId only
- ≥2 distinct SellerPartyIds at create
- Members must be ShipmentStatus.Created at create
- Unique active membership (`ReleasedAt IS NULL`)
- Membership immutable after Dispatch (cancel only while Created)
- Package Dispatched/Delivered only when all members reach those states via existing shipment commands
