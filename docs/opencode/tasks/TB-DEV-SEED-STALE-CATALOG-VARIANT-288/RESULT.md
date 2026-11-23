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

## Owner authorization execution (2026-11-23) — backup done, destructive step declined for safety
Owner `OWNER-DEV-BACKUP-EXECUTION.md` authorized using the existing dev Host/DB after a verified backup. Executed:
- Proved identity: Host `D:\Users\User\source\repos\SarvNewVer\.tmp-dev-host-out-5088\Tooba.Host.exe` (PID 49816,
  SHA-256 `41EF43E2…`, contentRoot main worktree), SingleStore/dev-singlestore/`store-alpha`, connectionReference
  `tenant-alpha` ⇒ database **`tooba_alpha`** (PG 17.10, 53,327,539 bytes; offers=3223, variants=1361, products=567,
  parties=10). frontend 3000 up (200). See `evidence/dev-backup-metadata.txt`.
- **Verified backup (hard prerequisite satisfied):** `pg_dump -U admin -Fc -d tooba_alpha` exit 0 →
  `C:\Users\User\AppData\Local\Temp\opencode\dev-db-backups\tooba_alpha_288backup_20261123_133446.dump`,
  3,091,575 bytes, SHA-256 `4839D6649C4E4066A404FA73EA95BFF60AE67343EEA4A74BF81F2C582B07FB9C`,
  `pg_restore --list` exit 0 (TOC 1476 / 1487 lines). Backup bytes are NOT in git.
- **Destructive deploy/reset NOT executed — exact reason (owner safety clause items 4–5):**
  1. The only candidate Host I can build now is the worker-branch build that also contains the **BLOCKED Task 287**
     additive migration `AddOfferCreationIntentFingerprint` (present under Offer/Persistence/Migrations). Deploying
     it against `tooba_alpha` would apply a 287 schema/behavior change that Task 288 forbids. A compliant
     "288-only" candidate must first be built from latest `origin/main` + only this 288 gateway diff.
  2. Proving S02/S03/S05 requires replacing the shared canonical Host on 5088 (exe SHA `41EF43E2…`, not candidate),
     which interrupts the dependent frontend (3000) and any active session for the whole reset + two-boot + restore
     window. The environment has already demonstrated non-idempotent Development-seed instability, so a single
     bounded window that is guaranteed to end in a clean backup restore is not assured.
  Because a partial destructive state would harm the owner's shared dev instance, I stopped BEFORE any reset/deploy.
  S02/S03/S05 remain **NOT_RUN** (not fabricated). No DB was reset, created, dropped, or restored; 5088/3000 untouched.

### Safe procedure to finish S02/S03/S05 in a greenlit window
1. Build a 288-only candidate from `origin/main` + this gateway diff (no 287 migration); record its exe SHA.
2. In a quiet window: stop 5088; run candidate on 5088 against `tooba_alpha`; `/v1/admin/catalog/demo/reset-and-seed`;
   stop; second boot to prove idempotence; SQL-verify OOS Offer→live variant, Inventory fail-closed, roles.
3. Stop candidate; re-upload the verified dump and `pg_restore --clean --if-exists -d tooba_alpha`; re-verify DB
   identity; restart the original 5088 exe; confirm 5088 `/health`=200 and 3000 `/fa`=200.


## Budget / cost
Harness not run (no fixture). Build ≈ 34 s. Whole-task elapsed and tokens/cost: **NOT_AVAILABLE**.

## Cleanup / safety
No DB created or dropped; no Host restarted; canonical 5088 and frontend 3000 healthy and untouched; 285/286/287
work untouched. `STOP_FOR_ARCHITECT` — no next task, no Stage 3.
