# TB-P08-T001 — Content / i18n Discovery

## Content module

| Area | Path | Classification |
|---|---|---|
| ContentArticle entity + scalar Locale | `src/backend/Modules/Content/Tooba.Content.Domain/ContentArticle.cs` | EXISTS_AND_REUSE |
| Publish/Unpublish per article | same + `ContentDirectory.cs` | EXISTS_AND_REUSE |
| Admin content grid | `src/frontend/app/admin/content-list.tsx` | EXISTS_AND_REUSE |
| Article translations table | — | GENUINELY_MISSING (by design: one article = one language) |
| Slug uniqueness | slug-only index | EXISTS_BUT_NEEDS_EXTENSION (future `(slug, locale)` if needed) |

## i18n / locale foundation

| Area | Path | Classification |
|---|---|---|
| Storefront locale routing | `src/frontend/lib/i18n/routing.ts`, `middleware.ts` | EXISTS_AND_REUSE |
| Locale constants fa/en | `src/frontend/lib/i18n/locale.ts` | EXISTS_AND_REUSE |
| Jalali admin formatting | `src/frontend/design-system/app-data-grid/jalali.ts` | EXISTS_AND_REUSE |
| Public blog date formatting | `src/frontend/app/content/content-api.ts` | EXISTS_BUT_NEEDS_EXTENSION |
| Persisted language registry | `SupportedLocaleRegistry` (Host) | GENUINELY_MISSING → implemented in T001 |
| Language admin UI | `src/frontend/app/admin/language-list.tsx` | GENUINELY_MISSING → implemented in T001 |

## Article language semantics (locked)

- Each `ContentArticle` row has one `Locale` identity (`fa-IR`, `en-US`, …).
- `Publish()` / `Unpublish()` affect only that article row.
- No mandatory `Article.Translations[]` for publishing.
- Persian and English articles may be unrelated topics with independent publish state.

## Reuse decisions

- Reused existing `lib/i18n` routing/locale helpers; extended with `supported-locales.ts` types.
- Did **not** duplicate Catalog translation subsystem for Content.
- Did **not** add DB migration for languages in T001 — minimal in-memory Host registry with Admin PATCH overlay.
