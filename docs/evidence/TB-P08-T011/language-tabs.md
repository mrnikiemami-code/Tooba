# language-tabs.md

Article Admin list (`content-list.tsx`) loads active languages via `loadAdminLanguages`, filters `active`, sorts by `sortOrder`, labels with `nativeName`/`displayName`.

- URL sync: `?language=<code>`; invalid codes fall back to default active language.
- Grid query injects `{ kind: "text", operator: "equals", query: selectedCode }` on `locale`.
- Tab change bumps `reloadToken` and remounts AppDataGrid via `key`.
- Locale column and advanced locale filter removed; create link includes `?language=`.
- Empty language yields AppDataGrid’s normal empty state (zero rows).
