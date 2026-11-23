# RESULT — TB-DEV-SEED-STALE-CATALOG-VARIANT-288

Status: **BLOCKED** (one exact cause: no authorized isolated fixture for the destructive Catalog-reset proof).
Protocol: TOOBA-OPENCODE-DELIVERY-V1@1.3.0 — worker outcome never ARCHITECT_ACCEPT.

## Root cause (proved)
A Catalog demo reset deletes demo variants (`CatalogDemoResetService.cs:196`) and regenerates new variant ids.
Offers survive. The Offer Development seed gateway reused a surviving Offer by `SellerSku` without checking that
its `CatalogVariantId` still exists, so Promotion's OOS seed drained stock on a dangling variant and Inventory
correctly failed closed (`InventoryDirectory.cs:91`, `inventory.catalog_variant.missing`). See `ROOT-CAUSE.md` and
`evidence/source-trace.txt`.

## Bounded fix (Offer-owned, Development-only)
`OfferDevelopmentSeedGateway.EnsureActiveCloneFromAnyActiveAsync` now reuses a seed Offer only when
`ICatalogVariantLookup.FindVariantAsync(existing.CatalogVariantId)` is non-null; otherwise it rebuilds the marked
dev-seed row (`SellerSku`) on a live published variant (fallback: valid Active template), keeping one Offer per
SellerSku. Inventory guard, Pricing/Inventory policy, public API, Offer 287 fingerprint, Host ownership, schema and
Cursor/AGENTS are untouched. OpenSpec change `dev-seed-stale-catalog-variant-288` validated **strict**.

## Acceptance results
- S01 root cause — **PASS (SOURCE_VERIFIED)**: `ROOT-CAUSE.md` + `source-trace.txt` + 287 real crash logs.
- S02 connected bootstrap after reset — **NOT_RUN**: no authorized isolated fixture; destructive reset must not run
  on shared dev DB / canonical Host (5088 runs non-candidate code on `tenant-alpha`); reprovision forbidden.
- S03 idempotence — **NOT_RUN** (depends on S02 fixture); logic proven by focused test (second run reuses rebuilt row).
- S04 Inventory fail-closed — **PASS (SOURCE_VERIFIED)**: guard unchanged and fired in 287 evidence.
- S05 roles across fixture — **NOT_RUN** (requires S02 fixture); no role code touched.
- S06 canonical/cleanup — **PASS (OBSERVED)**: 5088 `/health`=200, 3000 `/fa`=200; no task DB created or dropped; no
  shared DB mutation by this task.
- S07 build/scope/provenance — **PASS**: Host build 0 errors (`evidence/host-build.log`); focused tests 4/4 pass
  (`evidence/focused-test.log`); OpenSpec strict valid (`evidence/openspec-validate.txt`); diff scoped to Offer seed
  files + tests + task/openspec dirs; producer hash `evidence/observed-288.json`.

## Blocker (exact)
There is no explicitly authorized isolated fixture, and the original contract forbids running the destructive
Catalog demo reset on the shared Development DB (and 5088 runs non-candidate code). Full re-provision is outside the
task's stated budget. Therefore the connected reset→bootstrap replay (S02/S03/S05) is honestly NOT_RUN.

## Budget / cost
Harness not run (no fixture). Build ≈ 34 s. Whole-task elapsed and tokens/cost: **NOT_AVAILABLE**.

## Cleanup / safety
No DB created or dropped; no Host restarted; canonical 5088 and frontend 3000 healthy and untouched; 285/286/287
work untouched. `STOP_FOR_ARCHITECT` — no next task, no Stage 3.
