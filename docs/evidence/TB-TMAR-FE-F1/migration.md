# Migration — TB-TMAR-FE-F1

Moved `language-api.ts` + `language-list.tsx` from flat `app/admin/` into `features/admin-languages/`.

- Route `/admin/languages` unchanged; page imports public boundary
- Consumers of `loadAdminLanguages` updated to `features/admin-languages`
- Shared `AdminResult` / actor header extracted to `lib/admin/admin-result.ts`; `admin-api.ts` re-exports (LOC 1326→1321)
- No duplicate implementation; no temporary shim left in flat admin
- Styling/classes/locale copy preserved
