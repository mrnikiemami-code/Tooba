# SHIPPING-ONE-LEVEL-UI-109 — Isolated runtime proof after the sparse default-variant seam

Task: `TB-SHIPPING-ONE-LEVEL-TABS-109` (Parent: `TB-SHIPPING-ONE-LEVEL-TABS-108`)
Mode: `BOUNDED_ISOLATED_CATALOG_BOOTSTRAP_POST_DEFAULT_PROOF`
Stop-State: `STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
Expected-Base-SHA: `729fa793a58508ab05e207a7a60ebec0cfda19d9` · Base-SHA / HEAD at claim: `3bd045fc5191ba50a0ff6a082477cbd015269356`

## Verdict

**PARTIAL.** The original blocker is **resolved** — with 108's sparse default-variant seam, the Development
attribute-schema seed's product now **publishes** on an empty scratch database and the `missing=[variants]` refusal
never occurs. Startup then proceeded to the next stage and stopped at a different, precisely identified barrier on an
empty database: the Catalog-owned sellable enricher resolves a **seller party before the Party schema is migrated**
(`42P01 relation "party.parties" does not exist`). No offer, price or stock row exists. Per the task, scope was not
broadened to fix it: one launch, no retry, no source change.

## The two answers, in order

```text
ANSWER 1 (original blocker)   RESOLVED
  PublishReadiness diagnostic lines ......... 0
  'محصول برای انتشار آماده نیست.' ............ 0
  schema-mobile-demo-phone status ........... Published      (TB-106 was refused exactly here)
  defaults = 1 | total_variants = 2 | axes = color(0), storage(1)
  PHONE-BLK-128 is_default = true ; PHONE-BLU-256 is_default = false ; distinct fingerprints

ANSWER 2 (first new barrier)  Npgsql.PostgresException 42P01: relation "party.parties" does not exist
  PartyDevelopmentSeedGateway.ResolveDevelopmentSellerPartyAsync:28
    <- CatalogAttributeSchemaSellableEnricher.EnsurePublishedAndSellableAsync:80
    <- CatalogAttributeSchemaDevelopmentSeed.InvokeEnricherAsync:118 / ApplyAsync:110
    <- CatalogDevelopmentSeed.PostMigration.EnsureAsync:38
    <- DevelopmentSchemaMigrator.MigrateOrderedAsync:143  <- ApplyCoreAsync:70/112  <- Program.cs:367
  offer.offers / offer.prices / party.parties / inventory.stock_items all to_regclass() = NULL
  schemas present after the run: catalog, public  ONLY
```

## Why the barrier appears now (observation, not a fix)

`DevelopmentSchemaMigrator.MigrateOrderedAsync` runs each module's post-migration steps immediately after that
module's own migrator (lines 136–144). The Catalog post-migration step therefore executes before the
Party/Offer/Pricing/Inventory/Tax migrators, while the Catalog seed already calls a cross-module sellable enricher
that needs those schemas. On an empty database the enricher's first cross-module call fails. TB-106 never saw this
because it died one step earlier at the variant-default publish refusal.

## Bounds

`LAUNCH_COUNT=1` · watchdog `180 s` · elapsed `47 s` · no retry · `src/**` untouched · `RunLegacyBootstraps=true`
with `AllowResetAndSeed=false` · no shipping gate enabled · isolation guard passed (1 tenant, scratch-only refs, no
`tooba_alpha`, no `Port=5432`) · scratch container/databases/dirs/credentials destroyed · 0 stray listeners ·
shared `:5088` **same PID 35624**, `/health` 200, byte-identical provenance, unchanged `tooba_alpha` counts. Unlike
TB-106 there is **no deviation**: no name-based process termination was used anywhere.

## What is not proven

No storefront-label proof, no customer-cart projection, no Checkout proof and no commercial-readiness claim. Nothing
here claims the enricher ordering is defective — only that it is the first barrier after the original blocker.
