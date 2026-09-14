# Storefront section-context composition

Normative rule: the four global surface roles remain the only user-configurable colors. Below that, every customer-facing region is a **section context**. Ordinary children inherit. Independent objects opt into a local derived surface.

## Contexts

| Context | Token | Establishes |
| --- | --- | --- |
| PageBackground | `--color-page-background` | Outer canvas only |
| SectionSurface | `--color-section-surface` | Default content band + local card/elevated/input/interactive/media/border |
| SectionAlternate | `--color-section-alternate` | Alternate band + its local family |
| SectionAccent | `--color-section-accent` | Promo/highlight band + its local family |

`deriveLocalSurfacesFromContext` in `src/frontend/lib/storefront-appearance/derived-surface.ts` produces local surfaces from the **active** context. Neutral keeps accepted paper cards. PaletteTint local cards are mixed from the parent section so they cannot collapse to pure white islands.

CSS binds `--color-surface` / elevated / border / secondary / background on `[data-storefront-surface-role="section|alternate|accent"]` when `html[data-storefront-background-style="PaletteTint"]`. Admin/seller shells stay Neutral paper.

## Inherit-by-default

`inherit` (`bg-transparent`) is the default structural child. Padding, radius, or border does not imply Card.

`StorefrontPanelSurface` defaults to `inherit`. Card/Elevated/Input/Interactive/Media/Overlay are explicit.

Giant CardSurface as a page/section substitute is forbidden.

## Composition map

```
PageBackground
  -> Section context
       -> structural/content child = inherit
       -> true card = local-card
       -> elevated = local-elevated
       -> input/form = local-input
       -> interactive group = local-interactive
       -> media well = local-media
       -> status = status semantic
```

PDP main area is SectionSurface. Gallery is media. Product info and buy column inherit. Tabs/details are SectionAlternate with inherit body. Related products are SectionAlternate; ProductCard is local-card.

Cart outer is SectionSurface. Hero is SectionAccent. Line list inherit; each line may be local-card. Summary is local-elevated. Benefits band is SectionAlternate with inherit items.

Customer panel canvas is PageBackground. Main is SectionSurface. Page titles inherit. Order/address/summary objects are local-card/elevated. No giant white main wrapper.
