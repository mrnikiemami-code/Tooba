# Storefront Appearance Extension

Status: CONTRACT ONLY — TB-P10-T005 foundation. Custom theme editor is not implemented.

## Current release

- One canonical owner: `StoreAppearanceSettings` in Catalog (same Store-settings family as checkout identity / hold / abuse).
- Persisted fields: `PaletteKey`, `ThemeMode`, `UpdatedAt`.
- Registry: curated keys only. Default `tooba-blue` = `#2563EB` / `#1d4ed8`.
- Unknown `PaletteKey` falls back to `tooba-blue`.
- Runtime: Host projects tokens → memory cache keyed by Tenant/connection → SSR root sets CSS variables.
- Brand tokens never write `--color-danger`, `--color-success`, or `--color-warning`.

`Tenant.ThemeReference` is not the Storefront appearance owner and must not execute UI.

## Future custom theme (not built)

A future custom theme MUST:

- persist validated declarative tokens only (no HTML/CSS/JS text execution);
- enforce contrast/readability before publish;
- generate/resolve shades from brand seeds without mutating status colors;
- provide light and dark token sets;
- remain SSR- and cache-compatible (`StoreScope` cache key + invalidation on write);
- require no per-store rebuild, publish, or app-pool restart;
- reject executable HTML/CSS/JS and runtime Tailwind class construction from DB values.

Product Card skins remain a later presentation switch on the same canonical component.
