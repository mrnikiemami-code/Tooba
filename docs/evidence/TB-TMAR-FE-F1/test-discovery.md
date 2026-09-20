# Test discovery — TB-TMAR-FE-F1

## Before

`package.json` manually enumerated test files across many `test:*` scripts; `npm test` chained those scripts. Silent omission risk confirmed.

## After

Canonical discovery: `scripts/run-discovered-tests.mjs` walks `src/frontend` for `*.test.*` / `*.guard.test.*` (excludes `node_modules`/`.next`/build).

- `npm test` → `npm run test:discovered`
- Focused `test:*` scripts retained for local slices
- Discovery count at FE-F1: **174** files
- Sanity guard: `frontend-test-discovery.guard.test.ts` asserts discovery list == on-disk set

No test framework change. No test rewrites beyond path updates for migrated languages files.
