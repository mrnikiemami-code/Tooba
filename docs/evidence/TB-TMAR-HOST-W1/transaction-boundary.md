# TB-TMAR-HOST-W1 Transaction Boundary

Before:
- Host `StoreLandingPageComposer.SetHomeAsync` owned `BeginTransactionAsync` (when `_catalog.Database.IsRelational()`), SaveChanges, Commit.

After:
- Transaction ownership moved to `StoreLandingPageDirectory.SetHomeAsync` (Catalog.Infrastructure).
- Same relational guard (`IsRelational()`), same commit order, same page-type + settings atomic write.
- No global MediatR TransactionBehavior introduced.

Other write methods: single SaveChangesAsync (or two for ReplaceComposition/DeleteSection) inside Directory — same semantics as Host.
