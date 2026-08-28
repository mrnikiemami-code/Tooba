# TB-P07-T010 evidence

## Backend
- Full Host.Tests: Passed 325 / Failed 0 / Skipped 0
- Log: `backend-full-tests.log`

## Frontend
- typecheck / lint / test / build: exit 0
- Logs: `fe-typecheck.log`, `fe-lint.log`, `fe-test.log`, `fe-build.log`
- Route present in production build: `/category/[slug]`

## Runtime smoke
- Host `GET /v1/storefront/category-plp/{slug}?locale=fa-IR` → 200 for live slug
- Runtimes kept: Host `:5088`, FE `:3000`, Shopeiva `:3001`

## USER_VISUAL_ACCEPTED
NO
