# TB-P10-T004-R11 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| First seller gets shipping | CLEAN — StoreShipping target + initiate OrderBy seller id |
| Seller totals padded | CLEAN — merchandise-only seller allocations |
| Duplicated payable | CLEAN — initiate uses payable snapshot; projection reconstructs same equation |
| Frontend-derived totals | CLEAN — Host/Payment authoritative |
| Raw equality off Money policy | CLEAN — exact `decimal`; no epsilon |
| Projection retry/suppression | CLEAN — inbox EventId |
| Hardcoded shipping target outside canonical source | CLEAN — `PaymentAllocation.StoreShippingTargetId` only |
