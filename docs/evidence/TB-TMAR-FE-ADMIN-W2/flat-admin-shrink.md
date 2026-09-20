# Flat admin shrink — TB-TMAR-FE-ADMIN-W2

| File | Before | After | Class |
| --- | --- | --- | --- |
| `admin-screens.tsx` | 1120 | 1078 | OVERSIZED_LEGACY (unchanged band) |
| `admin-api.ts` | 1234 | 1145 | CRITICAL_GOD_FILE (shrink-only) |

Only reviews-owned screen/columns/API removed. No unrelated capability decomposition.
Source-size baselines updated shrink-only (FE-BASELINE + Host TmarSourceSize).
FE-FOLDER-002 export baseline shrink-only 55→49.
