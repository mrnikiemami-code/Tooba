# TB-P08-T001-R1 — Content Language Reference

- `ContentDirectory` injects `ILanguageDirectory`.
- `CreateAsync` / `UpdateAsync` call `EnsureActiveLanguageCodeAsync` before persisting `ContentArticle.Locale`.
- Scalar `Locale` string preserved; no `Article.Translations[]` introduced.
- `ContentLanguageReferenceGuard` implements `ILanguageReferenceGuard` against `content.articles`.
