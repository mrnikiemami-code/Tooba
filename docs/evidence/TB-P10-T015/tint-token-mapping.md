# Tint token mapping — TB-P10-T015

Registry (C# + FE) adds curated `StorefrontTintTokens` per palette:

- pageBackgroundRgb / sectionBackgroundRgb (light)
- pageBackgroundDarkRgb / sectionBackgroundDarkRgb (dark)

Neutral keeps `--color-page-background: 243 245 248` (`#f3f5f8`).

PaletteTint maps those vars from `--color-page-tint` / `--color-section-tint` (and dark pair) via `html[data-storefront-background-style=PaletteTint]`.

Tint values are paper-family, not primary RGB and not status colors.

Examples (page light / page dark):

- tooba-blue: 236 241 250 / 14 17 26
- forest-green: 236 244 238 / 13 18 15
