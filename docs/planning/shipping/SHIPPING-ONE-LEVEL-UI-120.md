# TB-SHIPPING-ONE-LEVEL-TABS-120 — Fulfillment-owned Development bootstrap for the one-level shipping catalog

Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_FULFILLMENT_OWNED_ONE_LEVEL_CATALOG_BOOTSTRAP_WIRING
Parent: TB-SHIPPING-ONE-LEVEL-TABS-119
Channel / Worker: `tooba-main` / `tooba-worker-01`
Expected-Base-SHA: `dc2eb1bd71261cd95bc49ac77814105034279913`
Stop-State: STOP_FOR_ARCHITECT

## Why this task exists (TB-119 finding)

TB-119 proved an isolated guest checkout Shipping Projection returns seven valid **legacy**
methods, but even with `Tooba:ShippingOneLevelCatalog:Enabled=true` the one-level catalog
endpoint returned `methods=[]`. Source trace confirmed the exact missing seam:

- `20261015120000_AddShippingOneLevelCatalog` creates the tables with **DDL only** — no rows.
- `ShippingOneLevelCatalogActivation` (`:75-81`) and `ShippingOneLevelCatalogAdminWrite`
  (`:75, :96`) only **update/translate existing rows** and return `MethodNotFound` — they never
  create a row.
- The **only** row creator is `ShippingOneLevelBackfillStore` / `ShippingMethodBackfillPlanner`,
  registered in DI (`FulfillmentModule.cs:117`) but with **no production/Host caller**.
- `IShippingCatalogSeedPort.EnsureSeedAsync` seeds the **legacy** service/option tables only.

So on any fresh environment the one-level catalog stays empty regardless of the gate, because
nothing authorized ever invokes the registered backfill.

## Traced call flow (before → after)

Before:

```
Host.Program (Development)
  └─ (nothing invokes ShippingMethodBackfillPlanner / IShippingOneLevelBackfillStore)
```

After (this task, bounded to Fulfillment-owned Development integration + one Host composition line):

```
Host.Program (Development, single-store else-branch)
  └─ DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(app.Services, "…", seam)
       ├─ resolves store-alpha (Active) + assigns trusted CommerceContext on the scope
       └─ Tooba.Fulfillment.Infrastructure.Shipping.ShippingOneLevelCatalogDevelopmentSeed.ApplyAsync(provider, ct)
            (a) IHostEnvironment.IsDevelopment()      → else return (no DB touch)
            (b) ShippingOneLevelCatalogPolicy.Evaluate(IOptions<ShippingOneLevelCatalogOptions>)
                                                      → Enabled? else return (no query/write)
            (c) IShippingCatalogSeedPort.EnsureSeedAsync(ct)   ← reuse existing legacy seed seam
            (d) IShippingCatalogReader.ListAsync(ct)           ← authoritative source
            (e) IShippingOneLevelBackfillStore.ListExistingAsync(ct)
            (f) ShippingMethodBackfillPlanner.Plan(catalog, existing)  ← existing pure function
            (g) !plan.IsExecutable           → return  (fail-closed, never applies)
            (h) all items == Skip            → return  (idempotent no-op, no transaction)
            (i) IShippingOneLevelBackfillStore.ApplyAsync(plan, ct)    ← existing transactional writer
```

No mapping rule, no SQL, no second algorithm: the bootstrap only sequences the seams that already
exist. The Host implements none of the Shipping catalog rules.

## Authority / gate / activation decisions

| Decision | Value | Rationale |
| --- | --- | --- |
| Caller authority | `DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync` | The canonical, already-authorized Host Development gateway (same one `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync` uses). It assigns the trusted `store-alpha` `CommerceContext`; the module never fabricates a tenant. |
| Gate | `Tooba:ShippingOneLevelCatalog:Enabled`, read via `ShippingOneLevelCatalogPolicy` | Default **off**, absent from tracked appsettings. Gate is evaluated **inside** the module seed so a disabled gate performs **zero** DB access. |
| Environment scope | `IHostEnvironment.IsDevelopment()` inside the module seed | Defense-in-depth: even if composed elsewhere, the seam cannot run outside Development. |
| Legacy source | `IShippingCatalogSeedPort.EnsureSeedAsync` (existing module seam) | The planner's authoritative input is the legacy service/option catalog; it is ensured, never invented. |
| Activation vs. backfill | Bootstrap does **not** call `IShippingOneLevelCatalogAdminActivation` | Activation of an option-backed method is a **separate explicit operator action**. The backfill preserves each option's `IsActive`, so no deployment silently activates an inactive method. |
| Translation/language | Planner keeps each language's own name; legacy seed no-ops when no languages exist | Missing languages ⇒ empty source ⇒ empty plan ⇒ no write (fail-closed). No name is smeared across languages. |
| Idempotency / transaction | Reuse `ShippingOneLevelBackfillStore` | All-or-nothing transaction; a violating plan is refused; a rerun yields only `Skip` and writes nothing. |
| Placement (module) | `Tooba.Fulfillment.Infrastructure/Shipping/ShippingOneLevelCatalogDevelopmentSeed.cs`, namespace `…Infrastructure.Shipping` | `FulfillmentArchitectureGuardTests.AllowedInfrastructureFolders` does **not** allow a `Development/` folder. Adding it would edit a certified module layout guard (guard weakening), so the file lives in the already-allowed `Shipping/` folder with exact path↔namespace alignment. |
| Placement (Host) | One call site in the existing `Program.cs` Development branch; **no new Host file** | Both `HostDevelopmentAmcGuardTests` (Host/Development allowlist) and `HostCompositionAmcGuardTests` (Host/Composition allowlist) pin exact file sets; a Host wrapper file would require editing two durable guards. The in-place composition call matches the pre-existing `CatalogDevelopmentSeed` hand-off pattern. |

