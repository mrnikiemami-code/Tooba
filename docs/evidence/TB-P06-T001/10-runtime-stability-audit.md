# 10 — Runtime stability audit (TB-P06-T001)

| Area | Finding / action |
|---|---|
| Graceful shutdown | Background services honor `CancellationToken` (Outbox, CartExpiry) |
| Cancellation on I/O | Standard ASP.NET + EF patterns; no deep refactor |
| HTTP clients | OTel instrumentation only; `IHttpClientFactory` deferred until external HTTP integrations |
| SpiceDB gRPC | `GrpcChannel` with `IDisposable` cleanup |
| Startup/shutdown logs | JSON console with UTC timestamps |
| HostOptions.ShutdownTimeout | Default; explicit drain tuning deferred |

No critical in-scope stability defect left unaddressed.
