# TB-P07-T018-R1 Evidence

Repair: forbid Archived -> Published; Restore is the only exit from Archive.

## Validation

| Check | Result |
|-------|--------|
| Backend build | green |
| Host.Tests | (see 02-backend-test-full.log) |
| FE typecheck | green |
| FE lint | green |
| FE product-workspace | 41 pass |
| FE build | green |
| git diff --check | clean (CRLF warnings only) |
