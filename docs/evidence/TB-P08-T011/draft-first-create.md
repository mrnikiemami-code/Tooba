# draft-first-create.md

`content-article-new-screen.tsx` creates drafts without requiring an author:

- Languages from Admin API (no `LANGUAGE_OPTIONS` hardcode).
- Prefill from `?language=` or default active language.
- `createAdminArticle` with `authorId: null`, `authorDisplayName: ""`, empty body.
- Removed “نویسندهٔ فعالی یافت نشد” gate and author preload.
- Errors via `mapAdminErrorMessage` / `normalizeAdminClientError`.
- On success, navigates to `/admin/content/articles/{id}`.
