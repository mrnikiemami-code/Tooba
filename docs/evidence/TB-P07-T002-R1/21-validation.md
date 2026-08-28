# TB-P07-T002-R1 — Validation

## Backend (Host stopped → restore/build/test → restarted)

| Step | Result |
|------|--------|
| Stop Host :5088 | PID 32436 stopped |
| `dotnet restore` | OK |
| `dotnet build` | 0 errors, 0 warnings |
| `dotnet test` (Tooba.Host.Tests) | 301 passed, 0 failed, 0 skipped |

## Frontend

| Step | Result |
|------|--------|
| `npm run typecheck` | 0 errors |
| `npm run lint` | 0 errors (1 img warning) |
| `npm test` | all suites green |
| `npm run build` | 0 errors |

## Live API smoke

`POST /v1/admin/products/query` with Dev admin actor:

- Status: **200** (was 405 on stale Host binary)
- Sample: `page=1 pageSize=5` → `items=5 totalCount=82`

## Architecture repair

- Replaced in-memory `BuildListItemsAsync(maxRows:null)` + `AdminProductGridEvaluator` path in `QueryGridAsync`
- Added `AdminProductGridQueryEngine` — SQL/catalog filters, module aggregate ID sets, page-only enrichment via `BuildListItemsForProductIdsAsync`
- No cross-module SQL JOIN; module boundaries preserved

## Runtimes after validation

| Service | URL | Status |
|---------|-----|--------|
| Host | http://127.0.0.1:5088 | live |
| FE | http://127.0.0.1:3000 | (unchanged) |
| Shopeiva | http://127.0.0.1:3001 | (unchanged) |
