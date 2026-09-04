# TB-P08-T016 — Admin walkthrough

## Code / contract (worker)

- List: `content-list.tsx` — language tabs, AppDataGrid, create → `/admin/content/articles/new?language=`
- Workspace: `content-article-admin-screen.tsx` — VIEW/EDIT, tabs including comments/history
- Create: `content-article-new-screen.tsx` — Draft-first, dynamic languages, no author gate
- FE source asserts green for list/create/workspace contracts
- FE fallback: `?-only` / corrupted `nativeName` treated as missing label (list + admin + new screens)

## Browser (parent)

- [x] `/admin/content` loads; language tabs from DB; default language selected  
  Observed: fa/en language tabs; AppDataGrid.
- [x] Switch tab → list filters; no redundant Language column  
  Observed: switch EN list works; no Language column.
- [ ] Empty state polished; no raw IDs / machine errors  
  Not exercised this session.
- [x] Create inherits active tab language  
  Observed: draft-first create via `/admin/content/articles/new?language=fa-IR` → article `01a06ac3-10b7-7000-bb13-f446f1df7962`.
- [ ] fa + en Article workspace open in EDIT  
  FA EDIT workspace verified (readiness badge, Preview/Publish/Save hierarchy, tabs). EN article created via API smoke (`01a06ac1-135f-7000-b45f-7089ee3d1add`); EN EDIT browser open not separately logged.
- [x] Header hierarchy: title/status/language/readiness/save; Preview/Publish ordered; destructive separated  
  Observed in FA EDIT: readiness badge; Preview/Publish/Save hierarchy; tabs.

## Gate notes

- `Language.nativeName` for `fa-IR` was corrupted as `?????` in DB; fixed via `PUT /v1/admin/languages/fa-IR` `nativeName=فارسی`; FE fallback added for `?-only` nativeName.
- Title briefly became literal `"undefined"` (automation fill artifact); corrected via API to `"T016 Browser Gate FA"`.
