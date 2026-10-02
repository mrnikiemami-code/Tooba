# Program disposition map — TB-TMAR-HOST-ROOT-FINAL-CERT-001

Full read of `Program.cs` (first using through `public partial class Program;`). No UNKNOWN dispositions.

| Responsibility | Classification |
| --- | --- |
| Logging (JSON console + scopes) | HOST_COMPOSITION_ROOT |
| Canonical observability foundation / OpenTelemetry | HOST_COMPOSITION_ROOT / GLOBAL_HOST_PLATFORM_BOUNDARY |
| Endpoint-presentation registrations | HOST_COMPOSITION_ROOT |
| Options binding/validation (platform, outbox, messaging, cache, auth) | GLOBAL_HOST_PLATFORM_BOUNDARY |
| StoreContext / current commerce plumbing | GLOBAL_HOST_PLATFORM_BOUNDARY |
| Outbox DI + hosted dispatcher | GLOBAL_HOST_PLATFORM_BOUNDARY |
| Messaging publisher registration | GLOBAL_HOST_PLATFORM_BOUNDARY |
| Cache registration | GLOBAL_HOST_PLATFORM_BOUNDARY |
| AuthSecurity options + session/throttle seams | GLOBAL_HOST_PLATFORM_BOUNDARY |
| MediatR / AddToobaCqrsFoundation (assembly markers) | HOST_COMPOSITION_ROOT |
| Module DI via AddToobaModules + adapters | HOST_COMPOSITION_ROOT |
| Authorization adapters (Host→module contracts) | HOST_COMPOSITION_ROOT |
| JSON options | HOST_COMPOSITION_ROOT |
| Kestrel MaxRequestBodyBytes from AuthSecurity | GLOBAL_HOST_PLATFORM_BOUNDARY |
| CORS (fail-closed empty origins) | GLOBAL_HOST_PLATFORM_BOUNDARY |
| TrustedProxies + IPAddress.Parse fail-fast | GLOBAL_HOST_PLATFORM_BOUNDARY |
| Development bootstrap (Marketplace/SingleStore/seeds) | DEVELOPMENT_ONLY_COMPOSITION |
| Middleware order (forwarded→correlation→exception→CORS→security→tenant→session→obs) | GLOBAL_HOST_PLATFORM_BOUNDARY |
| Module endpoint Map* composition | HOST_COMPOSITION_ROOT |
| HostHealthEndpoints.Map | GLOBAL_HOST_PLATFORM_BOUNDARY |
| /__platform-error|conflict|commerce | DEVELOPMENT_ONLY_COMPOSITION |
| app.Run | HOST_COMPOSITION_ROOT |
| public partial class Program | HOST_COMPOSITION_ROOT (WebApplicationFactory anchor) |

`Program-Disposition-Map-State = COMPLETE_NO_UNKNOWN`
