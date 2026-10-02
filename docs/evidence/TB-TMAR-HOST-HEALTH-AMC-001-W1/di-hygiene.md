# di-hygiene — TB-TMAR-HOST-HEALTH-AMC-001-W1

| Check | State |
|---|---|
| `IServiceProvider` in Host/Health | ZERO |
| `RequestServices` in Host/Health | ZERO |
| `GetService<IBusControl>` / `GetRequiredService<IBusControl>` | ZERO |
| Bus injection | `IEnumerable<IBusControl>` explicit DI |
| Zero buses when messaging disabled | empty collection OK |
| Zero buses when messaging enabled | `messaging=bus-unavailable`, not-ready |
| Multiple buses | fail deterministic (`bus-unavailable`) |
| Single bus Unhealthy | `messaging=unhealthy` |
| New Messaging abstraction | NOT created (Architect lock) |
