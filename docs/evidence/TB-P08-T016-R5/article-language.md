# Article language (TB-P08-T016-R5)

## Back navigation
- Article workspace back link: `/admin/content?language=${encodeURIComponent(draftLocale||article.locale)}` when locale known (`data-testid=content-article-back-link`).
- New draft screen back link: same pattern from selected `locale`.
- Delete redirect preserves list language.
- Content list view/edit row actions optionally append current list language query.

## Category picker locale
- `ContentArticleCategoryPicker` derives `direction`/`uiLocale` from `languageCode` (en→ltr/en, fa→rtl/fa); no hardcoded `rtl`/`fa`.
- Workspace passes `languageCode={draftLocale || article.locale}` and `loading={categoriesLoading}`.

## Loading
- Initial workspace load shows Spinner + «در حال بارگذاری…».
- Category tree fetch tracks `categoriesLoading` and surfaces Spinner on Categories tab.
