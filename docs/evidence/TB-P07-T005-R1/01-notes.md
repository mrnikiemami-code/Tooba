# TB-P07-T005-R1 notes

## Scope
Category workspace VIEW/EDIT modes + clean human slug UX. AppCategoryTree untouched.

## Changes
- `useAdminFormMode` design-system primitive + `docs/ui/ADMIN-FORM-MODE.md`
- Category admin General: default VIEW, ویرایش → EDIT, Save/Cancel, dirty confirm
- APIs: `updateCategoryCore`, `upsertCategoryTranslation`, `archiveCategory`, slug conflict mapping
- Host: `catalog.category.slug.duplicate` → Persian title (409)
- Slug preview `/fa/category/{slug}` — no CategoryId suffix
- Tree label soft-refresh after name save; expanded/selected preserved

## USER_VISUAL_ACCEPTED
NO (manual review still required)
