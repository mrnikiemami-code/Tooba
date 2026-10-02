# path-namespace — TB-TMAR-HOST-OBSERVABILITY-AMC-001

| Item | Value |
|---|---|
| Physical path | `Host/Tooba.Host/Observability/` |
| Current namespace | `Tooba.Host` |
| Expected if retained | `Tooba.Host.Observability` |
| State | **VIOLATION_Tooba.Host_vs_Observability** |
| Alias/shim/TypeForwardedTo | ZERO |

## Impact if repaired in W1

- File: change namespace to `Tooba.Host.Observability`
- Program: add `using Tooba.Host.Observability;` (type currently resolved via `Tooba.Host`)
- Tests: update any fully-qualified references (currently none beyond Program string contains)
