# TB-P08-T012 — SEO image root cause

## Root cause

Same missing `Content-Type` on media PUT as Featured (see media-root-cause.md). FE also had no error toast on SEO assign/fallback, so failures looked like “save failed” or no-ops.

## UX repair

- Wording: «تصویر اشتراک‌گذاری و شبکه‌های اجتماعی»
- Option: «استفاده از تصویر شاخص مقاله»
- Else: separate library pick
- Effective preview via `effectiveSeoImageMediaAssetId` / featured fallback
- Removed ambiguous «مؤثر: تصویر شاخص/SEO»
- Errors mapped through `mapArticleMediaMutationError`
