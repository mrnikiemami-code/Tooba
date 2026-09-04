# article-language-policy.md

Domain `ContentArticle.CanChangeLocale()`: Draft only; locked when any of AuthorId, CategoryId, CoverMediaAssetId, SeoImageMediaAssetId, Body, SeoTitle, SeoDescription, TagsCsv, Category label, or IsFeatured is set.

- `Validate` allows empty `authorDisplayName` (length ceiling only).
- `AssignAuthor` still requires non-empty display name.
- `EnsureArticleAuthorAssignmentAsync` returns when `authorId` is null.
- `ContentDirectory.UpdateAsync` uses `CanChangeLocale()` and also rejects locale change when `ArticleMedia` rows exist.
- FE `isArticleLocaleLocked` mirrors the pristine-draft policy (incl. Published/Archived).
