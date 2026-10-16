TB-SHIPPING-ONE-LEVEL-TABS-114 — ROOT CAUSE, BEFORE/AFTER CALL FLOW, NEGATIVE CONTROL
=====================================================================================

1. The failing boundary (TB-113 observation, reproduced by source trace)
----------------------------------------------------------------------
Program.cs (before):
    if (catalogDemoOptions.RunLegacyBootstraps)
    {
        await DevelopmentSchemaMigrator.ApplyAsync(app.Services);            // line 367
        await CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(            // line 368
            app.Services, CancellationToken.None);                          //  <-- ROOT provider
    }

`app.Services` is the composition-root provider. `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync`
does not create a scope; it forwards that same provider to the six Catalog-owned legacy bootstraps:

    CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(IServiceProvider provider, ct)   CatalogDevelopmentSeed.cs:46
      -> FashionTemplateCatalogSeed.ApplyAsync(provider, ct)                            CatalogDevelopmentSeed.cs:61
           -> provider.GetRequiredService<CatalogDbContext>()                           FashionTemplateCatalogSeed.cs:59
      -> IndustryBatchATemplateCatalogSeed.ApplyAsync(provider, ct)                     CatalogDevelopmentSeed.cs:62
      -> IndustryBatchBTemplateCatalogSeed.ApplyAsync(provider, ct)                     CatalogDevelopmentSeed.cs:63
      -> IndustryBatchCTemplateCatalogSeed.ApplyAsync(provider, ct)                     CatalogDevelopmentSeed.cs:64
      -> LandingPageDevelopmentSeed.ApplyAsync(provider, ct)                            CatalogDevelopmentSeed.cs:65
      -> StoreMenuDevelopmentSeed.ApplyAsync(provider, ct)                              CatalogDevelopmentSeed.cs:66

Every one of the six resolves SCOPED module services from that provider:

    FashionTemplateCatalogSeed.cs:59            provider.GetRequiredService<CatalogDbContext>()
    IndustryBatchATemplateCatalogSeed.cs:118    provider.GetRequiredService<CatalogDbContext>()
    IndustryBatchBTemplateCatalogSeed.cs:118    provider.GetRequiredService<CatalogDbContext>()
    IndustryBatchCTemplateCatalogSeed.cs:118    provider.GetRequiredService<CatalogDbContext>()
    LandingPageDevelopmentSeed.cs:27-28         provider.GetRequiredService<CatalogDbContext>()
                                                provider.GetRequiredService<IStoreLandingPageWorkspace>()
    StoreMenuDevelopmentSeed.cs:23-24           provider.GetRequiredService<CatalogDbContext>()
                                                provider.GetRequiredService<IStoreMenuWorkspace>()

`CatalogDbContext` is registered scoped (module DbContext registration). In Development the Host enables
scope validation, so the first resolution from the root provider throws:

    System.InvalidOperationException: Cannot resolve scoped service
      'Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext' from root provider.
      at ...FashionTemplateCatalogSeed.ApplyAsync(IServiceProvider provider, CancellationToken ct)
        FashionTemplateCatalogSeed.cs:line 59
      at ...CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(...) CatalogDevelopmentSeed.cs:line 61
      at Program.<Main>$(String[] args)  Program.cs:line 368

Contrast with the proven sibling path: `DevelopmentSchemaMigrator.ApplyCoreAsync` creates its own
async scope (`DevelopmentSchemaMigrator.cs:46-47`) and passes `scope.ServiceProvider` to every
migrator and module post-migration step — which is why the Catalog attribute-schema seed, the Party
schema and the legacy workspace demo seed all succeeded in the same startup.

Ownership reading: the module keeps seeding authority (it does not create scopes and does not know the
Host lifetime); the Host composition root owns the DI lifetime.

2. The repair (after)
---------------------
Program.cs (after), 1 file, +8/-1 lines:

    if (catalogDemoOptions.RunLegacyBootstraps)
    {
        await DevelopmentSchemaMigrator.ApplyAsync(app.Services);
        // TB-114: ... intentionally explains that the required scoped boundary is created here ...
        await using var legacyCatalogBootstrapScope = app.Services.CreateAsyncScope();
        await CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(
            legacyCatalogBootstrapScope.ServiceProvider,
            CancellationToken.None);
    }

Properties preserved:
  * ONE async scope encloses the WHOLE existing legacy Catalog bootstrap sequence (all six seeds), not
    merely the first DbContext resolution.
  * Order, once-only invocation and the group's position in the startup sequence are unchanged.
  * `await using` disposes the scope on success, on exception and on cancellation, because the scope is
    disposed while the exception unwinds out of the enclosing block.
  * Development-only gating and `RunLegacyBootstraps` semantics are unchanged.
  * No module API signature changed; no new route, flag, migration, contract, data change or framework.
  * `ValidateScopes` is not touched anywhere (no suppression, no workaround, no custom provider).
  * The Host still does not own seeding logic: it only owns the scope, and the Catalog-owned seam is
    still invoked by name from the composition root.

3. Negative control (performed, then restored)
----------------------------------------------
Program.cs was temporarily reverted to the pre-fix line
`await CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(app.Services, CancellationToken.None);`
and the focused guard test was executed:

    dotnet test ... --filter FullyQualifiedName~Program_hands_the_legacy_catalog_bootstraps_a_scoped_provider
    Assert.DoesNotContain() Failure: Sub-string found  (pos 21211)
    Found: "CatalogDevelopmentSeed.EnsureLegacyBootstraps..."    -> 1 failed / 1 total

The pre-fix code was then restored byte-for-byte (verified: 0 occurrences of the temporary marker in
Program.cs) and the same run passes. The guard therefore fails on the old hand-off and passes on the
repaired one, i.e. it is not a vacuous assertion.

4. Shared-environment non-interference
--------------------------------------
No Host was started, no shared process was stopped/restarted/killed, no SQL was executed against
`tooba_alpha` and no fixture or migration was run. Only reads and local builds/tests. Rechecked after
the work: `:5088` -> PID 35624 (health 200 {"status":"ok"}), `:3000` -> PID 31580 (308 -> /fa),
`/__platform-commerce` unchanged (store-alpha / tenant-alpha / dev-singlestore).

5. Rollback
-----------
`git revert <Code-SHA>` is a single-commit revert of one file (Program.cs) plus one new test file; no
schema, data, configuration or process consequence, and no runtime state to repair.
