# TB-TMAR-HOST-W2 Architecture Guards

Removed from `tmar-host-write-files.json`:
- `Admin/StoreMenuComposer.cs`

Baseline SHRINK.
Existing guards remain:
- Host write sites do not expand
- Module business MediatR handlers do not live in Infrastructure
- App→App edges baseline
- ArchitectureBoundaryTests
