# TB-P08-T004 — Article Editor Workspace

## Delivered

- Preserved AppDataGrid article list; added Edit action, Language and Author columns
- Language-first create at `/admin/content/articles/new` → workspace `/admin/content/articles/[articleId]`
- Workspace tabs: عمومی، محتوا، دسته‌بندی‌ها، نویسنده، رسانه، SEO، انتشار، تاریخچه
- TipTap body editor (ProductRichTextEditor) with RTL/LTR from article locale
- Same-language category picker, active author picker, DAM featured image
- Backend locale mutation guard (`content.article.locale_locked`) after publish/references
- PublishDate scheduling on update; Jalali/Gregorian display per locale

## Validation

- `dotnet build` PASS
- `ContentArticleEditorTests` PASS (with author/category tests)
- FE contract tests 8/8 (content-api, list, article workspace)
- recovery guard 3/3
