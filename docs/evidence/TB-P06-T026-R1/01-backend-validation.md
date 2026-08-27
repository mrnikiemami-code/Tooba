# 01 — Backend validation

Task: TB-P06-T026-R1

## Commands

```text
dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx
```

## Results

| Step | Result |
|------|--------|
| restore | exit 0 |
| build | **0 Warning(s), 0 Error(s)** |
| MigrationRunner.Tests | Passed **4** / Failed 0 / Skipped 0 |
| Host.Tests | Passed **286** / Failed 0 / Skipped 0 |
| **Totals** | Passed **290** / Failed **0** / Skipped **0** |

Logs: `backend-restore.log`, `backend-build.log`, `backend-test.log`
