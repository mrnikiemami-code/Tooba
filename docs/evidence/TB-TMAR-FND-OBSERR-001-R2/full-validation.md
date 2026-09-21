# Full validation — TB-TMAR-FND-OBSERR-001-R2

| Command | Result |
| --- | --- |
| `dotnet test Tooba.BuildingBlocks.Tests` | 29/29 PASS |
| `dotnet test Tooba.Offer.Tests` | 41/41 PASS |
| `dotnet test Tooba.Host.Tests --filter CorrelationRuntimeTests` | 3/3 PASS |
| `dotnet build Tooba.Host` | PASS |
| Frontend | NONE (not run) |

Backend solution full matrix not exhaustively re-run beyond focused suites above; Host build succeeded with module graph.
