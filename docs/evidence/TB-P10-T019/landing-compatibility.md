# Landing compatibility — TB-P10-T019

- LANDING_SECTION_TYPE_MAP preserved for Hero…NavigationMenu.
- config.variantKey overrides shared SectionType/Variant when implemented.
- BannerShowcase/StoryRail/ProductRankedList store via Host PromoBanner/CategoryGrid/ProductCollection with variantKey.
- Draft preview + published routes still use StorefrontLandingSections → adaptLandingSectionToComposition → renderSharedLandingSection.
- No forced migration; legacy configs without variantKey keep default mapped variants.
