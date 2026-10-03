# TB-TMAR-PLATFORMPROBE-AMC-001-W0 — Physical tree before

## On-disk (production source only)

```text
src/backend/Modules/PlatformProbe/
  Tooba.PlatformProbe.Infrastructure/
    PlatformProbeModule.cs
    PlatformProbeOutboxRegistration.cs
    Tooba.PlatformProbe.Infrastructure.csproj
    Events/
      ProbeEvents.cs
    Persistence/
      PlatformProbeDbContext.cs   # also hosts PlatformProbeRecord + PlatformProbePersistence + design-time factory
      Migrations/
        20260823000054_InitialPlatformProbe.cs
        20260823000054_InitialPlatformProbe.Designer.cs
        20260823010100_AddPlatformProbeOutbox.cs
        20260823010100_AddPlatformProbeOutbox.Designer.cs
        PlatformProbeDbContextModelSnapshot.cs
```

## Absent by design

- Domain / Application / Contracts / Endpoints projects: **none**
- HTTP routes / MediatR requests: **none**
- Manifest entry in `tmar-module-structure-manifests.json`: **none**
- `structureLock.certifiedModules` membership: **none**

## Solution Explorer

`Tooba.slnx`: project is listed under flat `/Modules/` (alongside `Tooba.ModuleContracts`), **not** under `/Modules/PlatformProbe/`.

Path on disk is already `Modules/PlatformProbe/...`; Solution Explorer grouping is non-canonical relative to ARCH-COMPLETE-002 `/Modules/<Name>/` convention used by business modules.
