# TB-296-R10 receipts

## Identity
- Base: 295d8241d277f4320f90ee9e5e75e907dfa5905f
- Status: BLOCKED
- Start: 2026-11-26 00:14:01.559 +03:30
- FE: http://127.0.0.1:3000
- Host: http://127.0.0.1:5088
- Health: ready, SingleStore, inmemory authorization, messaging Healthy

## Executed evidence
| Gate | Result | File |
|---|---|---|
| typecheck | PASS | `evidence/r10-typecheck.log` |
| lint | PASS pre-existing warnings | `evidence/r10-lint.log` |
| build | PASS | `evidence/r10-build.log` |
| Offer backend | 97/97 | `evidence/r10-offer-tests.log` |
| Host Localization/Catalog | 9/9 | `evidence/r10-host-tests.log` |
| mount | 26/26 | `evidence/r10-runtime` |
| API | 16/16 | R9-R1 evidence r9r1-api.log |
| model/i18n/routing | 26/26 | R9-R1 evidence r9r1-model-i18n.log |
| R9 behavior runtime | 32/32 | `evidence/r10-runtime-verify.json` SHA-256 DA00DD5B… |
| cleanup | ok | `evidence/r10-runtime-cleanup.json` SHA-256 93256140… |
| public languages | public DTO | `evidence/r10-public-languages.json` |
| OpenSpec | valid | `evidence/r10-openspec.log` |

## Blockers
R10-01/03/05/06/09 full/14 matrix. Exact owning-domain authority and authorization are absent; no migrations or fabricated contracts.
