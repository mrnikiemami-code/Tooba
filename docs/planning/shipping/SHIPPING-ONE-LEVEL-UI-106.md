# SHIPPING-ONE-LEVEL-UI-106 — Isolated capture of the real product-publish missing codes

Task: `TB-SHIPPING-ONE-LEVEL-TABS-106` (Parent: `TB-SHIPPING-ONE-LEVEL-TABS-105`)
Mode: `BOUNDED_ISOLATED_CATALOG_PUBLISH_MISSING_CODES_CAPTURE`
Stop-State: `STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
Expected-Base-SHA: `2699e53e877a8cbb6f084d1a93e0410aa9950182` · Base-SHA: `46170447fe5da00cd65be455efcfd6146e720ff6`

## Question this task had to answer

105 made the refusal verbose but only proved the seam fires on the schema-demo publish; the actual
`ProductPublishReadiness.Missing[]` codes for the failing scratch product were still **NOT OBSERVED**. 106 was
diagnosis/proof only: obtain those codes with **one** bounded, isolated startup, then stop.

## Answer

```
stage=catalog-dev.attribute-schema.publish-product
product=01a1421c-5ca6-7000-b43d-9797d12fc02d
isReady=False
missing=[variants]
```

obtained from one real isolated scratch run (single launch, 50 s, outer watchdog 240 s), with the original refusal
unchanged underneath it:

```
Unhandled exception. System.InvalidOperationException: محصول برای انتشار آماده نیست.
   at ...CatalogDirectory.UnwrapLifecycle ... CatalogDirectory.cs:line 849
   at ...CatalogDevelopmentPublishDiagnostics.PublishWithDiagnosticsAsync ... line 75 / line 84
   at ...CatalogAttributeSchemaDevelopmentSeed.ApplyAsync ... CatalogAttributeSchemaDevelopmentSeed.cs:line 101
```

## Narrowed to one sub-rule (with scratch data, not guesswork)

`ProductVariantDirectory.GetReadinessAsync` reduces to

```
isValid = missingAxes == 0 && invalidVariants == 0 && duplicates == 0 && noDefault != true
```

The scratch database eliminated three of the four: the axes are correctly bound (`color`, `storage`), both variants
carry both axes with GUID enum values, and the two fingerprints differ. What remained:

```
PHONE-BLK-128  is_default = f
PHONE-BLU-256  is_default = f      -> noDefault = !Any(IsDefault) = true  -> isValid = false
```

**Root cause:** the Development attribute-schema seed creates variants through the direct create path
(`CreateVariantAsync` → `CreateWorkspaceVariantAsync`), which never calls `SetDefault` / `EnforceSingleDefault`; only
the Admin matrix-apply path enforces a single default. Publish readiness therefore requires a default variant that
the seed never creates — which is exactly why this is invisible on the shared database and fatal on an empty one.

Nothing was fixed: `src/**` is untouched by this task, no readiness rule was relaxed, and no synthetic codes were
produced.

## What was and was not proved

| Proved | Not proved (out of scope) |
| --- | --- |
| `missing=[variants]` observed on a real isolated run | that the seed correction publishes successfully |
| the single failing sub-rule is `noDefault` | a sellable offer on scratch |
| original refusal retained byte-for-byte in behaviour | storefront labels / checkout / commercial shipping |
| isolation, watchdog, cleanup and shared parity | any commercial readiness claim |

## Isolation, watchdog and parity

One launch, `LAUNCH_COUNT=1`, `WATCHDOG_LIMIT_SECONDS=240`, `ELAPSED_SECONDS=50`, `KILLED_BY_WATCHDOG=False`,
`DIAGNOSTIC_LINE_CAPTURED=True`, `LISTENERS_ON_5392_AFTER_STOP=0`. The isolated configuration had exactly one tenant
(`store-alpha:Active:tenant-106-scratch`), scratch-only connection references, no `tooba_alpha`, no `Port=5432`, no
shipping gate (`ShippingOneLevelCatalog` / `ShippingRateAuthority` / `ShippingOperationalShadow` all absent), and
`RunLegacyBootstraps=true` with `AllowResetAndSeed=false` so the forbidden demo reset/seed could not run.

Scratch container `tooba-106-scratch-pg` (postgres:17, no volume, loopback-only `55444`) plus
`.tmp-106-run` / `.tmp-106-host-out` are destroyed; 0 listeners on `5392` / `55444`; only `postgres-db` and `rabbitmq`
remain. Shared read-only parity is unchanged: `:5088` `/health` 200 with the same provenance and identical
`tooba_alpha` counts (`7` methods / `14` translations / `50` carts / `78` lines / `14` drafts / `26` seller orders /
`567` products / `3223` offers) before and after; no writes to shared databases.

**Disclosed deviation:** during teardown a name-based `Get-Process -Name Tooba.Host | Stop-Process` also matched the
pre-existing shared Development Host on `:5088`. The mandatory post-task health check caught it immediately and the
Host was restarted from the same exe, working directory, port and safe defaults; provenance and counts verified
identical afterwards (only the PID changed). Full detail in `isolation-watchdog-and-parity.txt`.

## Recommended next step (Architect decision, not executed)

One bounded Catalog-owned seed correction: designate exactly one created variant as the product default through the
existing public contract that already enforces a single default, then publish. The Architect should rule whether the
defect belongs to the seed (must set a default) or to the create-path contract (should enforce a default when none
exists). No rule relaxation, no direct DML, no scope beyond Catalog.

## Artefacts

- `observed-result.txt` — the observed codes, the retained refusal, the sub-rule narrowing.
- `redacted-diagnostic-excerpt.txt` — bounded redacted log excerpt (raw 683 KB logs dropped).
- `isolation-watchdog-and-parity.txt` — isolation guard, watchdog, cleanup, parity, deviation.
- `code-to-prerequisite-mapping.txt` — six readiness domains mapped, and the exact single blocker.
- `scratch-variant-inspection.{sql,txt}` — the read-only scratch queries/exports that pinned `noDefault`.
- `build-record.txt`, `watchdog-result.txt`, `prepare-output.txt`, `isolated/*` — reproducibility.
- `shared-parity-preflight.*`, `shared-parity-postflight.*` — read-only shared parity.
