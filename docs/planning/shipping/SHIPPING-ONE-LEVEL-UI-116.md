# TB-SHIPPING-ONE-LEVEL-TABS-116 — legacy Catalog bootstrap commerce-context scope repair

Parent: TB-SHIPPING-ONE-LEVEL-TABS-115 · Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: `BOUNDED_DEVELOPMENT_LEGACY_CATALOG_COMMERCE_CONTEXT_SCOPE_REPAIR` · Stop-State: `STOP_FOR_ARCHITECT`
Expected-Base-SHA: `2ea499e98026a522f0bb98a2cce2cc5fe9f9cae5` · SOURCE + FOCUSED TESTS ONLY

## Verdict

**`PASS`** — the TB-115 missing-CommerceContext root cause has a source-backed minimal fix, focused
evidence shows the trusted context is assigned inside the same scoped lifetime before any scoped
database dependency resolves, the fail-closed guards survive, and the relevant build/tests pass.
No runtime claim: **TB-117** owns the next isolated startup.

## Root cause (TB-115, reproduced)

TB-114 fixed the DI **lifetime** (scoped provider instead of the root provider) but the TB-114 scope was
never **assigned** a commerce context. The scoped `CatalogDbContext` options factory
(`CatalogModule.cs:160`) then called `ToobaNpgsql.ResolveForContext` (`ToobaNpgsql.cs:57`), which threw
`503 platform.edition.unconfigured` because `ICurrentCommerceContext.Current` was `null` in that scope.

`ICurrentCommerceContext` is served by the scoped `HttpCommerceContextAccessor` and is non-null only
after `ICommerceContextAssigner.Assign(...)` runs on that same scope. `DevelopmentSchemaMigrator`
does exactly that inside its own scope — which is why everything before the sibling scope succeeded.
**The context was lost between the migrator's assigned scope and the TB-114 unassigned scope.**
All other Development startup scopes (`Content`/`Support`/`Wallet`/`Marketplace` hosts) already assign;
they were audited and deliberately left alone.

## The repair

One file, +9/−4 in `Program.cs`: the Host composition root now delegates the legacy Catalog sequence to
the **already allowlisted but previously unused** canonical gateway
`DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(app.Services, "legacy-catalog-dev-seed",
CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync)`. That gateway creates one async scope, resolves the
configured `store-alpha` tenant from `ControlPlaneRegistry`, assigns the trusted `CommerceContext`, and
only then invokes the Catalog-owned seed seam on that same scoped provider.

Preserved: one disposed scope for the whole six-seed sequence (disposal on success/exception/cancel);
ordering, flags and abort behaviour; fail-closed for missing/inactive tenant (twice: the migrator still
throws, and the gateway declines); SingleStore and Marketplace edition behaviour; the `ToobaNpgsql`
fail-fast path and `ConnectionReference` indirection; module seeding authority.
Rejected: duplicated tenant selection, a hand-built `CommerceContext` in the Host, synthetic
HttpContext/actor/headers, hardcoded `store-alpha` connection values, `tenant-alpha`, any shared-database
fallback, any DI lifetime change or `ValidateScopes` suppression. No new Host composition file was added
because the seam already existed.

## Proof

| Item | Result |
| --- | --- |
| New focused tests | 7 cases (5 facts + 1 theory × 2) — assigned-context-before-DbContext, fail-closed Disabled/Suspended, fail-closed absent, unassigned-scope negative control, fresh scope + disposal on success/throw, Program.cs composition guard |
| Updated TB-114 test | now asserts the seed seam is reachable only as the gateway's delegate |
| Focused run | **12 passed / 0 failed / 12 total** |
| Relevant existing Development guards | 55 passed, 4 failed — all 4 pre-existing (`Host_Admin_count_15_*` ×3; stale `IPartyDevelopmentSeedGateway.cs` path) |
| Bounded negative control | pre-fix TB-114 composition → both Program.cs guards FAIL; fix restored → both pass, 0 marker occurrences |
| Host build | `dotnet build .../Tooba.Host.csproj -c Debug` → 0 errors |
| Shared (read-only) | `:5088` PID 35624 health 200, `:3000` PID 31580, `tooba_alpha` counts identical; nothing started/stopped/mutated |

## Evidence

`docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-116/` — `root-cause.md` (call flow + authority
map), `minimal-diff.txt`, `test-build-record.md`, `negative-control.txt`, `focused-tests.txt`,
`guard-tests.txt`, `shared-readonly-check.txt`, `task.md`, `task-receipt.json`, `post-result.js`,
`RESULT.bridge.txt`. No secrets, credentials, tokens, connection strings or customer data.

## Next

**TB-117**: one isolated scratch Development startup to observe whether the six legacy Catalog
bootstraps now complete and `/health 200` is reached, and to capture the next first blocker. Rollback is
a single commit revert with no data or process consequence.
