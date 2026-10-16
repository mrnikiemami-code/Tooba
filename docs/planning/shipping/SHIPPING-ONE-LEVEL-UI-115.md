# TB-SHIPPING-ONE-LEVEL-TABS-115 — isolated runtime proof of the TB-114 DI scope repair

Parent: TB-SHIPPING-ONE-LEVEL-TABS-114 · Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: `BOUNDED_ISOLATED_LEGACY_CATALOG_DI_RUNTIME_AND_HOST_HEALTH_PROOF` · Stop-State: `STOP_FOR_ARCHITECT`
Expected-Base-SHA: `af836cc992f9cdc40425d98d0169eba7c39b37e5` · OBSERVATION ONLY — no source change

## Verdict

**`PARTIAL`** — the TB-114 root-provider failure is **eliminated at runtime**, but the isolated Host does
**not** reach `/health 200`, and the **first subsequent blocker** is captured precisely. Nothing was coded
around it.

## What was launched (once)

One watchdog-bounded isolated Development Host (limit 180 s, `LAUNCH_COUNT=1`, no retry, no HTTP seed
endpoint called), from the current `main` publish that contains the TB-114 async-scope repair, on a fresh
no-volume disposable PostgreSQL (`127.0.0.1:55449`, task-owned `tooba_115_scratch` / `tooba_115_messaging`,
per-run credentials), Host on `http://127.0.0.1:5397`. The offline fail-closed effective-config guard
(`isolation-guard.json`, `failClosed=true`) confirmed: 1 Active `store-alpha` tenant, both database
references on scratch only, no `tooba_alpha` / `tenant-alpha` / port 5432 / prior scratch port,
Development environment, `RunLegacyBootstraps=true`, `AllowResetAndSeed=false`, and all three shipping
switches absent.

## Stage result

| Stage | Result |
| --- | --- |
| `DevelopmentSchemaMigrator.ApplyAsync` (Program.cs:367) | **COMPLETED** (migrations + Catalog attribute schema + full workspace demo seed chain) |
| `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(scope.ServiceProvider)` (Program.cs:374) | **REACHED** — former DI failure gone |
| `FashionTemplateCatalogSeed.ApplyAsync` | **NEW BLOCKER** at DbContext options construction |
| Kestrel listen / `/health` | never reached; process exited itself at **61 s** of the 180 s budget |

Whole-run markers: `Cannot resolve scoped service` = 0, `from root provider` = 0 (old blocker gone);
`catalog.attribute.code.duplicate` = 0, `catalog-dev.attribute.incompatible-reuse` = 0, `42P01` = 0
(no regression); `Now listening on` = 0, `Application started` = 0.

## First subsequent blocker (not fixed)

```
Tooba.BuildingBlocks.PlatformHttpException: Service Unavailable
  Tooba.Persistence.ToobaNpgsql.ResolveForContext            ToobaNpgsql.cs:57
  CatalogModule.<>c.<AddServices>b__2_8 (DbContext options)  CatalogModule.cs:160
  CatalogDbContext resolved via GetRequiredService
  FashionTemplateCatalogSeed.ApplyAsync                      FashionTemplateCatalogSeed.cs:59
  CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync         CatalogDevelopmentSeed.cs:61
  Program.<Main>$                                            Program.cs:374
```

The scope now resolves, but `ICurrentCommerceContext.Current` is `null` in that scope, so
`ToobaNpgsql.ResolveForContext` throws `503 / platform.edition.unconfigured`. `DevelopmentSchemaMigrator`
avoids this because it explicitly `assigner.Assign(...)`s a `CommerceContext` in its own scope before any
DbContext work; the Program.cs:374 scope has no such assignment and startup has no HTTP request to
establish one. The legacy Catalog bootstrap sequence needs the same commerce-context establishment — the
next bounded repair, deliberately out of scope here.

## Scratch read-back (row presence only, not eligibility)

Exactly one `color` definition; single shared `definition_id 01a14277-3514-7000-b1bc-99d267ba7ee1` for
PHONE-BLK-128, PHONE-BLU-256 and LIVE-SHIRT-BLK (TB-112 reuse holds at real startup);
`schema-mobile-demo-phone` Published with exactly 2 sparse variants and 1 default and a complete
active-offer / active-Base-price / positive-stock chain; `party.parties` = 4 with no 42P01;
store_landing_pages 3 and store_menus 1 (module-owned seeds inside the migrator scope); legacy
template_products 0 / store_templates 0 (legacy bootstraps never ran); scratch commerce-free
(carts 0, orders 0, shipping_methods 0).

## Isolation

Task-owned Host PID 40980 verified not alive; 5397 and 55449 listeners back to 0; only container
`tooba-115-scratch-pg` removed; ignored `.tmp-115-run` (incl. credentials) and `.tmp-115-host-out` deleted;
shared `:5088` PID 35624 / `:3000` PID 31580 / `/__platform-commerce` / `tooba_alpha` counts **identical**
to the preflight baseline. No INCIDENT.

## Next

One more bounded repair: establish the commerce context for the legacy Catalog bootstrap scope at the
Host composition boundary exactly as the migrator does, then **one** new isolated scratch startup to see
whether the six legacy bootstraps complete and `/health 200` is reached. No automatic next task claimed.
