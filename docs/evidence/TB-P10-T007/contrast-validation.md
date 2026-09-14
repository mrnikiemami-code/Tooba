# TB-P10-T007 — Contrast validation

WCAG relative-luminance helpers in `palette-registry.ts`. AA 4.5:1 required for primary vs on-primary and primary vs paper.

amber-gold failed at `217 119 6` (CTA 3.19). Repaired to `180 83 9` / `146 64 14` (CTA 5.02, link 4.81, strong 7.09).

All 7 palettes pass `every curated palette meets AA contrast`.
