# TB-P10-T005-R1 — Validation

| Check | Result |
| --- | --- |
| recovery-staleness | 4/4 after SoT correction |
| palette-registry + appearance API | 6/6 (scope marker added) |
| `npm run test:storefront` | 67/67 |
| `npm run test:critical-storefront` | 16/16 |
| Host `StoreAppearanceFoundationTests` | 4/4 (isolated `-o .tmp-t005r1-test-out`) |
| `npm run typecheck` | not PASS — 107 pre-existing Admin/grid/`maxWidth`/wallet/i18n errors |
| T005/R1 files in tsc errors | none (`storefront-appearance`, `palette-registry`, `layout.tsx` absent) |
| `git diff --check` | clean on task-owned sources |

Full typecheck is not reported PASS. Unrelated Admin/grid code was not changed.
