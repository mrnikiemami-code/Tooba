# anti-enumeration — TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT

Touched MultiTenancy behavior only (NOT MultiTenancy certification):

| Case | HTTP | Code |
|---|---|---|
| Unknown host | 404 | `platform.resolution.failed` |
| Disabled tenant | 404 | `platform.resolution.failed` |
| Suspended / non-Active | 404 | `platform.resolution.failed` (same `FailClosed()`) |

Evidence:

- `TenantResolutionTests.SingleStore_unknown_host_fails_closed`
- `TenantResolutionTests.SingleStore_disabled_tenant_fails_closed`
- Middleware: `record.Status != TenantStatus.Active` → `FailClosed()` → `PlatformResolutionFailed`

No connection reference / tenant existence / status distinction / stack / exception.Message in failure responses.
