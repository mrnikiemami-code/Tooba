# Flat growth guard — TB-TMAR-FE-F1

## FE-FOLDER-001 / FE-FOLDER-002

Baseline: `frontend-flat-folder-baseline.json`

- Flat admin dir: `src/frontend/app/admin` (direct files)
- Business flat files baselined: 169 (post languages migration)
- Route convention at admin root: `page.tsx`, `layout.tsx`
- Dumping ground: `admin-api.ts` — export name freeze (59 names)

Guards: `lib/architecture/frontend-flat-folder.guard.test.ts`

Locks recorded in `docs/architecture/TMAR-architecture-locks.md`.
