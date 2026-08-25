# 13 — R1 Validation Proof

Task: `TB-P05-T015-R1` (repair of `TB-P05-T015`)

Verified on 2026-08-26.

## Backend

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj
Passed: 196
Failed: 0
Skipped: 0
```

PostgreSQL: Docker `postgres-db` on `127.0.0.1:5432` with `tooba_alpha`, `tooba_disabled`, `tooba_marketplace`, `tooba_messaging`.

## Frontend

```text
npm run test:customer   → 21/21 pass
npm run build           → success
```

## Runtime (live Host)

Probe file: `_api-probe-r1.json`

| Check | Result |
| --- | --- |
| GET `/v1/customer/profile` (seed actor) | 200, `displayName` = saved value |
| GET `/v1/customer/dashboard` (seed actor) | 200, greeting `displayName` matches profile |
| GET `/v1/customer/profile` (actor B) | 200, separate owner row (no cross-read of seed data in UI) |
| PUT `/v1/customer/profile` (actor B) | 200, updates actor B only |
| Anonymous GET in Development | resolves controlled guest/dev seam (Production remains 401 per contract tests) |

## Visual

All six mandatory PNGs captured live (see `11-shopeiva-profile-fidelity.md`).
