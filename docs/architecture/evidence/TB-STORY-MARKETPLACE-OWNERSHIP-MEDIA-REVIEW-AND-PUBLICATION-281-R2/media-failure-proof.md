# Media failure proof (TB-281-R2)

## Algorithm change

`MediaDirectory.UploadForSellerAsync` now:

1. Buffers bytes and computes intent hash (tenant+shop+type+name+digest).
2. Replays exact scoped occurrence before any store write when intent matches.
3. Writes object store, then ONE SaveChanges for asset+binding+occurrence.
4. On unique violation: detach attempt-owned entries only, delete attempt-owned object, fold to winner if intent matches.
5. On other save failure: detach attempt-owned entries, delete attempt-owned object, typed UploadFailed — no blind SaveChanges cleanup on a failed tracker.

## Proof status

- Source/compile: Release build of Media Infrastructure/Endpoints/Application succeeded.
- Dedicated recording `IMediaObjectStore` suite asserting save-failure/conflict/duplicate row and object call counts: NOT completed in this pass.
- Controlled compensation remains test-storage only; canonical media files untouched; ZERO live upload.
