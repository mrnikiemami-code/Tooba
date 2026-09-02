# TB-P08-T010 visual gate notes

- Architecture preserved: AppDataGrid on article/author/language lists; AppCategoryTree on content categories; no Article-list redesign; no routing/domain change.
- Localized copy: article loading (no English “workspace”); media picker “کتابخانه”; gallery Alt/Caption Persian; language banner Persian; public blog RTL/LTR chevrons.
- Author list edit href uses `?mode=edit`.
- Article list grid errors mapped via `mapAdminErrorMessage`.
- USER_VISUAL_ACCEPTED remains NO (human-only).
- Browser MCP unavailable this session; HTTP smoke of admin shells 200; public `/fa/blogs` 308 rewrite/redirect loop via curl; public content API 500 PostgresException.
- Runtimes left running: Host `:5088`, FE `:3000`. Shopeiva `:3001` down. Postgres `:5432` occupied by existing Docker/WSL listener.
