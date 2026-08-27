# 18 — Final validation (TB-P06-T013)

Task: `TB-P06-T013`

## Commands executed

```text
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj
dotnet test src/backend/Tooba.slnx
cd src/frontend && npm run typecheck && npm run lint && npm run test && npm run build
git diff --check
```

## Record

| Check | Result |
|---|---|
| Backend build | PASS — 0 Warning(s), 0 Error(s) |
| Backend tests | PASS — Host 241 + MigrationRunner 4 (clear `Tooba__Edition=Marketplace` for MassTransit foundation) |
| Frontend typecheck | PASS |
| Frontend lint | PASS — No ESLint warnings or errors |
| Frontend test | PASS — includes `test:content` (3) + full suite via `npm run test` |
| Frontend build | PASS — Next production build |
| git diff --check | PASS (recorded at commit time) |
| Runtime probes | `/health/live` 200; `/health/ready` 200; `GET /v1/content/articles` 200; detail slug body+category+seo populated; `/blogs` `/blogs/{slug}` `/admin/content` 200 |

## Focus proofs

| Check | Result |
|---|---|
| ContentFoundationTests | Included in Host suite PASS |
| content-api.test.ts | 3/3 PASS |
| Seed backfill Body/SEO/Category | Idempotent update for pre-expand rows |
