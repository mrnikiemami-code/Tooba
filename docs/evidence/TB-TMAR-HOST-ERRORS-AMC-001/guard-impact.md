# guard-impact — TB-TMAR-HOST-ERRORS-AMC-001

| Asset | Impact if W1 deletes mapper / renames namespace |
| --- | --- |
| `PlatformExceptionMapperTests` | Must delete or retarget to SafeErrorMapper/ExceptionPresentationService |
| `ErrorContractTests` (integration) | Keep — exercises live handler path via `/__platform-error` / conflict |
| `OfferArchitectureGuardTests` path assert on `Errors/ToobaExceptionHandler.cs` | Update namespace expectation if asserted; path may stay |
| Program `AddExceptionHandler<ToobaExceptionHandler>` | Update using if namespace → `Tooba.Host.Errors` |
| `TenantResolutionMiddleware` | Must stop calling mapper before delete |
| HostSecurity / HostAdmin cert guards | No Errors coupling expected — preserve |
| Historical OBSERR docs | Historical only |

No durable Errors-folder exact-allowlist guard found today; W2 CERT should add one.
