# Recovery Start — TB-P10-T022-R9

Recorded at claim/start of Store Pages Foundation task.

## Git

- branch: main
- HEAD: 4485e3e9aa18140a7d77c0325abc079eb833a450
- origin/main: 4485e3e9aa18140a7d77c0325abc079eb833a450 (HEAD == origin/main)
- Expected claim HEAD matches.
- git status: clean tracked tree; many unrelated local `.tmp-*` untracked artifacts (not committed)

## Preflight audit (current state)

| Area | Finding |
|------|---------|
| Admin menu | `landingPages` label «صفحات فرود» in admin-chrome-messages; route `/admin/landing-pages` |
| Page entity | `StoreLandingPage` — Locale, Slug, Title, SeoTitle, SeoDescription, Status; **no PageType** |
| Home | `StoreAppearanceSettings.HomePageId` singleton pointer; SetHome/clear exists |
| Landing route | FE `app/[slug]/page.tsx` → `/{slug}` — **not** `/landing/{slug}` |
| SEO | Title/description only; no robots/OG/canonical override/H1 policy fields |
| Sitemap | Does not include Landing pages |
| Grid | AppDataGrid orders-canonical; missing page type + indexability columns |
| Locks | LOCK-SF-001…315 present; 316–324 not yet registered |

## Recovery decision

- Expected previous HEAD matches claim. No RECOVERY_CONFLICT.
- Unrelated `.tmp-*` preserved; not committed.
- Scope: Store Pages foundation only; do **not** invent TB-P10-T023.
