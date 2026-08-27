# 18 — Final validation

Task: TB-P06-T024-R1

## Backend

```text
dotnet build src/backend/Tooba.slnx
→ see host-build.log — Build succeeded, 0 Warning(s), 0 Error(s)

dotnet test src/backend/Tooba.slnx --filter FullyQualifiedName~Tooba.Host.Tests
→ see dotnet-test-full.log / dotnet-test-rebuild.log
```

### Host.Tests (logged)

| Metric | Value |
|--------|-------|
| Passed | **274** |
| Failed | **0** |
| Skipped | **0** |

Includes `AccessControlFoundationTests` + `AccessControlRuntimeScopeTests` (Docker/Testcontainers required for skippable facts).

### Full solution test

If full `dotnet test src/backend/Tooba.slnx` still running or not re-logged — **see logs in this folder** for final warnings/errors/failed/skipped counts.

## Frontend

```text
npm run typecheck  → frontend-typecheck.log — exit 0
npm run lint       → frontend-lint.log — No ESLint warnings or errors
npm run test       → frontend-test.log — see note below
npm run build      → (not yet logged in this folder — run if needed)
```

### Frontend test note

`frontend-test.log` shows **1 failure** in `panel-nav-integrity.test.ts`:

```text
✖ vendor shell exports deferred hrefs and filters live-only nav
```

Remaining seller tests in that file pass. Full `npm run test` grid may abort before later suites — **re-run and refresh log before Worker PASS claim**.

## Repo hygiene

```text
git diff --check
→ (run before commit — trailing whitespace in evidence files per task §S)
```

## Readiness claims (allowed when all green)

- `ACCESS_CONTROL_CENTER_COMPLETE_FOR_CURRENT_SCOPE`
- `CATEGORY_SCOPED_ORDER_ACCESS_LIVE`
- `REAL_DATA_SCOPE_SELECTORS_LIVE`

## Not claimed

- `ALL_RESOURCE_SCOPES_FULLY_INTEGRATED` (Warehouse/Store/OrderSegment deferred)
- `USER_VISUAL_ACCEPTED`
- `PRODUCT_FULLY_READY`

## Log files (this folder)

| Log | Purpose |
|-----|---------|
| `host-build.log` | Backend build |
| `dotnet-test-full.log` | Host.Tests run |
| `dotnet-test-rebuild.log` | Host.Tests rebuild run |
| `frontend-typecheck.log` | tsc |
| `frontend-lint.log` | ESLint |
| `frontend-test.log` | npm test (partial — check failure) |
