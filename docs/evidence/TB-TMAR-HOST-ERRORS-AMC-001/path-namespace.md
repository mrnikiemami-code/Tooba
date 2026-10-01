# path-namespace — TB-TMAR-HOST-ERRORS-AMC-001

| File | Physical path | Declared namespace | Required path-derived |
| --- | --- | --- | --- |
| PlatformExceptionMapper.cs | Host/Errors/ | `Tooba.Host` | `Tooba.Host.Errors` |
| ToobaExceptionHandler.cs | Host/Errors/ | `Tooba.Host` | `Tooba.Host.Errors` |

## Classification

**VIOLATION** — exact path↔namespace lock not satisfied.

Not an accepted intentional global Host-namespace exception in current Architecture locks.

Repair belongs in migration wave (rename namespace + update Program/usings/tests). Do **not** repair in Analyze.
