TB-SHIPPING-ONE-LEVEL-TABS-115 — STAGE-BY-STAGE ISOLATED STARTUP PROOF
=====================================================================

Artifacts: one launch only. ISOLATED_HOST_PID=40980, absolute exe
.tmp-115-host-out\Tooba.Host.exe, ASPNETCORE_URLS http://127.0.0.1:5397, watchdog limit 180 s,
LAUNCH_COUNT=1, no retry, no HTTP seed endpoint ever called.

Publish source: HEAD af836cc992f9cdc40425d98d176... = af836cc992f9cdc40425d98d0169eba7c39b37e5
(i.e. current main containing the TB-114 async-scope repair), `dotnet publish -c Debug`, 0 errors.

Stage table
-----------
| # | Stage | Result |
| --- | --- | --- |
| 1 | Process start, host builder, DI container build | OK (no ValidateScopes/wiring error) |
| 2 | `DevelopmentSchemaMigrator.ApplyAsync(app.Services)` (Program.cs:367) | COMPLETED — 110 EF migration events + 1544 command events; two-phase ordered migration ran for catalog/inventory/localization/offer/party/pricing/promotion/tax |
| 3 | Catalog attribute/workspace demo seed inside the migrator's own async scope | COMPLETED — `attribute_definitions` 270 SQL touches, `attribute_options` 87, schema-mobile-demo-phone created with 2 variants + 1 default, workspace-live-shirt chain including the final admin-r3-archived-hat archive/outbox insert |
| 4 | `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(scope.ServiceProvider, …)` (Program.cs:374, TB-114 scope) | REACHED — the former DI-lifetime failure is GONE |
| 5 | `FashionTemplateCatalogSeed.ApplyAsync` → `provider.GetRequiredService<CatalogDbContext>()` (line 59) | Scope resolution now succeeds; the DbContext OPTIONS FACTORY throws instead |
| 6 | Host listen / Kestrel / `Now listening on` / `Application started` | NEVER REACHED (0 occurrences) |
| 7 | `GET /health` | NOT OBSERVED — process exited by itself at ELAPSED_SECONDS=61 |

Former blocker: ELIMINATED
--------------------------
Over the whole 1657-line stdout: `Cannot resolve scoped service` = 0, `from root provider` = 0.
The repaired call site is the one on the stack (Program.cs:line 374). So the TB-114 correction is
runtime-proven for the exact failure TB-113 captured, and no relaunch was needed to see it.

Also unchanged/absent: `catalog.attribute.code.duplicate` = 0, `catalog-dev.attribute.incompatible-reuse` = 0,
`42P01` = 0.

FIRST SUBSEQUENT BLOCKER (exact, not fixed here)
-----------------------------------------------
Unhandled exception. Tooba.BuildingBlocks.PlatformHttpException: Service Unavailable
   at Tooba.Persistence.ToobaNpgsql.ResolveForContext(ICurrentCommerceContext commerce, IDatabaseConnectionResolver resolver)
      src/backend/BuildingBlocks/Tooba.Persistence/ToobaNpgsql.cs:line 57
   at Tooba.Catalog.Infrastructure.CatalogModule.<>c.<AddServices>b__2_8(IServiceProvider sp, DbContextOptionsBuilder options)
      src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs:line 160
   at Microsoft.Extensions.DependencyInjection.EntityFrameworkServiceCollectionExtensions.CreateDbContextOptions[...]
   at ...GetRequiredService(IServiceProvider provider, Type serviceType)
   at Tooba.Catalog.Infrastructure.Development.FashionTemplateCatalogSeed.ApplyAsync(IServiceProvider provider, CancellationToken cancellationToken)
      src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/FashionTemplateCatalogSeed.cs:line 59
   at Tooba.Catalog.Infrastructure.Development.CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(IServiceProvider provider, CancellationToken cancellationToken)
      src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogDevelopmentSeed.cs:line 61
   at Program.<Main>$(String[] args) in src/backend/Host/Tooba.Host/Program.cs:line 374

