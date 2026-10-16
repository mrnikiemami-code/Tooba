TB-SHIPPING-ONE-LEVEL-TABS-116 — ROOT CAUSE, AUTHORITY MAP, BEFORE/AFTER CALL FLOW
===============================================================================

1. The failing boundary (observed in TB-115, reproduced by source trace)
-----------------------------------------------------------------------
Program.cs (TB-114 state):
    if (catalogDemoOptions.RunLegacyBootstraps)
    {
        await DevelopmentSchemaMigrator.ApplyAsync(app.Services);                    // line 367
        await using var legacyCatalogBootstrapScope = app.Services.CreateAsyncScope(); // line 372
        await CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(
            legacyCatalogBootstrapScope.ServiceProvider, CancellationToken.None);   // line 374
    }

TB-114 fixed the DI LIFETIME (scoped provider, not the root provider). It did not populate the
commerce context, so the very next scoped resolution still failed:

    CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync            CatalogDevelopmentSeed.cs:61
      -> FashionTemplateCatalogSeed.ApplyAsync                    FashionTemplateCatalogSeed.cs:59
           -> provider.GetRequiredService<CatalogDbContext>()
                -> Catalog DbContext options factory            CatalogModule.cs:160
                     -> ToobaNpgsql.ResolveForContext           ToobaNpgsql.cs:57
                          commerce.Current ?? throw PlatformHttpException(
                              503, "Service Unavailable", "platform.edition.unconfigured")

`ICurrentCommerceContext.Current` is served by the SCOPED `HttpCommerceContextAccessor`
(Program.cs:131-135: HttpCommerceContextAccessor + ICurrentCommerceContext + ICurrentEdition +
ICurrentTenant + ICommerceContextAssigner all resolve to the same scoped instance). It is non-null
ONLY after `ICommerceContextAssigner.Assign(...)` is called on that same scope — otherwise it falls
back to `HttpContext.Items[ItemKey]`, which is empty at startup because no ASP.NET request exists.

Why the earlier stage does not hit it: `DevelopmentSchemaMigrator.ApplyCoreAsync`
(DevelopmentSchemaMigrator.cs:46-68) creates its own async scope AND assigns the trusted context in
that scope before any DbContext work:

    await using var scope = services.CreateAsyncScope();
    var provider = scope.ServiceProvider;
    var registry = provider.GetRequiredService<ControlPlaneRegistry>();
    if (!registry.Tenants.TryGetValue(DevelopmentTenantCommerceContext.DevelopmentTenantId, out var tenant)
        || tenant.Status != TenantStatus.Active)
        throw new InvalidOperationException("Development seed requires Active tenant store-alpha.");
    provider.GetRequiredService<ICommerceContextAssigner>().Assign(new CommerceContext(
        new EditionContext(registry.Edition, registry.DeploymentId),
        new TenantContext(tenant.TenantId, tenant.Status, tenant.ConnectionReference, tenant.DisplayName,
                          tenant.ThemeReference, tenant.DefaultMarketReference, tenant.Hosts[0], tenant.PrimaryDomain),
        tenant.ConnectionReference,
        seedCatalog ? "workspace-dev-seed" : "workspace-dev-schema"));

So the context is lost BETWEEN the migrator's scope (assigned) and the TB-114 sibling scope
(unassigned). That is the exact "where context is lost between scopes" answer.

Other Development startup scopes were audited and need no change (they each already assign):
  ContentDevelopmentSeedHost.cs:17-35, SupportDevelopmentSeedHost.cs:21-onwards,
  WalletDevelopmentSeedHost.cs:18-onwards, MarketplaceDevelopmentBootstrap.cs.
No hypothetical failure was fixed.

2. Authority map (unchanged by this task)
-----------------------------------------
  * Tenant/edition/connection-reference authority: `ControlPlaneRegistry`, built from Host
    configuration at startup (`Configuration/ControlPlaneRegistry.cs`). Never a request header.
  * Development tenant selection: `DevelopmentTenantCommerceContext.DevelopmentTenantId = "store-alpha"`,
    Active status required, fail-closed otherwise.
  * Context assignment: `ICommerceContextAssigner` (implemented by the scoped
    `HttpCommerceContextAccessor`); assignment is scope-local.
  * Connection string resolution authority: `IDatabaseConnectionResolver` via
    `ToobaNpgsql.ResolveForContext`; `ICurrentCommerceContext` only names the reference. The
    fail-fast 503 path and the `ConnectionReference` indirection are untouched.
  * Seeding authority: `CatalogDevelopmentSeed` + the six Catalog-owned legacy seeds. The Host still
    owns composition only.

3. The repair (after)
---------------------
Program.cs, 1 file, +9/-4:

    await DevelopmentSchemaMigrator.ApplyAsync(app.Services);
    // TB-116: ... comment explains the unassigned-scope failure ...
    await DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(
        app.Services,
        "legacy-catalog-dev-seed",
        CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync);

The canonical Host Development gateway (already on the accepted
`src/backend/Host/Tooba.Host/Development` allowlist and previously unused) does exactly the
migrator's proven hand-off:

    await using var scope = services.CreateAsyncScope();          // ONE scope for the whole sequence
    var provider = scope.ServiceProvider;
    var registry = provider.GetRequiredService<ControlPlaneRegistry>();
    if (!registry.Tenants.TryGetValue("store-alpha", out var tenant) || tenant.Status != TenantStatus.Active)
        return;                                                   // fail-closed, no assignment, no seed
    provider.GetRequiredService<ICommerceContextAssigner>().Assign(new CommerceContext(...));
    await seed(provider, CancellationToken.None);                 // the scoped, ASSIGNED provider

Properties preserved or improved:
  * ONE disposed async scope still covers the full six-seed legacy Catalog sequence; disposal happens
    on success, exception and cancellation (`await using` inside the gateway).
  * The context is assigned INSIDE that same scope BEFORE any scoped database dependency resolves.
  * No duplicated tenant selection, no hand-built CommerceContext in Program.cs, no synthetic
    HttpContext/actor/headers, no hardcoded connection string, no `tenant-alpha`, no cross-tenant
    context, no shared-database fallback.
  * Fail-closed preserved twice over: the migrator still throws for a missing/inactive store-alpha
    before this line, and the gateway itself declines to assign or seed.
  * Only the Development composition boundary changed. `CatalogDevelopmentSeed`,
    `DevelopmentSchemaMigrator`, `ToobaNpgsql`, module code, Contracts, migrations and appsettings
    are untouched; SingleStore/Marketplace edition behaviour is unchanged (the Marketplace branch is a
    sibling `if` and never reaches this code).
  * No DI lifetime change, no `ValidateScopes` suppression.

4. Why no new helper was introduced
-----------------------------------
The task preferred reusing "an existing narrowly scoped trusted setup seam rather than duplicating
tenant selection". `DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync` is exactly that
seam (already present, allowlisted, and unused until now), so no new Host composition file was added.

5. Rollback
-----------
`git revert <Code-SHA>`: one Program.cs hunk plus the focused test files; no schema, data,
configuration or process consequence.
