# Build / tests — TB-TMAR-FE-BASELINE

## Commands

```text
cd src/frontend
npm run test:architecture
npm run test:critical-storefront
npm run typecheck
```

Focused backend (repo-wide size freeze already includes FE oversized entries):

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~TmarSourceSize
```

## Results

| Suite | Result | Notes |
| --- | --- | --- |
| `npm run test:architecture` | PASS (7/7) | source-size, import-boundary, test-discovery, SEO characterization |
| `npm run test:critical-storefront` | 1 pre-existing FAIL | `component-compliance.guard.test.ts`: `storefront-landing-blocks.tsx:285 bg-white` — untouched by this task; not fixed (no product/visual change in baseline) |
| `npm run typecheck` | FAIL (legacy) | Existing admin/grid typing errors; none introduced by architecture guard files |
| `dotnet test …TmarSourceSize*` | PASS (6/6) | inventory refreshed to 1741; oversized freeze intact |
| Architecture artifacts | PASS | Evidence under `docs/evidence/TB-TMAR-FE-BASELINE/` |

## Scope compliance

- No broad frontend refactor
- No dependency install/uninstall
- No folder moves
- No product behavior / visual changes
- Backend production code unchanged

Canonical FE validation for this task = architecture guards + evidence pack.
