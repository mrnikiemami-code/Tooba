# R18 performance

- Store: reservation preview rides the existing hold-policy GET or one `/store` GET.
- Category: one GET per workspace tab.
- Offer batch: three catalog/offer lookups + one override query via `PreviewManyAsync`. No per-row HTTP from the product grid.
- No supply/reservation runtime queries for settings.
- No polling (`setInterval` absent from editor/API).
