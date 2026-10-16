TB-SHIPPING-ONE-LEVEL-TABS-113 — OBSERVATION ONLY (no source/test/config change)
=================================================================================

Status            : PARTIAL — the TB-111 collision is demonstrably gone and the FIRST subsequent
                    barrier is precisely captured, with complete teardown and shared parity.
                    The single launch did NOT reach HEALTHY (a later, different startup step aborts).
Original collision: YES -> NO. `catalog.attribute.code.duplicate` occurrences = 0 (was the crash in TB-111).
New first blocker : System.InvalidOperationException "Cannot resolve scoped service
                    'Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext' from root provider."
                    at Tooba.Catalog.Infrastructure.Development.FashionTemplateCatalogSeed.ApplyAsync
                    (FashionTemplateCatalogSeed.cs:59), called from
                    CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync (CatalogDevelopmentSeed.cs:61),
                    from Program.cs:368.
Authoritative detail: see redacted-diagnostic-excerpt.txt and sellable-chain-evidence.txt.

What this launch proved
-----------------------
1. TB-112 HOLDS AT REAL STARTUP. The whole `DevelopmentSchemaMigrator.ApplyAsync(app.Services)`
   call at Program.cs:367 completed, which includes the legacy workspace demo seed chain
   (`WorkspaceDemoSeed.SeedNewProductAsync` -> `WorkspaceDemoProductSeed.SeedProductAsync`) that
   TB-111 crashed in. The duplicate-code failure and the `catalog-dev.attribute.incompatible-reuse`
   guard are both absent from the full 1,657-line stdout.
2. TB-110 ORDERING STILL HOLDS. `party.parties` is referenced 22 times and resolves; 42P01
   occurrences = 0; party.parties holds 4 rows.
3. COMPATIBLE REUSE IS VISIBLE IN PERSISTED DATA. Exactly one `color` definition and exactly one
   `black` option exist, and the LEGACY workspace variant LIVE-SHIRT-BLK stores the SAME
   `definition_id` (01a14266-d5b4-7000-9836-fedb228ab758) for `color` that the schema demo phone's
   two variants use — i.e. the legacy seed consumed the schema seed's existing definition instead of
   creating a second one.
4. PUBLISHED SPARSE PRODUCT INTACT. schema-mobile-demo-phone = Published with exactly 2 variants and
   exactly 1 default; the two variants differ on BOTH axes (fingerprints confirm no Cartesian matrix).
5. PRICED/STOCKED ACTIVE OFFERS EXIST for both schema demo variants (2 active offers / 1 active Base
   price / available 5.0 for the default; 1 / 1 / 5.0 for the second). Row-count/integrity observation
   ONLY — explicitly NOT customer/cart/storefront eligibility proof.
6. The later legacy development bootstraps (Fashion/Industry templates, landing pages, store menu)
   are now the blocker, and `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync` is reached with the
   ROOT `app.Services` from Program.cs:368 while every earlier seed ran inside the async scope created
   at DevelopmentSchemaMigrator.cs:46-47. TB-112 did not touch any of these files.

Deviation from the ideal PASS shape
-----------------------------------
One isolated launch was permitted and only one was performed. The launch exited by itself at 61 s of
the 180 s watchdog (KILLED_BY_WATCHDOG=False, HEALTH_200_OBSERVED=False, listeners on 5396 after stop = 0),
so no /health 200 and no provenance could be read from the isolated Host. No retry was attempted, as
instructed.

Cleanup and non-interference
----------------------------
Task-owned Host PID 6192 confirmed gone; `docker rm -f tooba-113-scratch-pg` removed the only
task-owned container (no tooba-113 containers remain); `.tmp-113-run` and `.tmp-113-host-out` deleted
(with the scratch credential file); listeners on 5396 and 55448 = 0. No name/wildcard termination, no
broad PID cleanup, no shared process touched.
SHARED PARITY (read-only): :5088 -> PID 35624, :3000 -> PID 31580 (byte-identical to the preflight and
to the TB-111 capture), /health 200 {"status":"ok"}, /__platform-commerce unchanged tenant
store-alpha / tenant-alpha / dev-singlestore, Next 308 -> /fa, and tooba_alpha counts
methods 7 / translations 14 / carts 50 / cart_lines 78 / drafts 14 / seller_orders 26 / products 567 /
offers 3223 — IDENTICAL to the preflight baseline. No incident.

Rollback
--------
None required: this task changed no source, test, migration, Host config, frontend, script or shared
data. Its commit contains docs/evidence only.
