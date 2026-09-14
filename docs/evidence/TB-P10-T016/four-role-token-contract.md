# Four-role token contract — TB-P10-T016

T015 tint evolved, not duplicated.

| Role | CSS | Neutral light | Neutral dark | PaletteTint |
| --- | --- | --- | --- | --- |
| PageBackground | `--color-page-background` | 243 245 248 | 12 12 14 | `pageBackgroundRgb` / dark |
| SectionSurface | `--color-section-surface` (`--color-section-background` alias) | 255 255 255 | 28 28 32 | `sectionBackgroundRgb` / dark |
| SectionAlternate | `--color-section-alternate` | 247 248 250 | 22 22 26 | `sectionAlternateRgb` / dark |
| SectionAccent | `--color-section-accent` | 238 242 247 | 34 38 48 | `sectionAccentRgb` / dark |

All 7 palettes have distinct light/dark quartets. Values are not primary and not status. Public `/v1/storefront/appearance` exposes the four roles; T015 names remain aliases.
