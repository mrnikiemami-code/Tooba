# TB-P08-T016 — Admin walkthrough

## Code / contract (worker)

- List: `content-list.tsx` — language tabs, AppDataGrid, create → `/admin/content/articles/new?language=`
- Workspace: `content-article-admin-screen.tsx` — VIEW/EDIT, tabs including comments/history
- Create: `content-article-new-screen.tsx` — Draft-first, dynamic languages, no author gate
- FE source asserts green for list/create/workspace contracts

## Browser (parent)

- [ ] `/admin/content` loads; language tabs from DB; default language selected
- [ ] Switch tab → list filters; no redundant Language column
- [ ] Empty state polished; no raw IDs / machine errors
- [ ] Create inherits active tab language
- [ ] fa + en Article workspace open in EDIT
- [ ] Header hierarchy: title/status/language/readiness/save; Preview/Publish ordered; destructive separated
