TB-SHIPPING-ONE-LEVEL-TABS-114 — TEST / BUILD / GUARD RECORD
===========================================================

New focused test class
----------------------
src/backend/Host/Tooba.Host.Tests/CatalogLegacyBootstrapsDiScopeTests.cs (5 tests)

1. Root_provider_handoff_reproduces_the_tb113_lifetime_failure
   Real `ServiceCollection` + `BuildServiceProvider(ValidateScopes = true, ValidateOnBuild = true)`,
   scoped probe registration; resolving the scoped probe from the ROOT provider throws
   `InvalidOperationException` whose message contains "Cannot resolve scoped service" and
   "from root provider" — i.e. it pins the exact TB-113 failure mode rather than an approximation.
   Also asserts Program.cs contains no `ValidateScopes` / `ValidateOnBuild` (no suppression workaround).
2. Scoped_provider_handoff_succeeds_and_disposes_scoped_services
   Uses the identical call-site shape (`await using var scope = provider.CreateAsyncScope();` then
   `scope.ServiceProvider`): resolution succeeds, scoped identity is stable inside the scope, the
   `IAsyncDisposable` scoped service is disposed with the scope, the `IDisposable` scoped service is
   disposed with the scope, and a second scope receives fresh instances.
3. Scope_disposes_when_the_bootstrap_throws_or_is_cancelled
   The same shape with a throwing delegate and a cancelling delegate: the exception propagates
   unaltered AND the scoped service is still disposed — the `await using` unwind guarantee.
4. Program_hands_the_legacy_catalog_bootstraps_a_scoped_provider
   Composition guard on the REAL call site: Program.cs still contains
   `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync`; it must NOT contain
   `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(app.Services`; and a multi-line regex proves an
   async scope created from `app.Services` is the provider actually passed (`scope.ServiceProvider`).
5. Legacy_catalog_bootstrap_sequence_resolves_scoped_services_from_the_single_provider
   Structural proof that ONE scope is required for the WHOLE sequence: the six legacy bootstraps each
   receive the same single `provider` (`…ApplyAsync(provider, cancellationToken)` x6), each of the six
   seed files resolves `provider.GetRequiredService<CatalogDbContext>()`, and the module seam creates
   no scope of its own (boundary stays at the composition root).

Run (focused only):
  dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~CatalogLegacyBootstrapsDiScopeTests
  -> Passed!  Failed: 0, Passed: 5, Skipped: 0, Total: 5

Relevant existing Host Development composition guards (no broad suite run):
  dotnet test ... --filter "HostDevelopmentAmcGuardTests|HostAdminAmcW34TemplateSeedsGuardTests|HostAdminAmcLandingPageSeedGuardTests|HostAdminAmcStoreMenuGuardTests|HostDevelopmentEnricherClosureGuardTests|ContentDevelopmentSeedHostSourceTests|CatalogLegacyBootstrapsDiScopeTests"
  -> Passed: 25, Failed: 3, Total: 28
     The 3 failures are the pre-existing `Host_Admin_count_15_*` drift only (they assert 15 `*.cs`
     files under `src/backend/Host/Tooba.Host/Admin` while 17 exist). Proven pre-existing and
     unrelated: `git ls-tree -r --name-only <sha> -- src/backend/Host/Tooba.Host/Admin` gives 17 `*.cs`
     at the expected base 5c5c5d2a, at the earlier c862d6c, and at HEAD; TB-114 did not touch that
     folder. Every other guard test in those classes passes, including the Program.cs composition
     assertions (`DevelopmentSchemaMigrator.ApplyAsync` + `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync`).
  Narrow confirmation run of the three composition-guard classes: 11 passed / 2 failed (the two
  `Host_Admin_count_15_*` count tests only).

Build:
  dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Debug
  -> 0 Error(s)

NOT run on purpose: any Host launch, scratch PostgreSQL, demo reset/seed, the broad known-drifted Host
suite, and any browser flow (TB-115 owns runtime proof).
