# Feature boundary — TB-TMAR-FE-F1

Public: `src/frontend/features/admin-languages/index.ts`

Exports: `AdminLanguagesScreen`, `loadAdminLanguages`, `updateAdminLanguage`, `patchAdminLanguage`

Guard: `frontend-feature-boundary.guard.test.ts` forbids external deep imports into `api/` or `components/`.

`language-list` still uses `app/admin/saved-view-store.ts` (shared admin grid helper — temporary cross-path; not deep feature internals).
