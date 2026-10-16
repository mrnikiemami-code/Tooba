# SHIPPING-ONE-LEVEL-UI-110 — Development migration / post-migration ordering repair

- Task: `TB-SHIPPING-ONE-LEVEL-TABS-110` (parent `TB-SHIPPING-ONE-LEVEL-TABS-109`)
- Program: `TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE`
- Mode: `BOUNDED_DEVELOPMENT_MIGRATION_POSTMIGRATION_ORDER_REPAIR`
- Channel: `tooba-main` / worker `tooba-worker-01`
- Expected base SHA: `072127f7f60a11c0de5583a472b78c27bcf8a5d3` (verified ancestor of HEAD)
- Stop state: `STOP_FOR_ARCHITECT`, automatic next task `NONE`
- Timebox: 30 minutes HARD — implementation stopped well before minute 22.

## 1. Architect premise confirmed by source

TB-109 proved the TB-108 sparse default-variant repair works in one isolated startup (Catalog demo
product Published, two sparse variants, exactly one default). The first new failure was
`Npgsql 42P01 relation "party.parties" does not exist` raised from
`CatalogAttributeSchemaSellableEnricher` (via `PartyDevelopmentSeedGateway`) **before Party migrates**.

This task confirms the cause is a Development **orchestration** prerequisite and not a Catalog readiness
or Shipping price defect:

- The step body is correct and reachable: it is registered by the module that owns it
  (`CatalogModule` → `CatalogDevelopmentSeed.PostMigration.EnsureAsync`, `AfterOrder == Catalog == 1`).
- The enricher legitimately reaches sibling modules through that module's own Contracts-only Development
  gateways (Party, Offer, Pricing, Inventory, Tax), whose schemas migrate at orders 2–6.
- `MigrateOrderedAsync` executed each step **immediately after its own module's migration**
  (`steps.Where(x => x.AfterOrder == migrator.Order)`), so a step that needs a later module's schema was
  guaranteed to fail on a fresh database. `AfterOrder` was therefore an insufficient ordering contract
  for the step body.

Full audit (registered steps, owners, orders, dependency matrix): `ordering-audit.md` in the evidence
folder.

## 2. Change (smallest ownership-correct boundary)

`src/backend/Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs` — `MigrateOrderedAsync` runs two
stable phases instead of one interleaved loop:

1. **all** registered `IModuleSchemaMigrator`s in the unchanged canonical `Order`;
2. **then** all registered `IModuleSchemaMigrationStep`s in the unchanged deterministic order
   (`OrderBy(AfterOrder).ThenBy(Module, Ordinal)`).

`private` → `internal` so the focused test can exercise the seam; nothing else in Host changed
(`Program.cs`, `ModuleSchemaMigrator.cs`, all module composition roots, all `I*SchemaMigrator`
implementations, MigrationRunner and every production path are untouched).

Preserved: canonical migration order, relative step order (Catalog step → Promotion step → Localization
step, exactly as before), fail-fast on duplicate `Order` before anything runs, at-most-once execution per
migrator/step, cancellation propagation, strictly stronger "no step after a failed/canceled migration",
`seedCatalog: false` semantics, Host composition-only rules (no foreign DbContext, no business seed
authority, still no module type named in Host).

Legacy behavior intentionally kept for one edge: a step whose `AfterOrder` names **no** registered
migrator still never runs.

## 3. Why the other two registered steps are safe

| Step | AfterOrder | Cross-module schema need | Effect of moving to phase 2 |
| --- | --- | --- | --- |
| Catalog | 1 | Party(6), Offer(2), Pricing(3), Inventory(4), Tax(5) | **fixed**: all dependencies now migrated first |
| Promotion | 12 | Catalog(1), Offer(2), Pricing(3), Inventory(4), Party(6) | already ordered after its needs; only later relative to pure-DDL migrations |
| Localization | 22 | own module only (`ILanguageDirectory` + `LocalizationDbContext`) | no cross-module need |

Every migrator registered **after** Localization (Content, Media, PageComposition, Story, Notification,
AccessControl, Support, Geography, Shipping) is a plain EF `Database.MigrateAsync` (DDL only; verified by
registration style) and therefore reads no seeded row, so running the Localization step later cannot
affect them.

## 4. Evidence

| Artifact | Content |
| --- | --- |
| `ordering-audit.md` | before/after ordering graph, owners, exact paths, out-of-scope list, TB-111 next check |
| `migrator-diff.txt` | exact source diff of the single changed production file |
| `test-evidence-positive.txt` | focused ordering tests **7/7 passed**; post-fix Host Development guards **17/17 passed** |
| `test-evidence-negative-control.txt` | same tests against the legacy interleaved algorithm: **3 failed / 4 passed**, including `post-migration step ran before its dependency schema migrated.` |
| `no-shared-change-proof.txt` | read-only shared-instance proof, no DDL/DML, no destructive git command, preserved dirty work, base ancestry |
| `task-receipt.json`, `task.md`, `claim-task.js`, `summary.md` | claim artifact and receipt |

Focused tests (`src/backend/Host/Tooba.Host.Tests/Development/DevelopmentSchemaMigratorOrderingTests.cs`,
synthetic migrators/steps only — no DbContext, no database, no Testcontainers):

1. orphan-free core: a module's post-migration step observes a later module's completed migration;
2. canonical migration order preserved and steps ordered by `AfterOrder` then `Module`, each exactly once;
3. failing migration → no post-migration step runs, exception propagates;
4. canceled migration → no post-migration step runs;
5. step with no registered `AfterOrder` keeps its legacy never-run behavior;
6. no step registered → every module still migrates once;
7. duplicate migration `Order` rejected before anything runs.

## 5. Risk and rollback

- Risk: low and Development-only. Ordering of *DDL* migration steps is unchanged; only the position of
  post-migration **seed** steps moved, and all three keep their relative order. Production is unaffected
  (`DevelopmentSchemaMigrator` is not used in Production; MigrationRunner keeps its own registry/path).
- Residual risk: a future module could register a step that (incorrectly) expects to run *before* another
  module migrates. No such step exists today; the guard is the deterministic two-phase contract.
- Rollback: revert the single commit — one production file plus one test file, no data or schema effect;
  the runtime symptom then returns exactly as observed in TB-109.

## 6. Unresolved — next runtime prerequisite (TB-111)

Source-proven and unit-proven only. A separately authorized bounded scratch Host startup must still
verify:

1. Development startup now proceeds past `relation "party.parties" does not exist` with the Catalog
   schema demo product published and the enricher succeeding;
2. genuine sellable offers / base prices / stock rows then exist for the demo product;
3. whether any further barrier appears, and whether the one-level tab surface receives shipping labels.

No runtime launch, scratch bootstrap, sellable-offer claim, customer-cart claim, or storefront-label
claim is made by this task.
