# 11 — Home composition runtime (TB-P06-T015)

## Flow

```text
page.tsx
  → loadHomeComposition()  // GET /v1/storefront/home/composition
  → StorefrontShopeivaHome({ compositionSections, ...liveData })
  → sort by displayOrder
  → renderHomeSection(sectionType) switch
  → existing Shopeiva-locked section components
```

## Fallback

If composition missing/empty → `defaultHomeCompositionSections()` / `DEFAULT_HOME_SECTION_ORDER` (same 11 types).

## Visual contract

- No new home CSS redesign.
- Existing renderers reused (`HomeHeroSlider`, `ProductRailSection`, brands, articles, …).
- Critical storefront guards assert `renderHomeSection` presence.
