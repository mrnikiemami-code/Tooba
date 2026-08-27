# 09 — Final validation

| Check | Result |
| --- | --- |
| `dotnet build src/backend/Tooba.slnx` | EXIT 0 · 0 Warning(s) · 0 Error(s) |
| `dotnet test src/backend/Tooba.slnx` | Host.Tests **298** + MigrationRunner **4** = **302** · Failed 0 · Skipped 0 |
| `npm run typecheck` | EXIT 0 (after Storage mock fix in cart bootstrap test) |
| `npm run lint` | EXIT 0 · No ESLint warnings or errors |
| `npm run test` ×3 | EXIT 0 · fail 0 |
| `npm run build` | EXIT 0 · Compiled successfully |
| `git diff --check` | clean (verified at commit) |
| focused returns/payment/cart tests | 16/16 pass |

Logs under `docs/evidence/TB-P06-T028-R1/` (`be-build.log`, `be-test.log`, `fe-*.log`).

Code changes in R1 (preview blockers only):

- query cart/actor bootstrap for confirmation
- wallet-quote Host field mapping
- Host-safe middleware rewrite
- refundDestination numeric enum mapping
- Storage-typed test mock for typecheck
- `/customer-panel/dev/wallet-checkout` preview route
