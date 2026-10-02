# messaging-di — TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT

| Check | State |
|---|---|
| Injection | `IEnumerable<IBusControl>` explicit DI |
| IServiceProvider / RequestServices | ZERO |
| GetService/GetRequiredService bus | ZERO |
| Enabled + 0 buses | `bus-unavailable` / not-ready |
| Enabled + 1 bus | `CheckHealth` |
| Enabled + N buses | deterministic `bus-unavailable` |
| Disabled | no bus required; `messaging=disabled`, `transport=n/a` |
| Unhealthy | `messaging=unhealthy` |
| Publish/consume/retry ownership | ZERO in Health |
| MassTransit scope | transport readiness only |
