# Full validation — TB-P07-T029-R1

## Backend
Commands (isolated `-o artifacts/t029-r1-test-out` so live Host :5088 stayed up):

```
dotnet restore src/backend/Host/Tooba.Host/Tooba.Host.csproj
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Debug -o artifacts/t029-r1-test-out/host
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Debug -o artifacts/t029-r1-test-out/host-tests
dotnet test src/backend/Host/Tooba.MigrationRunner.Tests/Tooba.MigrationRunner.Tests.csproj -c Debug -o artifacts/t029-r1-test-out/mig-tests
```

Results:
- restore: up-to-date
- build: **0 Warning(s), 0 Error(s)**
- Host.Tests: **Passed 345, Failed 0, Skipped 0**
- MigrationRunner.Tests: **Passed 4, Failed 0, Skipped 0**
- Backend total: **349 passed / 0 failed / 0 skipped**

Log: `full-validation-backend.log`

## Frontend
```
npm run typecheck
npm run lint
npm test
npm run build
```

Results:
- typecheck: PASS (`tsc --noEmit`)
- lint: exit 0; pre-existing unused-var warnings in `catalog-facet-api.ts` / `catalog-mega-menu-api.ts` (unchanged by R1; not introduced here)
- tests: **455 passed / 0 failed / 0 skipped** across npm test suites (see `fe-test-summary.log`)
- production build: PASS after clean `.next` (`Compiled successfully`)

Log: `full-validation-frontend.log`

## git diff --check
```
git diff --check -- <R1 media UX files>
```
**PASS** — `git-diff-check.log`

## Runtime (kept alive)
- Backend `:5088` health/live 200
- Frontend `:3000/admin` 200 (restarted after clean `.next` so HEAD UX loaded)
- Shopeiva `:3001` 200
