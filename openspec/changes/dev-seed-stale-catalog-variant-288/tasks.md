# Tasks — dev-seed-stale-catalog-variant-288

## 1. Root cause (before code)
- [x] 1.1 Source-trace Catalog reset variant deletion vs Offer seed reuse-by-SKU; confirm Inventory guard.
- [x] 1.2 Record ROOT-CAUSE.md with source branches and 287 crash evidence.

## 2. OpenSpec
- [x] 2.1 Create strict change `dev-seed-stale-catalog-variant-288` with REQ-SEED-01..04 traceability.

## 3. Bounded fix (Offer-owned, Development-only)
- [x] 3.1 Reuse a surviving seed Offer only when its Catalog variant still exists (ICatalogVariantLookup).
- [x] 3.2 Rebuild stale marked dev-seed Offer on a live published variant; one Offer per SellerSku.
- [x] 3.3 Focused tests: stale-variant rebuild + idempotent second run + valid reuse.

## 4. Build & tests
- [x] 4.1 `dotnet test` focused Offer seed tests pass.
- [x] 4.2 `dotnet build` Host graph 0 errors.

## 5. Connected verification (S01–S07)
- [x] 5.1 S01 root cause SOURCE_VERIFIED (source + 287 crash evidence).
- [ ] 5.2 S02/S03 connected bootstrap after reset — BLOCKED (no authorized isolated fixture; shared dev DB/canonical Host must not run the destructive reset; re-provision forbidden).
- [x] 5.3 S04 Inventory fail-closed SOURCE_VERIFIED (guard unchanged; fired in 287 evidence).
- [ ] 5.4 S05 roles across fixture setup — NOT_RUN (no fixture).
- [x] 5.5 S06 canonical untouched / no shared DB writes (ports 5088/3000 healthy; no DB created/deleted by this task).
- [x] 5.6 S07 build+diff scope+provenance.
