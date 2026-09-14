# Storefront global surface theme

Normative rule: changing a few global surface colors must coherently affect the whole storefront.

## Four global roles

| Role | Token | Meaning |
| --- | --- | --- |
| زمینه صفحه / PageBackground | `--color-page-background` | Outer canvas |
| بخش اصلی / SectionSurface | `--color-section-surface` | Default content band |
| بخش جایگزین / SectionAlternate | `--color-section-alternate` | Alternate band |
| بخش برجسته / SectionAccent | `--color-section-accent` | Promo/highlight band |

CardSurface, ElevatedSurface, Input, HeaderSurface, FooterSurface, and status colors stay independent.

## BackgroundStyle

- Neutral: accepted white/gray hierarchy. No surprise wash.
- PaletteTint: curated family values for the four roles, light and dark, for all 7 palettes.

A future custom theme may override only these global roles (plus already-canonical brand/text/border tokens). No page-by-page or Home/PDP/Blog-specific color settings. No custom color database fields in this release.

Landing SectionTypes map through `landingSectionSurfaceRole` in `src/frontend/lib/storefront-appearance/surface-role.ts`.
