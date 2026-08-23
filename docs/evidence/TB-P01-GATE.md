# TB-P01-GATE — Platform Foundation evidence

Gate:

```text
TB-P01-GATE
```

Predecessor:

```text
cdc7c3c42e72466dceef58c5ce82e4c352536c07
```

Recommendation:

```text
P01_GATE_PASS
```

P01 is not marked COMPLETE until Architect ACCEPT.

## Scope reviewed

Accepted chain T001–T009: bootstrap, observability/ProblemDetails, edition/tenant/DB resolution, PostgreSQL persistence, Persian documentation, outbox/events/background, MassTransit 8.5.10 SQL Transport, cache abstraction, module composition/architecture guards.

No new product features were added in this Gate.

## Validation evidence (current run)

Backend (`Tooba.Host.Tests`): restore PASS; build PASS (0 warnings / 0 errors); test PASS — 68 passed, 0 skipped. Suite includes PostgreSQL Testcontainers outbox/persistence tests and MassTransit SQL Transport postgres tests where Docker is available.

Frontend: `npm ci`, `typecheck`, `lint`, `build` PASS (Next.js production build).

`git diff --check` PASS aside from CRLF working-copy warnings.

## Package audit

| Item | Version / state |
| --- | --- |
| .NET target | net8.0 |
| EF Core | 8.0.11 |
| Npgsql EF provider | 8.0.11 |
| Npgsql | 8.0.7 |
| MassTransit | 8.5.10 |
| MassTransit.SqlTransport.PostgreSQL | 8.5.10 |
| OpenTelemetry Exporter/Hosting | 1.15.3 |
| OpenTelemetry ASP.NET/HTTP/Runtime instrumentation | 1.12.0 |
| Next.js | ^15.1.6 |
| React | ^19.0.0 |
| Tailwind | ^3.4.17 |
| MassTransit.RabbitMQ / RabbitMQ.Client / MassTransit 9.x | absent |
| StackExchange.Redis / Caching.StackExchangeRedis | absent |

No Gate package upgrades.

## Architecture invariants

Remain true: Modular Monolith; PostgreSQL canonical; Marketplace vs SingleStore distinct; TenantId != Hostname; SingleStore DB-per-tenant; one messaging transport DB per deployment; module-owned DbContext (`PlatformProbeDbContext`); no `ToobaDbContext`/`AppDbContext`; Domain Event != Integration Event; module transactional outbox retained; Cache != Source of Truth; Redis deferred; backend/module boundary != UI boundary.

Executable `ArchitectureBoundaryTests` guard Domain↛Infrastructure, Application↛Host, foreign module Infrastructure/Persistence, and mega DbContext. PlatformProbe remains a disposable sample.

## Security / isolation

Covered by existing tests and Host code: unknown/disabled tenant fail-closed; Tenant A cannot resolve Tenant B database; Tenant A cache != Tenant B cache; workers reconstruct commerce context from event/outbox metadata, not Host; secrets are not logged; production ProblemDetails do not dump internals; forwarded Host trust requires configured proxies.

## Messaging

Flow remains: module transaction → T006 outbox → dispatcher → `IIntegrationEventPublisher` → MassTransit 8.5.10 PostgreSQL SQL Transport → consumer. At-least-once; no exactly-once claim; retry documented; MassTransit EF Outbox not added (no duplicate outgoing store).

## Caching

In-process Memory provider; no Redis; tenant/edition isolation; Market/Locale/Currency dimensions; GetOrCreate single-flight; tag invalidation. Process-local until Redis.

## Persian documentation

CS1591 is a build error on non-test backend projects via `Directory.Build.props`. Tests disable 1591. Generated EF `Migrations/*.Designer.cs` and `*ModelSnapshot.cs` are the only narrow `.editorconfig` exclusions. Gate did not find a blanket suppression.

## Concern classification

| Concern | Class |
| --- | --- |
| OpenTelemetry contrib vs exporter version split (1.12.0 vs 1.15.3) | DEFERRED_NON_BLOCKING |
| `/__platform-*` diagnostic endpoints before public deploy | DEFERRED_NON_BLOCKING |
| Config-backed tenant registry is not a production control plane | DEFERRED_NON_BLOCKING |
| Npgsql / MassTransit SQL Transport Dapper vs NodaTime constraint | DEFERRED_NON_BLOCKING |
| SQL Transport admin/runtime credential split | DEFERRED_NON_BLOCKING |
| PlatformProbe disposable | RESOLVED |
| Durable Inbox/dedup | DEFERRED_NON_BLOCKING |
| MassTransit delayed redelivery/scheduler | DEFERRED_NON_BLOCKING |
| T006 outbox vs MassTransit EF Outbox future review | DEFERRED_NON_BLOCKING |
| Cache process-local until Redis | DEFERRED_NON_BLOCKING |
| Optional HTTP endpoints kept off `IToobaModule` | DEFERRED_NON_BLOCKING |

No BLOCKER. No REPAIR_BEFORE_P02 required for Gate PASS.

## Final gate recommendation

```text
P01_GATE_PASS
```

Ready for P02 only after Architect ACCEPT of this Gate. Mandatory UX sequence (Deep Shopeiva Study → Design System → Data Grid → visual ACCEPT) remains unexecuted.
