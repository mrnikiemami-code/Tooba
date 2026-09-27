# Readiness parity — W16

Preserved exactly:

1. Locale: `ProductSeoRules.NormalizeLocale` (null/blank → `fa-IR`)
2. `categoryReady` = primary category assignable (level-3 tree rule)
3. `translationReady` = non-blank localized product name from SEO detail
4. `attributeReady` / `variantReady` / `mediaReady` / `seoReady` from focused seams
5. Missing order: category → identity → attributes → variants → media → seo
6. Attribute message suffix with MissingRequiredCodes joined by `، `
7. Media/SEO `MessageFa ?? ProductPublishRules.Message*IncompleteFa`
8. `isReady = missing.Count == 0`
9. `messageFa` = MessageReadyFa when ready else SummarizeMissingFa(count)
10. No Offer / price / stock checks
11. ProductPublishRules Domain constants unchanged
