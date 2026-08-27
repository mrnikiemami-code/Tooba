# 08 — Localized data binding (TB-P06-T014)

| Domain | Binding | Status |
|---|---|---|
| Blog/Article | Host `locale` field; FE `loadPublishedArticleBySlug(slug, locale?)` | LIVE |
| Product/Category localized texts | Catalog localized_texts (prior) | LIVE where seeded |
| SEO metadata | Page `generateMetadata` + OG locale from cookie | LIVE foundation |
| Static chrome | `lib/i18n/messages.ts` + DS workspace/grid catalogs | PARTIAL |
| Parallel translation DB | Not created | Correct — reuse Host + DS |

No duplicate translation storage invented.
