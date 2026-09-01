# TB-P08-T001-R2 — Admin UI

- Edit modal shows Code + UrlPrefix with `readOnly` when `canEditCode` / `canEditUrlPrefix` false.
- fa/en lock explanations via `language-identity-lock.ts`.
- Save uses PUT `/v1/admin/languages/{code}`.
