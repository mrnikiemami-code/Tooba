# Recovery start — TB-TMAR-FND-OBSERR-001-R1

## Task

TB-TMAR-FND-OBSERR-001-R1 — Central Observability + Error/ProblemDetails Foundation — Phase 1 Core

## Claim

- Bridge claim id: `df3cb731-25b0-4e80-9afc-e6d19ed0b7fd`
- Channel: `tooba-main`
- Worker: silent Bridge worker (parent ships Result)

## Git safety (pre-mutation)

| Check | Result |
| --- | --- |
| Branch | `main` |
| `HEAD == origin/main` | yes (`c5c7930d1512bd255c98e038cdc119c394ab71fe`) |
| Staged files | 0 |
| Protected ancestor `18ca10c9` is ancestor | yes |
| Stashes | untouched (`stash@{0}` unrelated-pre-r10; `stash@{1}` temp-before-push) |
| User `.rar` archives | untouched / not staged |

## Scope

- Backend-only
- Adapt external `mrnikiemami-code/BuildingBlocks` branch `mastertest` into Tooba BuildingBlocks + Host
- Do **not** duplicate existing OpenTelemetry wiring
- Do **not** commit / push / Bridge from this worker

## External reference

Cloned read-only to `%TEMP%\BuildingBlocks-mastertest` (outside SarvNewVer). Not committed into the repo.
