# TB-SHIPPING-ONE-LEVEL-TABS-106 — summary

**Status: PASS_DIAGNOSTIC** (real missing codes captured on ONE isolated scratch startup; original refusal retained;
scratch fully cleaned; shared parity unchanged; one disclosed deviation in the shared Host process — see below).

## The fact that was missing since 103/104/105

```
stage=catalog-dev.attribute-schema.publish-product
product=01a1421c-5ca6-7000-b43d-9797d12fc02d
isReady=False
missing=[variants]
```

Emitted by the 105 seam on a real isolated Development startup over an empty scratch database, with the original
refusal (`System.InvalidOperationException: محصول برای انتشار آماده نیست.`) unchanged beneath it at
`CatalogDirectory.UnwrapLifecycle:849`.

## The single failing sub-rule

`ProductVariantDirectory.GetReadinessAsync`: `isValid = missingAxes == 0 && invalidVariants == 0 && duplicates == 0
&& noDefault != true`. Scratch data (read-only SQL) shows correct axes (color+storage), correct per-variant axis
values with GUID options, distinct fingerprints — and `is_default = false` on **both** variants, so `noDefault = true`.
The seed's create path (`CreateVariantAsync` → `CreateWorkspaceVariantAsync`) never designates a default variant,
while publish readiness requires one. That is why this is latent on the shared database and fatal on an empty one.

## Bounds respected

`LAUNCH_COUNT=1` · watchdog `240 s` · elapsed `50 s` · no retry · no `src/**` change · no demo reset/seed ·
`RunLegacyBootstraps=true` with `AllowResetAndSeed=false` · no shipping gate enabled · isolation guard passed
(1 tenant, scratch-only refs, no `tooba_alpha`, no `Port=5432`) · scratch container/volumes/dirs destroyed ·
0 stray listeners · shared `:5088` provenance and `tooba_alpha` counts identical before/after.

## Disclosed deviation

During teardown a name-based `Get-Process -Name Tooba.Host | Stop-Process` also matched the pre-existing shared
Development Host on `:5088`. The mandatory post-task health check caught it, and the Host was restarted from the same
exe, working directory, port and safe defaults; it is healthy now with byte-identical provenance and unchanged counts.
Only the PID changed. Details in `isolation-watchdog-and-parity.txt`.

## Recommendation

One bounded Catalog-owned seed correction designating exactly one created variant as the default through the existing
public contract that already enforces a single default — with the Architect ruling whether the defect is the seed or
the create-path contract. No readiness rule may be relaxed, and no sellable-offer/storefront/checkout claim is made here.
