# privacy-scope — TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT

| Concern | Classification |
|---|---|
| RemoteIpAddress read | ZERO |
| X-Forwarded-For read | ZERO |
| ClientIp scope population | OMITTED_PRIVACY_SAFE_CERTIFIED |
| QueryString / RawTarget / full URL / headers | ZERO |
| HttpPath | PATH_ONLY_NO_QUERY_CERTIFIED (`Path.Value`) |
| Actor | GUID_N_CERTIFIED only |
| Authorization / cookies / password / OTP / body / tokens | ZERO from this middleware |
| Allowed identifiers | correlationId, requestId, tenantId, storeId mirror, actor Guid, method, path |

Canonical scope only: `ObservabilityLogScope.CreateState` + `Begin`.
Custom ActivitySource / Meter: ZERO.
Sensitive-data state: ZERO.
