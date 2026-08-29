# TB-P07-T016 evidence

## Scope

Level-3-only Product category assignment + Product attributes from Category schema only (no Product-local AttributeDefinition create). Product SEO deferred to TB-P07-T017.

## Validation results

| Check | Result |
|-------|--------|
| Focused catalog product tests | Passed 15 / Failed 0 / Skipped 0 (`01-focused-catalog-tests.log`) |
| Full `Tooba.Host.Tests` | Passed 334 / Failed 0 / Skipped 0 (`06-backend-full-tests.log`) |
| `npm run test:product-workspace` | Passed 32 / Failed 0 (`02-fe-product-workspace-tests.log`) |
| `npx tsc --noEmit` | 0 errors (`03-fe-typecheck.log`) |
| `npm run lint` | No ESLint warnings or errors (`04-fe-lint.log`) |
| `git diff --check` (task paths) | clean (`05-git-diff-check.log`; LF/CRLF warnings only) |

## Runtime

| Service | Port | Probe |
|---------|------|-------|
| Host | :5088 | 200 |
| FE | :3000 | 200 |
| Shopeiva | :3001 | timeout at Worker check (parent should verify) |

Tests used `-o artifacts/t016-test-out` so Host binaries on :5088 were not overwritten.

## Collision

`docs/ai/tasks/TB-P07-T015.task.md` preserved untracked; not implemented. No SEO / no migration / no commit by Worker.
