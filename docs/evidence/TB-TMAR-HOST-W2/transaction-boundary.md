# TB-TMAR-HOST-W2 Transaction Boundary

Before: Host StoreMenuComposer owned SaveChangesAsync per write method; no BeginTransaction.

After: StoreMenuDirectory (Catalog.Infrastructure) owns all SaveChangesAsync for this slice.
No explicit transaction was present; none introduced.
SetHeader remains single SaveChanges of appearance settings + validation — same semantics.
