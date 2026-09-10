# TB-P09-T021 Reuse vs Change

## Reuse
- Seller Shipment aggregate and dispatch/deliver commands
- FulfillmentDirectory orchestration + inventory consume on dispatch
- Whole-order cancel abort path (extended, not replaced)
- Shipping method registry codes/labels
- Error machine-code → FA mapping pattern

## Change (additive)
- Domain: `ConsolidatedPackage`, `ConsolidatedPackageMember`, `ConsolidatedPackageStatus`
- Persistence tables + filtered unique index on active membership
- Directory APIs for create/cancel/dispatch/deliver/query/lock/void
- Member lock on cancel/correct-tracking/dispatch/deliver while package Created|Dispatched
- Host tests `ConsolidatedPackageTests.cs`
