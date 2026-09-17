# slider-variant-map — TB-P10-T022-R13-R1

| Persian UI | Persisted key | Layout id |
|------------|---------------|-----------|
| الماس | `hero.fullscreen` | `fullscreen` |
| سیمین | `hero.shapes` | `shapes` |
| کیمیا | `hero.diagonal` | `diagonal` |
| فاخته | `hero.cinematic` | `cinematic` |
| صبا | `hero.split` | `split` |
| عقیق | `hero.editorial` | `editorial` |

## Rules applied

- Admin picker shows Persian design names only (`VARIANT_DESIGN_NAMES` / registry `nameFa`).
- Persist English variant key only.
- Legacy aliases map: `hero.full-width`→fullscreen, `hero.contained`→shapes, `hero.side-promos`→diagonal.
- `implementedVariantsForSection("HeroCarousel")` returns exactly these 6.
- Non-slider / obsolete Slider choices are not exposed for new edits.

Source: `hero-slider-config.ts`, `registry.ts`, `variant-design-names.ts`.
