# TB-SHIPPING-ONE-LEVEL-TABS-112 — Development attribute seed collision repair

Parent: TB-SHIPPING-ONE-LEVEL-TABS-111 · Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: `BOUNDED_CATALOG_DEVELOPMENT_ATTRIBUTE_SEED_COLLISION_REPAIR` · Stop-State: `STOP_FOR_ARCHITECT`
Expected-Base-SHA: `c862d6c63ef571addb8c593f5bd63997a86e29dc`

## Verdict

`PASS` (source + focused-test level only; **no runtime/sellable claim in 112**).

TB-111 proved the TB-110 two-phase Development migration ordering is fixed and then hit a later
failure in the legacy Development bootstrap:
`System.InvalidOperationException: catalog.attribute.code.duplicate` at
`WorkspaceDemoProductSeed.SeedProductAsync` line 103. TB-112 repairs exactly that, with compatible
semantic reuse instead of any weakening of Catalog uniqueness or public attribute creation behavior.

## What was wrong

| Step | Owner | Line | Action |
| --- | --- | --- | --- |
| Post-migration phase-2 step `CatalogDevelopmentSeed.PostMigration.EnsureAsync` | Catalog attribute-schema seed | `CatalogAttributeSchemaDevelopmentSeed.cs:37-44` | creates definition `color` (Enumeration, variant axis) |
| same step | Catalog attribute-schema seed | `CatalogAttributeSchemaDevelopmentSeed.cs:74` | creates option `black` under that definition |
| later Host legacy bootstrap | workspace demo seed | `WorkspaceDemoProductSeed.cs:103` (pre-fix) | tried to create code `color` again → `catalog.attribute.code.duplicate` |

The immediate next statement (`AddAttributeOptionAsync(colorId, "black", ...)`) would have collided
again on the unique index `(definition_id, code)` of `attribute_options`, so the collision fix covers
the definition **and** the option under it.

## Fix (behaviour-preserving, Development-only)

New helper `Tooba.Catalog.Infrastructure/Development/CatalogDevelopmentAttributeReuse.cs`:

- `EnsureCompatibleDefinitionAsync` — reads the canonical code through the existing Catalog read seam
  (`ICatalogDirectory.ListAttributeDefinitionsAsync`), creates via the existing
  `CreateAttributeDefinitionAsync` only when absent, and otherwise reuses the **same** `DefinitionId`
  only when every downstream-required semantic matches: `value-kind`, `variant-axis`, `active`.
  Otherwise it throws the stable, data-minimal diagnostic
  `catalog-dev.attribute.incompatible-reuse: code=<code>; rules=<rule>[,...]` and writes nothing.
- `EnsureCompatibleOptionAsync` — an option's identity is `(definitionId, code)`; reuses the existing
  `OptionId` (module-owned `CatalogDbContext` read, mirroring the sibling seed helper) or creates via
  the existing `AddAttributeOptionAsync`.

`WorkspaceDemoProductSeed.cs` changes by 10 lines: the two unconditional creates become those two calls.
No public Catalog behavior, uniqueness rule, contract, migration, schema, config or Host seam changes.

## Focused proof

- `CatalogDevelopmentAttributeReuseTests` — 7 tests (2 pure decision-rule tests + 5 Testcontainers
  PostgreSQL tests against the real `catalog` schema): empty catalog creates normally; compatible
  existing definition is reused with the same persistent identifier; incompatible existing definition
  is refused before any downstream write (nothing rewritten, no option appended, message names only the
  code + mismatched rules); repeated preparation is idempotent and keeps definition/option identity;
  reused axis and option identifiers stay valid for real variant creation.
- `WorkspaceDemoProductSeedCollisionRegressionTests` — runs the real schema seed (owner of
  `color`/`black`) and then the real affected operation `WorkspaceDemoProductSeed.SeedProductAsync`, and
  asserts the workspace product publishes, exactly one `color` definition exists with the same
  `DefinitionId`, `black` keeps the same `OptionId`, and the created variant still references the reused
  definition.
- Run: `Passed! Failed: 0, Passed: 8, Skipped: 0`.
- Negative control with the pre-fix code: `1 failed — catalog.attribute.code.duplicate` at
  `WorkspaceDemoProductSeed.cs:103` (same failure TB-111 observed, same line).
- `dotnet build src/backend/Tooba.slnx -c Debug` → 0 errors.

Not run on purpose: the broad known-failing Host suite (contains pre-existing unrelated guard drift:
`Host_Admin_count_15_*` expects 15 `*.cs` under `src/backend/Host/Tooba.Host/Admin`, while the expected
base SHA and HEAD both contain 17 — proven via `git ls-tree -r`), the 545-product demo seed, live
browser flows, and any Host start (TB-113 owns that).

## Shared environment

Read-only check only: `:5088` → PID 35624 and `:3000` → PID 31580, byte-identical to the TB-111 parity
capture. No restart/stop/DDL/DML/kill, no scratch start, no shared `tooba_alpha` access.

## Evidence

`docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-112/` — `root-cause.md`,
`focused-tests.txt`, `negative-control.txt`, `shared-no-change.txt`, `task-receipt.json`,
`task.md`, `post-result.js`, `RESULT.bridge.txt`.

## Next

Architect ruling → TB-113 isolated scratch runtime proof (watchdog ≤ 180 s) as described in
`root-cause.md` §6. No automatic next implementation task.
