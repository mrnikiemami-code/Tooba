# TB-TMAR-PLATFORMPROBE-AMC-001-W1 — Fixture extraction

## Destination

`src/backend/Host/Tooba.Host.Tests/Fixtures/PlatformProbe/`

| File | Contents |
|---|---|
| `ProbeEvents.cs` | test-owned domain/integration events; type map `platform_probe.record_created.v1` |
| `TestPlatformProbeDbContext.cs` | `TestPlatformProbeRecord`, `TestPlatformProbeDbContext` (schema `platform_probe`), `TestPlatformProbePersistence` |
| `TestPlatformProbeOutboxRegistration.cs` | test-owned `IOutboxModuleRegistration` with production-equivalent translate/type-map |

## Namespace

`Tooba.Host.Tests.Fixtures.PlatformProbe` — no `Tooba.PlatformProbe.Infrastructure.*`.

## Migrations

Not copied. Tests continue to use `EnsureCreatedAsync` where applicable.
