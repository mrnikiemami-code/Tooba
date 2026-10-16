# TB-SHIPPING-ONE-LEVEL-TABS-113 — isolated runtime proof (observation only)

Parent: TB-SHIPPING-ONE-LEVEL-TABS-112 · Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: `BOUNDED_ISOLATED_CATALOG_ATTRIBUTE_COLLISION_RUNTIME_PROOF` · Stop-State: `STOP_FOR_ARCHITECT`
Expected-Base-SHA: `8f00b8f528291ff5a311cfa0e5ee4f2deba9dec7` · Code-SHA: `NONE` (no source change)

## Verdict

**`PARTIAL`** — the TB-111 collision is demonstrably gone and the first subsequent barrier is precisely
captured, with complete teardown and shared parity. The single permitted launch did not reach `/health`
because a **different, later** startup step aborts.

## Verdict table

| Question | Result |
| --- | --- |
| `catalog.attribute.code.duplicate` recovered at real startup? | **NO** — 0 occurrences in 1,657 stdout lines |
| `catalog-dev.attribute.incompatible-reuse` fired? | **NO** — 0 occurrences |
| `party.parties` 42P01 recurring? | **NO** — 0 occurrences; `party.parties` resolves, 4 rows |
| One compatible `color` definition / `black` option? | **YES** — exactly 1 each; legacy variant reuses the same `definition_id` |
| Published `schema-mobile-demo-phone`, 2 sparse variants, 1 default? | **YES** |
| Active Base-priced offers with positive stock? | **YES** for both schema variants (row-count/integrity only) |
| `/health` 200 before the watchdog? | **NO** — process exited itself at 61 s of 180 s |
| New first blocker | `InvalidOperationException: Cannot resolve scoped service 'CatalogDbContext' from root provider` at `FashionTemplateCatalogSeed.ApplyAsync:59` |

## First new blocker

```
System.InvalidOperationException: Cannot resolve scoped service
  'Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext' from root provider.
  at Tooba.Catalog.Infrastructure.Development.FashionTemplateCatalogSeed.ApplyAsync(...)
        FashionTemplateCatalogSeed.cs:line 59
  at Tooba.Catalog.Infrastructure.Development.CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(...)
        CatalogDevelopmentSeed.cs:line 61
  at Program.<Main>$(String[] args)  Program.cs:line 368
```

`FashionTemplateCatalogSeed.ApplyAsync` line 59 resolves the scoped `CatalogDbContext` from the provider
it is handed. `Program.cs:368` hands it the **root** `app.Services`, whereas
`DevelopmentSchemaMigrator.ApplyCoreAsync` creates an async scope and passes the **scoped** provider
(`DevelopmentSchemaMigrator.cs:46-47`) — which is why every earlier seed, including the whole workspace
demo seed chain, succeeded. TB-111 never reached `Program.cs:368`, and TB-112 did not touch any of these
files. No fix was attempted (Mission A forbids coding).

Last completed stage: the entire `DevelopmentSchemaMigrator.ApplyAsync(app.Services)` call at
`Program.cs:367`; its final SQL statement was the archive update of `admin-r3-archived-hat`, the last
step of the workspace demo seed's Admin R3 enrichment.

## Evidence highlights

- Exactly one `color` definition and exactly one `black` option; and
  `workspace-live-shirt / LIVE-SHIRT-BLK / color` stores the **same** `definition_id`
  (`01a14266-d5b4-7000-9836-fedb228ab758`) as `schema-mobile-demo-phone / PHONE-BLK-128 / color` →
  runtime proof that the legacy seed reused rather than re-created the schema seed's definition.
- `schema-mobile-demo-phone` Published, 2 sparse variants, 1 default (`PHONE-BLK-128`), both axes differ.
- Both schema variants: Active offer + Active Base price + positive available stock. **Not** a
  customer/cart/storefront eligibility claim.
- Scratch remains commerce-free: carts 0, seller_orders 0, shipping_methods 0.

## Isolation, teardown, parity

- Fail-closed offline guard (`isolation-guard.json`): 1 Active tenant `store-alpha:Active:tenant-113-scratch`,
  scratch-only connection references, no `tooba_alpha` / `tenant-alpha` / `Port=5432` / prior TB-111 scratch
  port, shipping authorities OFF, `AllowResetAndSeed=false`.
- ONE launch: PID 6192, absolute exe path, watchdog ≤ 180 s, no retry, isolated port 5396, scratch PostgreSQL
  on 55448 with task-owned `tooba_113_scratch` + `tooba_113_messaging` databases.
- Teardown: PID 6192 already gone; only `tooba-113-scratch-pg` removed; `.tmp-113-run`/`.tmp-113-host-out`
  deleted; 0 listeners on 5396 and 55448. No name/wildcard kill, no shared process touched.
- Shared parity identical before/after: `:5088` → PID 35624, `:3000` → PID 31580, `/health` 200,
  `/__platform-commerce` unchanged, and `tooba_alpha` counts 7 / 14 / 50 / 78 / 14 / 26 / 567 / 3223.

## Next (Architect)

The Development legacy-bootstrap stage is now the single remaining empty-scratch startup barrier: the
`Program.cs:368` root-provider hand-off into `CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync`. The
Architect should decide whether to authorise a bounded repair task for that DI lifetime hand-off (or for
the seed's own scope creation), and then re-run one isolated scratch startup to observe the next stage.
No automatic next implementation task is proposed here.
