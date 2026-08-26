# 04 — Startup fail-fast map (TB-P06-T001)

| Dependency | Class | Fail-fast behavior |
|---|---|---|
| Platform edition shape | REQUIRED_TO_START | `ValidateOnStart` via `PlatformOptionsValidator` |
| Production Edition=Unset | REQUIRED_TO_START | Rejected in Production |
| Production missing tenant/connection refs | REQUIRED_TO_START | Rejected in Production |
| Messaging config (when enabled) | REQUIRED_TO_START | `MessagingOptionsValidator` |
| SpiceDB endpoint+token (Mode=SpiceDb) | REQUIRED_TO_START | `AuthorizationOptionsValidator` |
| InMemory auth in Production | REQUIRED_TO_START | Blocked |
| PostgreSQL TCP reachability | REQUIRED_FOR_FEATURE | Fail at request via `DatabaseConnectionResolver` (503), not silent |
| SpiceDB gRPC reachability | REQUIRED_FOR_FEATURE | Fail-closed auth adapter; no fake allow |
| Outbox worker | OPTIONAL at startup | Background service; empty outbox not readiness blocker |
| OTLP collector | OPTIONAL | Empty endpoint = no export |

Error messages are actionable; no secret leakage in exceptions/ProblemDetails.
