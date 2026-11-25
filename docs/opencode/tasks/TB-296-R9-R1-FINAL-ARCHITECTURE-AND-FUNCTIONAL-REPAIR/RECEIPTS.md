# TB-296-R9-R1 receipts

## Timing / identity

| Item | Value |
|---|---|
| Base R9 | 63da7a2dbcaf61c0469252c65e5e42b499db6a0e |
| Start | 2026-11-25 19:29:39.875 +03:30 |
| End | 2026-11-25 21:06:26.855 +03:30 |
| Duration | 01:36:46 |
| FE | http://127.0.0.1:3000 |
| Host | http://127.0.0.1:5088 |
| Health | ready (SingleStore, inmemory authorization, messaging Healthy) |

## Gates

| Gate | Result | Evidence |
|---|---|---|
| typecheck | PASS | `evidence/r9r1-typecheck.log` |
| lint | PASS (pre-existing warnings) | `evidence/r9r1-lint.log` |
| build | PASS | `evidence/r9r1-build.log` |
| mounted workspace | 26/26 | `evidence/r9r1-mount.log` |
| admin/seller API | 16/16 | `evidence/r9r1-api.log` |
| model/i18n/routing | 26/26 | `evidence/r9r1-model-i18n.log` |
| Offer backend | 97/97 | `evidence/r9r1-offer-tests.log` |
| Host localization/Catalog/Stage2 | 11/11 | `evidence/r9r1-host-tests.log` |
| frontend architecture | 27 pre-existing unrelated; no task offer file | `evidence/r9r1-architecture.log` |
| OpenSpec R9-R1 | valid strict | `evidence/r9r1-openspec.log` |
| runtime | 32/32 | R9 `evidence/r9-runtime/verify-r9.json` SHA-256 EFB18406… |
| cleanup | ok true | R9 `evidence/r9-runtime/cleanup.json` SHA-256 93256140… |
| performance | 1028 ms / 71 ms | `evidence/r9r1-performance.json` |
| public languages | public DTO only | `evidence/r9r1-public-languages.json` |

## Source size

- `offer-workspace.tsx`: 1161 → 785; new `offer-actions.ts`, `local-detail.ts` under 800.
- `seller-api.ts`: 1488 → 1116, baseline 1211; new `seller-offer-api.ts` 374.
- New shared use case `features/offer-create/offer-create-and-apply.ts`; shared `lib/i18n/registry-locale.ts`.
- No source-size baseline or exception changed.

## Fixtures

R9 cleanup JSON remains authoritative: R9 offers 0, roles [], assignments [], ceiling enabled 0, seed 199 and two-position [0,0] restored. R9 runtime was rerun after refactor and cleanup succeeded.
