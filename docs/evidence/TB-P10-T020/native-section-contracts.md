# Native section contracts — TB-P10-T020

- `SECTION_TO_LANDING_HOST.StoryRail = "StoryRail"` (no CategoryGrid proxy)
- `SECTION_TO_LANDING_HOST.BannerShowcase = "BannerShowcase"` (no PromoBanner proxy)
- Host `StoreLandingPageSectionRegistry` ApprovedTypes includes StoryRail + BannerShowcase
- Host normalizers: `NormalizeStoryRail`, `NormalizeBannerShowcase`; `variantKey`/`heightPreset` preserved on Hero/Promo/ProductCollection/CategoryGrid/BrandStrip/ArticleList/Reviews/RichText/NavigationMenu
- FE adapter: native LANDING maps + `migrateProxySectionType` for T019 CategoryGrid/PromoBanner configs carrying story.*/banner.* variantKey
