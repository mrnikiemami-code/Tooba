# path-namespace — TB-TMAR-HOST-HEALTH-AMC-001

| Item | Current | Required if retained |
|---|---|---|
| Path | `Host/Tooba.Host/Health/` | same |
| Namespace | `Tooba.Host` | `Tooba.Host.Health` |
| State | **VIOLATION** | EXACT after W1 |

Impact: Program already calls `HostHealthEndpoints` via root `using Tooba.Host`; after namespace fix add `using Tooba.Host.Health;`. Tests referencing types need usings. No TypeForwardedTo/shim.