## Exact bounded diff

- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/ShippingOneLevelCatalogDevelopmentSeed.cs` **(new)** — the module-owned Development bootstrap.
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/ShippingOneLevelDevelopmentSeedTests.cs` **(new)** — five focused facts.
- `src/backend/Host/Tooba.Host/Program.cs` **(modified, +16 lines)** — one `using` plus one Development-only `try/catch` composition call.

No change to Shipping/Order/Checkout/Payment business behavior, Catalog, migrations, schema,
production startup paths, frontend, shared appsettings, public endpoints or feature-gate defaults.

## Focused verification (one bounded run each; no loops)

| Run | Filter | Result |
| --- | --- | --- |
| New focused tests | `FullyQualifiedName~ShippingOneLevelDevelopmentSeedTests` | **5 passed / 0 failed** (`focused-tests-120.log`) |
| Module guards + shipping one-level | `FulfillmentArchitectureGuardTests \| ShippingOneLevel` | **93 passed / 0 failed** (`module-guards-shipping-120.log`) |
| Build (changed surface) | `Tooba.Fulfillment.Tests`, `Tooba.Host` | 0 errors (existing, unrelated warnings only) |

Facts covered:

1. **Population** — enabled + Development + canonical legacy source ⇒ backfill applied once with 7 `Create`
   items, `service`+`option` identity preserved as its own field, and each language keeping its own label.
2. **Idempotency** — second pass re-reads, plans only `Skip`, and never opens a transaction (`ApplyAsync`
   not called again).
3. **Fail-closed (gate off)** — zero calls to seed/reader/store; no rows.
4. **Fail-closed (non-Development)** — zero calls to seed/reader/store.
5. **Fail-closed (non-executable plan)** — plan violations ⇒ `ApplyAsync` never called; nothing mutated.

## Non-interference

- Shared instances `:5088` / `:3000`, `tooba_alpha` and all existing containers: **untouched**; no
  start/stop/DB write/appsettings edit; **no scratch Host startup** in this task.
- `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-120.task.md` (Bridge receipt) preserved **unstaged**.
- Pre-existing modified/untracked paths in the working tree (evidence txts, frontend files) preserved;
  none adopted, reset, stashed or reverted.

## Known pre-existing failures (not repaired, out of scope)

`Tooba.Host.Tests.Architecture` shows **43 pre-existing failures / 1413 passes** that are unrelated to
this change and already present at the base tree, e.g.:

- `HostAdminCanon001GuardTests.Contract_files_expose_no_persistence_types` — expects
  `Party.Contracts/IPartyAdminSellerReadGateway.cs` but the file lives at `Party.Contracts/Ports/…`.
- `HostSellerAmcR5GuardTests.Party_development_seam_…` — same stale `Party.Contracts` root path.
- `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
  — `Tooba.Catalog.Contracts.Cart` vs `Tooba.Catalog.Contracts` namespace drift.
- Story / ProductWorkspace / Payment SOT-guard mismatches.

None touches Fulfillment, Shipping, or the Program.cs Development composition. They were **not** repaired.

## Risks

- **Gate remains off by default**, so nothing is created in any tracked/shared environment until an
  operator enables `Tooba:ShippingOneLevelCatalog:Enabled` in a Development overlay — intended.
- The Host call is wrapped in `try/catch` (log-and-continue) like every other Development seed, so a
  bootstrap failure cannot abort startup.
- The in-memory test double models the persisted seam's contract but not real EF transactions; the
  transactional apply was already proven against real PostgreSQL in TB-090 evidence and is unchanged here.

## Rollback

Delete the two new files and revert the 16-line `Program.cs` addition. No data migration, no schema
change, no config change — nothing to roll back in any database.

## First remaining blocker / next minimal step (needs Architect authorization; not started)

Runtime/UI acceptance on an isolated environment (gate enabled in the disposable overlay) to observe
non-empty one-level `methods[]` and storefront label parity — that belongs to a separately approved
TB-121. **No UI parity or commercial-readiness claim is made here.**