Meaning: the scoped `CatalogDbContext` is now resolvable from the handed-over provider (TB-114 works),
but its options factory asks `ICurrentCommerceContext.Current` for the connection reference and gets
`null`, so `ToobaNpgsql.ResolveForContext` throws `503 / platform.edition.unconfigured`:

    var current = commerce.Current ?? throw new PlatformHttpException(503, "Service Unavailable", "platform.edition.unconfigured");

Blocker-Code: `platform.edition.unconfigured` (Host-visible as HTTP 503 class, thrown at startup).

Why the earlier stage did not hit it: `DevelopmentSchemaMigrator.ApplyCoreAsync` explicitly assigns the
commerce context inside its own scope before touching any DbContext:

    var assigner = provider.GetRequiredService<ICommerceContextAssigner>();
    assigner.Assign(new CommerceContext(new EditionContext(...), new TenantContext(...), tenant.ConnectionReference, "workspace-dev-seed"));

That assignment lives in the migrator's scope only. The new scope created at Program.cs:374 has no
assigned commerce context (and no ASP.NET request exists at startup to establish one), so the first
DbContext resolution after the migrator returns fails. The legacy Catalog bootstrap sequence therefore
needs the same commerce-context establishment as the migrator — this is the next bounded repair and is
deliberately NOT fixed in this observation-only task.

Scratch data read-back (row presence only, NOT storefront-eligibility proof)
--------------------------------------------------------------------------
Migration history: catalog 29, inventory 5, localization 1, offer 3, party 2, pricing 1, promotion 2, tax 1.
Definitions: color 1, ram 1, screen_size 1, storage 1 — exactly ONE color definition.
Options: color/black 1, color/blue 1, ram/8gb 1, storage/128gb 1, storage/256gb 1 — no duplicate option.
Single-identity proof: PHONE-BLK-128, PHONE-BLU-256 and LIVE-SHIRT-BLK all store the SAME color
definition_id `01a14277-3514-7000-b1bc-99d267ba7ee1` (and PHONE-BLK-128 shares LIVE-SHIRT-BLK's
canonical black value) — the legacy seed reused the schema-owned definition instead of duplicating it.
Products: schema-mobile-demo-phone Published with exactly 2 variants and exactly 1 default
(PHONE-BLK-128); workspace-live-shirt Published; admin-r3-draft-scarf Draft; admin-r3-archived-hat Archived.
Sellable chain for schema-mobile-demo-phone: PHONE-BLK-128 (default) 2 offers / 2 active / 1 active Base
price / available 5.000000; PHONE-BLU-256 1 offer / 1 active / 1 active Base price / available 5.000000.
Totals: offers 5, active_offers 5, prices 6, active_base_prices 4, stock_positions 6, positive on_hand 5,
seller_parties 4 (party.parties, i.e. no 42P01), tax_rules 1.
Rows from the module-owned Development seeds inside the migrator scope: store_landing_pages 3, store_menus 1;
the six LEGACY bootstraps reached in this run produced nothing (they never ran): template_products 0,
store_templates 0.
Scratch remains commerce-free: carts 0, seller_orders 0, shipping_methods 0. No shipping flag was changed,
no cart/order was created.

Cleanup / non-interference
--------------------------
Task-owned Host PID 40980 verified not alive; listeners on 5397 = 0. `docker rm -f tooba-115-scratch-pg`
removed the only task-owned container (no 115 container remains, no 113/111 container was touched);
listeners on 55449 = 0 after removal. Ignored `.tmp-115-run` (including the per-run credential file) and
`.tmp-115-host-out` deleted. No name/wildcard/broad termination; nothing on a shared port was ever a target.
Read-only shared postflight: :5088 -> PID 35624 `/health` 200, :3000 -> PID 31580 -> 308 /fa,
`/__platform-commerce` unchanged (store-alpha / tenant-alpha / dev-singlestore / theme-alpha / IR), and
tooba_alpha counts byte-identical (methods 7, translations 14, carts 50, cart_lines 78, drafts 14,
seller_orders 26, products 567, offers 3223). No INCIDENT.
