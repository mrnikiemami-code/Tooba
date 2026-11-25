# TB-296-R10 receipts

## Identity
- Base: 295d8241d277f4320f90ee9e5e75e907dfa5905f
- Branch: opencode/iso-agents-002
- FE: http://127.0.0.1:3000
- Host: http://127.0.0.1:5088
- DB: tooba_alpha
- Build: 4ipc4fsBsrOYcuqaJQCCB
- Start: 2026-11-26 00:14:01.559 +03:30
- Status: PASS_WITH_LIMITATIONS / READY_FOR_ARCHITECT_REVIEW
- Implementation commit: 3cc0b31421fc735a2769c6f2af1a30c7c58a5441
- End: 2026-11-26 02:25:45.000 +03:30
- Duration: 02:11:43

## Applied dev migration
- `20261126120000_R10OfferHistory` applied to tooba_alpha (additive `offer.offer_history`).
- History GET observed total 18 before final test cleanup.

## Gates
| Gate | Result | File |
|---|---|---|
| typecheck | PASS | `evidence/r10-final2-typecheck.log` |
| lint | PASS (pre-existing warnings) | `evidence/r10-final2-lint.log` |
| build | PASS | `evidence/r10-final2-build.log` |
| Offer backend | 123/123 | `evidence/r10-final2-offer-tests.log` |
| Host Locale/Catalog/Stage2 | 11/11 | `evidence/r10-final2-host-tests.log` |
| mount | 26/26 | `evidence/r10-final-mount.log` |
| admin/seller API | 16/16 | `evidence/r10-final-api.log` |
| model/i18n/routing | 26/26 | `evidence/r10-final-model-i18n.log` |
| runtime | 42/42, 0 errors | `evidence/r10-runtime/verify-r10.json` SHA-256 983FBE8A… |
| cleanup | ok true | `evidence/r10-runtime/cleanup.json` SHA-256 93256140… |
| CAS | 1 winner / 1 conflict / restored | `evidence/r10-cas.json` SHA-256 700E47B6… |
| performance | form median 1014/p95 1078; picker 61/97 | `evidence/r10-performance.json` SHA-256 9529EB3D… |
| architecture | 27 pre-existing baseline; no offer size violation | `evidence/r10-final3-architecture.log` |
| OpenSpec | valid (warnings only) | `evidence/r10-final2-openspec.log` |

## Cleanup
- R9/R10 test offers remaining 0; roles [] assignments [] ceiling 0; seed 199 and two-position [0,0] restored.
- Migration kept (additive) because it is the required production history schema.
