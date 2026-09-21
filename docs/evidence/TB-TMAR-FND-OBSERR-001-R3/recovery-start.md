# Recovery start — TB-TMAR-FND-OBSERR-001-R3

## Task

TB-TMAR-FND-OBSERR-001-R3 — Central Error Classification + Localization Catalog + Unified ProblemDetails Pipeline + Offer Final Adoption

## Claim

- Bridge claim id: `6db35f04-df0f-4428-a27f-0b93c0f906ce`
- Channel: `tooba-main`
- Worker: silent Bridge worker (parent ships Result)

## Git safety (pre-mutation)

| Check | Result |
| --- | --- |
| Branch | `main` |
| `HEAD == origin/main` | yes (`8f73ca48e2add2f2802cf88c309a12bbcd1e45bd`) |
| Staged files | 0 |
| Protected ancestor `18ca10c9` is ancestor | yes |
| Stashes | untouched (`stash@{0}` unrelated-pre-r10; `stash@{1}` temp-before-push) |
| User `.rar` archives | untouched / not staged |
| R2 lineage | tip `c7af4d2f` present; HEAD includes R2 tip-align `8f73ca48` |

## Scope

- Backend-only
- ErrorDescriptor catalog (no naming heuristics)
- Delete OfferEndpointLocalizer → .resx / IStringLocalizer
- ApiResponseFactory context-based; thin exception presentation
- PlatformHttpException bounded; Offer pipeline final adoption
- Do **not** commit / push / Bridge from this worker
- Do **not** stage/delete `.rar`

## Prior state

R2 left `FOUNDATION_RUNTIME_TRACING_COMPLETE`. Offer remains `REOPENED_WAITING_CENTRAL_FOUNDATION`.
