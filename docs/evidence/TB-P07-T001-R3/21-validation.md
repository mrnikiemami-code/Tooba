# TB-P07-T001-R3 — Validation

## Frontend
| Check | Result |
| --- | --- |
| `npm run typecheck` | TC=0 |
| `npm run lint` | LINT=0 |
| `npm run test` | TEST=0 |
| `npm run build` | BUILD=0 |

## Backend
| Check | Result |
| --- | --- |
| `dotnet test src/backend/Tooba.slnx` | FULL_TEST_EXIT=0 — Host **301** pass / MigrationRunner **4** pass / **0** fail / **0** skip |
| Auth composition | Product handlers RequireAuthorizedAsync count = **16** |

## Runtime (kept alive)
- Host `:5088` health ok
- FE `:3000` Admin 200
- Shopeiva `:3001` 200

## Diff hygiene
`git diff --check` clean (CRLF warnings only).
