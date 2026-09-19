# Localization Audit

## Page language

- `StoreLandingPage.Locale` is canonical page language (`fa` / `en` normalized via `StoreLandingPageSlug.NormalizeLocale`).
- Unique `(Locale, Slug)` per store DB.
- **Invariant for Amazing source:** inherit Page locale; do **not** add section-level language independent of Page.

## Product / catalog copy

- Primary i18n pattern: `CatalogLocalizedField` with `(OwnerKind, OwnerId, FieldKey, Locale)`.
- Category: `CatalogCategoryTranslation` (locale + slug).
- Mega-menu translations similarly.
- Products expose `SlugSeam` on entity; localized titles via localized fields pattern (not a separate `product_translations` table name).

## Campaign-facing text (future)

- Existing `promotion.promotions.Name` is **operational**, explicitly “not marketing content”.
- No promotion translation table today.
- Recommendation: if Amazing campaign shows user-facing title/subtitle/badge, add `campaign_translations` (or reuse `CatalogLocalizedField` OwnerKind=PromotionCampaign) — **do not** hardcode Persian; resolve by Page locale with fallback to store default language.

## RTL / LTR

- Frontend admin/storefront already RTL for `fa`; locale drives dir. Campaign presentation should follow Page locale, not invent section dir.

## Section locale

- `StoreLandingPageSection` does not own locale; tied to parent page. Keep this for PromotionCampaign source config.
