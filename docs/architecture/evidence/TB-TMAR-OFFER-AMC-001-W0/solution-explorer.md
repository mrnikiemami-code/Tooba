# Offer Solution Explorer / Tooba.slnx — W0

Solution-Explorer-State: `CANONICAL`

## `src/backend/Tooba.slnx` (lines 133–140)

```xml
<Folder Name="/Modules/Offer/">
  <Project Path="Modules/Offer/Tooba.Offer.Application/Tooba.Offer.Application.csproj" />
  <Project Path="Modules/Offer/Tooba.Offer.Contracts/Tooba.Offer.Contracts.csproj" />
  <Project Path="Modules/Offer/Tooba.Offer.Domain/Tooba.Offer.Domain.csproj" />
  <Project Path="Modules/Offer/Tooba.Offer.Endpoints/Tooba.Offer.Endpoints.csproj" />
  <Project Path="Modules/Offer/Tooba.Offer.Infrastructure/Tooba.Offer.Infrastructure.csproj" />
  <Project Path="Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj" />
</Folder>
```

## Verification

| Check | Result |
| --- | --- |
| All 6 on-disk projects mapped | PASS |
| Grouping name `/Modules/Offer/` matches disk `Modules/Offer/` | PASS |
| `Tooba.Offer.Endpoints` present (project exists on disk) | PASS |
| `Tooba.Offer.Tests` present | PASS |
| No stale entry pointing at a deleted path | PASS |
| No decorative folder disconnected from disk | PASS |
| No missing project entry vs disk | PASS |

Disk ⇄ slnx equality holds exactly (6 ⇄ 6). No rename of assemblies or `.csproj` paths is required or permitted for visual reasons.

## Action

**No change required in W2** for solution grouping. The `Structure` wave will re-verify this file and will extend the durable guard to assert the exact 6-entry `/Modules/Offer/` set.
