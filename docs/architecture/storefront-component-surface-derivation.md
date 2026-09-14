# Storefront component surface derivation

Normative rule: the user configures only the four global surface roles. Every other customer-facing surface is derived automatically.

## User-configurable (Store Appearance)

- PageBackground
- SectionSurface
- SectionAlternate
- SectionAccent

No additional color settings, DB columns, or Admin controls exist for component surfaces.

## System-derived

`deriveComponentSurfaces(palette tint, BackgroundStyle, dark)` in `src/frontend/lib/storefront-appearance/derived-surface.ts` produces:

| Surface | Neutral light | PaletteTint light |
| --- | --- | --- |
| Card | `255 255 255` | section tint (not pure white) |
| Elevated | white | mix(section, white) |
| Input | white | mix(section, white) |
| Interactive | `244 244 245` | mix(accent, section) |
| Media | `249 250 251` | mix(page, alternate) |
| Overlay | white | mix(section, white) |
| Border | `228 228 231` | mix(alternate, neutral border) |

Dark relationships use the curated dark tint family. Status (danger/success/warning) and Primary remain existing semantic tokens.

CSS variables `--color-*-derived` are injected with brand/tint vars. They bind to `--color-surface` / `--color-surface-elevated` / `--color-border` / `--color-secondary` / `--color-background` **only** on `[data-storefront-canvas]` and `[data-customer-panel-canvas]` when `data-storefront-background-style="PaletteTint"`. Admin and seller shells are unchanged.

Canvas-scoped `.bg-white` / `.bg-gray-50` / `.bg-gray-100` remap onto those derived tokens so leftover structural classes inherit the same system.

## Primitives

`StorefrontSurface` accepts `surface=card|elevated|input|interactive|media|overlay|page|section|alternate|accent|header|footer` only. No raw color prop.
