# Storefront global surface theme

Normative rule: changing a few global surface colors must coherently affect the whole storefront and every customer-facing page.

## Coverage rule

Every public Storefront route and every customer-panel route inherits the canonical Store Appearance projection from the root layout (`loadStorefrontAppearance` once). Customer panel is not a separate theme system and must not fetch appearance per route.

Every major page/section background maps to a canonical semantic surface role or an explicitly allowed Card / Input / Header / Footer / Overlay / Media / Status role. Large hardcoded white/gray wrappers on page or section chrome are forbidden.

New Storefront or customer pages must:

1. choose semantic surface roles through shared wrappers/primitives
2. be added to `src/frontend/lib/storefront-appearance/storefront-route-inventory.ts`
3. pass the coverage guards and the reusable crawler in `src/frontend/scripts/storefront-theme-coverage.mjs`

## Four global roles

| Role | Token | Meaning |
| --- | --- | --- |
| زمینه صفحه / PageBackground | `--color-page-background` | Outer canvas |
| بخش اصلی / SectionSurface | `--color-section-surface` | Default content band |
| بخش جایگزین / SectionAlternate | `--color-section-alternate` | Alternate band |
| بخش برجسته / SectionAccent | `--color-section-accent` | Promo/highlight band |

CardSurface, ElevatedSurface, Input, Interactive, Media, Header, Footer, Overlay, and Border are **system-derived** from PaletteKey + ThemeMode + BackgroundStyle + the four global roles. They are not independent Store settings.

See `docs/architecture/storefront-component-surface-derivation.md`.

## Shared wrappers

- `StorefrontShell` is the Storefront page canvas (`data-storefront-surface-role="page"`, `bg-page`). Home/Landing may set `fullBleed` so section bands span the canvas.
- `StorefrontPageSurface` / `StorefrontSectionSurface` / `StorefrontPanelSurface` in `src/frontend/app/storefront/storefront-surface.tsx` are the typed primitives. They accept a constrained `surface` role only — no raw color parameter.
- Header/Footer use HeaderSurface/FooterSurface (`bg-surface`).
- `CustomerPanelShell` maps outer canvas → PageBackground, header/sidebar → Header/Elevated, main → SectionSurface. Inner panels use CardSurface (`bg-surface`).
- Blog/content routes use `src/frontend/app/blogs/layout.tsx` so they inherit the same Storefront shell.

Landing SectionTypes map through `landingSectionSurfaceRole` in `src/frontend/lib/storefront-appearance/surface-role.ts`.

## Route inventory

`STOREFRONT_ROUTE_INVENTORY` is the source of truth for crawl coverage (groups A–G). Dynamic patterns require at least one real seeded sample. Customer navigation destinations require authenticated crawl coverage.

## BackgroundStyle

- Neutral: accepted white/gray hierarchy. No surprise wash.
- PaletteTint: curated family values for the four roles, light and dark, for all 7 palettes.

A future custom theme may override only these global roles (plus already-canonical brand/text/border tokens). No page-by-page or Home/PDP/Blog-specific color settings. No custom color database fields in this release.
