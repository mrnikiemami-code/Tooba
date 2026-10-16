# TB-SHIPPING-ONE-LEVEL-TABS-114 — Development legacy Catalog bootstrap DI scope repair

Parent: TB-SHIPPING-ONE-LEVEL-TABS-113 · Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: `BOUNDED_DEVELOPMENT_CATALOG_LEGACY_SEED_DI_SCOPE_REPAIR` · Stop-State: `STOP_FOR_ARCHITECT`
Expected-Base-SHA: `5c5c5d2a0072e4ed965063776c2bf299d5d05b7f` · Source + focused tests only

## Verdict

**`PASS`** — the legacy Catalog Development bootstrap sequence now receives a real scoped provider,
the original root-provider failure is prevented by structural + test proof (with a negative control),
and the scoped objects are disposed on success, exception and cancellation. **No** runtime claim:
TB-115 must still prove an isolated Host startup.

## The failing boundary (TB-113, reproduced by source trace)

`Program.cs` passed the composition-root provider straight into the Catalog-owned seam:

`Program.cs:368` → `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(app.Services, …)`
→ `FashionTemplateCatalogSeed.ApplyAsync` (`CatalogDevelopmentSeed.cs:61`)
→ `provider.GetRequiredService<CatalogDbContext>()` (`FashionTemplateCatalogSeed.cs:59`) → **root
provider** → `InvalidOperationException: Cannot resolve scoped service … from root provider`.

All **six** legacy bootstraps in that sequence resolve scoped services from the one provider they are
given: `CatalogDbContext` in every seed, plus `IStoreLandingPageWorkspace` and `IStoreMenuWorkspace` in
the last two. The proven sibling path (`DevelopmentSchemaMigrator.ApplyCoreAsync`) already creates its
own async scope, which is why everything before this line succeeded.

## The repair

One file, +8/−1: the composition root creates the required async scope and hands its `ServiceProvider`
to the unchanged Catalog-owned seam.

```csharp
await using var legacyCatalogBootstrapScope = app.Services.CreateAsyncScope();
await CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(
    legacyCatalogBootstrapScope.ServiceProvider,
    CancellationToken.None);
```

Preserved: one scope covers the whole existing sequence (not just the first resolution); order,
once-only invocation, group position, Development gating and `RunLegacyBootstraps` semantics;
exception/sheath propagation; module ownership of seeding (the module creates no scope of its own).
Not done: no `ValidateScopes` suppression, no root workaround, no custom provider, no signature change,
no new route/flag/migration/contract/data change, no seeding authority moved into the Host.

## Proof

| Item | Result |
| --- | --- |
| New focused tests | 5 passed / 0 failed |
| Negative control (pre-fix root hand-off) | guard test fails (`Assert.DoesNotContain` hit) → restored → passes |
| Relevant existing composition guards | 25 passed, 3 failed — all 3 are the pre-existing `Host_Admin_count_15_*` drift |
| Pre-existing drift proof | `Host/Tooba.Host/Admin` holds 17 `*.cs` at base `5c5c5d2a`, at `c862d6c`, and at HEAD (untouched by TB-114) |
| Host build | `dotnet build .../Tooba.Host.csproj -c Debug` → 0 errors |
| Disposal | async + sync scoped services disposed with the scope; also on thrown and cancelled path |
| Shared environment | untouched: `:5088` PID 35624 (health 200), `:3000` PID 31580, `/__platform-commerce` unchanged |

## Evidence

`docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-114/` — `root-cause.md`, `minimal-diff.txt`,
`test-build-record.md`, `focused-tests.txt`, `guard-composition-tests.txt`, `shared-non-interference.txt`,
`task.md`, `task-receipt.json`, `post-result.js`, `RESULT.bridge.txt`.

## Next

TB-115 runtime proof only: **one** isolated scratch Development Host startup (watchdog ≤ 180 s, empty
disposable PostgreSQL, distinct scratch port/tenant, fail-closed config guard, absolute-PID cleanup) to
confirm the DI blocker is gone, whether the remaining legacy bootstraps (fashion/industry templates,
landing pages, store menu) complete, whether `/health` reaches 200, and to capture the next first
blocker. Rollback is a single commit revert with no data or process consequence.
