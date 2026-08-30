# Full validation R1 (TB-P07-T036-R1)

## Backend
- Full Host tests (in-place): **359 passed / 0 failed / 0 skipped** (`backend-full-tests-r1.log`, ~24m)

## Frontend
- Relevant Catalog/Product/Category tests: **60 passed / 0 failed / 0 skipped** (`frontend-relevant-r1.log`)
- Recovery staleness guard: **2 passed** (`recovery-guard-r1.log`)
- `tsc --noEmit`: exit 0 (`frontend-typecheck-r1.log`)
- `next lint`: exit 0; pre-existing unused-var warnings only (`frontend-lint-r1.log`)
- Production build: Compiled successfully; BUILD_ID recorded (`frontend-production-build-r1.log`)

## Live
- `live-r1-proof.mjs`: **OVERALL PASS** — A 10, B 8, C 9, D 7 (`live-r1-proof.json`)

## Git
- Scoped commit required; HEAD must equal origin/main after push.
- `git diff --check` on commit tree (unrelated dirty evidence logs may still warn outside scope).

## Notes
- USER_VISUAL_ACCEPTED=NO
- No Product/Variant Price/Stock introduced
