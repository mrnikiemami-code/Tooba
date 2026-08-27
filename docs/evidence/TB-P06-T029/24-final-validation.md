# 24 — Final validation (TB-P06-T029)

| Check | Result |
| --- | --- |
| `dotnet build` | EXIT 0 · 0 Warning(s) · 0 Error(s) |
| `dotnet test` | Host **298** + MigrationRunner **4** = **302** · Failed 0 · Skipped 0 |
| `npm run typecheck` | EXIT 0 |
| `npm run lint` | EXIT 0 · No ESLint warnings or errors |
| `npm run test` | EXIT 0 · fail 0 |
| `npm run build` | EXIT 0 |
| `git diff --check` | clean at commit |

Code change this gate: customer dashboard honesty fix (wallet/tickets/gift no longer labeled unavailable).
