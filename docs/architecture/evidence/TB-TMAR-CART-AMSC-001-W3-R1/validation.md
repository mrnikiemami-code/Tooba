# TB-TMAR-CART-AMSC-001-W3-R1 — Validation

Bounded validation only. No full solution test suite, no unrelated drift repair.

| Check | Command | Result |
|---|---|---|
| SoT JSON parse | `node -e "require('./docs/architecture/tmar-current-state.json')"` | **OK** |
| Manifest JSON parse | `node -e "require('./docs/architecture/tmar-module-structure-manifests.json')"` | **OK** |
| Focused Cart W3 cert guard (incl. new reconciliation lock) | `dotnet test Host/Tooba.Host.Tests --filter FullyQualifiedName~CartModuleAmsc001W3CertGuardTests` | **12 passed / 0 failed** |
| Composed error-catalog uniqueness | `--filter FullyQualifiedName~ErrorCatalogUniqueCodeGuardTests` | **3 passed / 0 failed** |
| Manifest structural fields unchanged | read-back of `structureCertified`, `lockVersion`, `projects`, `rootAllowlist`, `forbiddenTopLevelFolders` | **UNCHANGED** |

## Production / schema / resource delta proof

`git diff --stat` for this R1 commit must contain only:

- `docs/architecture/tmar-current-state.json`
- `docs/architecture/tmar-module-structure-manifests.json` (note wording only)
- `docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W3-R1/*`
- `docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W3/*` (behavior statement correction)
- `src/backend/Host/Tooba.Host.Tests/Architecture/CartModuleAmsc001W3CertGuardTests.cs` (test-only)
- `docs/ai/tasks/TB-TMAR-CART-AMSC-001-W3-R1.task.md` (claim artifact)

Zero `src/backend/Modules/Cart/**` production changes; zero migrations; zero `.resx` changes.

## Pre-existing / unrelated failures (recorded, not repaired)

`TmarSourceSizeAndInfraAppTests` remains failing for the already-recorded repo-wide reason (untracked
`.tmp-baseline/` working copy plus stale baseline keys in other modules). Not caused by this task and
out of its allowed scope; `.tmp-baseline` was not touched.

## Commit / push state

- One dedicated R1 commit; pushed to `origin/main`; `HEAD == origin/main`.
- Working tree clean except the pre-existing unrelated untracked artifacts
  (`TB-TMAR-ORDER-AMC-001-W5-R1/worker-result.txt`,
  `TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/{RESULT.bridge.txt,post-result.js}`), preserved untouched.
