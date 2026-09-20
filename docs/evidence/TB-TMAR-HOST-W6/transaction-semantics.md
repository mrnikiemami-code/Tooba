# Transaction semantics — TB-TMAR-HOST-W6

Per-operation single SaveChangesAsync in ShippingServiceDirectory; module-local FulfillmentDbContext; no BeginTransaction; no TransactionScope; no cross-context ACID. ARCH-TX-001 preserved.
