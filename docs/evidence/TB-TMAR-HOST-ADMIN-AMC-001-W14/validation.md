# Validation — W14 ProductSeo

| Request | Classification | Notes |
|---|---|---|
| GetProductSeoQuery | NO_VALIDATOR_REQUIRED | route Guid + optional locale |
| GetProductSeoReadinessQuery | NO_VALIDATOR_REQUIRED | route Guid + optional locale |
| UpdateProductSeoCommand | VALIDATOR_REQUIRED | Locale NotNull transport (`catalog.validation.product_seo_locale_required`) |

Not duplicated in FluentValidation:

- slug normalization / uniqueness
- product existence
- ExpectedUpdatedAt concurrency comparison
- SEO readiness evaluation
- ProductSeoRules authority
