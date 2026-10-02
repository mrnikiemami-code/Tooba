# resolution-security — TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT

| Case | Result |
|---|---|
| Edition Unset | 503 platform.edition.unconfigured |
| Marketplace connection missing | 503 platform.connection.unconfigured |
| Unknown/inactive/disabled/suspended | 404 platform.resolution.failed (identical FailClosed) |

Tenant-id header NOT authority. SkipPrefixes exact: /health /ready /__platform-error /__platform-conflict.
Canonical presentation: IExceptionPresentationService + Foundation codes. Dual LogWarning + presentation logging = accepted operational + presentation signals.
