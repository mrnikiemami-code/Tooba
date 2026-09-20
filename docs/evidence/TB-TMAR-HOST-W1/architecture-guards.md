# TB-TMAR-HOST-W1 Architecture Guards

Removed from `tmar-host-write-files.json`:
- `Admin/StoreLandingPageComposer.cs`

Baseline SHRINK confirmed (one fewer Host write site).

Guards still enforce:
- no NEW Host write sites beyond shrunk baseline
- App→App edges baseline unchanged
- IMemoryCache baseline unchanged

Host `StoreLandingPageComposer` after refactor:
- no SaveChangesAsync
- no BeginTransaction
- no entity Add/Remove on business DbContext for this slice
- writes only via ISender
