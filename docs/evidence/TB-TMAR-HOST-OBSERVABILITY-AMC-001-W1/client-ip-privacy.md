# client-ip-privacy — TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1

| Check | State |
|---|---|
| `RemoteIpAddress` read | **ZERO** |
| ClientIp passed to `ObservabilityLogScope.CreateState` | **OMITTED** |
| `X-Forwarded-For` read | **ZERO** |
| Custom IP hashing | **ZERO** |
| Custom IP masking | **ZERO** |
| BuildingBlocks ClientIp key/type | **UNTOUCHED** (optional; trusted-proxy-safe contract retained) |

Reason: Host cannot durably prove trusted-proxy-safe for every request at this boundary; privacy-safe omission preferred over inventing hash/mask/proxy state in W1.
