# TB-P08-T001-R2 — Reference Detection

- `ILanguageReferenceGuard` (existing) — `ContentLanguageReferenceGuard` checks `content.articles.locale`.
- `LanguageDirectory.ListAdminAsync` / `GetAdminByCodeAsync` query guard per language (no cross-module SQL JOIN).
- Minimum rule: ContentArticle reference locks Code + UrlPrefix.
