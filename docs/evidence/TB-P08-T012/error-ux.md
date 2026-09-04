# TB-P08-T012 — Error UX

## Mapped codes (touched)

| Code | FA intent |
|------|-----------|
| localization.language.inactive | زبان انتخاب‌شده فعال نیست |
| content.category.language_mismatch | دسته با زبان مقاله هم‌خوان نیست |
| content.article.media_not_found | رسانه در دسترس نیست |
| content.article.media.rejected | اختصاص رسانه ناموفق |
| content.update.rejected | ذخیره انجام نشد (fallback) |
| content.author.inactive | نویسنده غیرفعال (existing) |

## Forbidden in UI

Bad Request / raw `content.update.rejected` / JSON / HTTP titles — blocked by `mapAdminErrorMessage` + `isTechnicalAdminErrorText`.

## Paths

General/Content/Category/Author/Media/SEO mutations toast via `mapAdminErrorMessage` / `mapArticleMediaMutationError`.
