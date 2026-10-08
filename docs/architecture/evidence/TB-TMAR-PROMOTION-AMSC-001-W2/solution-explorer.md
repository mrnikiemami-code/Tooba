# TB-TMAR-PROMOTION-AMSC-001-W2 — solution explorer

## Solution-Explorer-State: `CANONICAL`

Verified separately from disk, per skill section 17.

`src/backend/Tooba.slnx` contains exactly:

```xml
<Folder Name="/Modules/Promotion/">
  <Project Path="Modules/Promotion/Tooba.Promotion.Domain/Tooba.Promotion.Domain.csproj" />
  <Project Path="Modules/Promotion/Tooba.Promotion.Contracts/Tooba.Promotion.Contracts.csproj" />
  <Project Path="Modules/Promotion/Tooba.Promotion.Application/Tooba.Promotion.Application.csproj" />
  <Project Path="Modules/Promotion/Tooba.Promotion.Infrastructure/Tooba.Promotion.Infrastructure.csproj" />
  <Project Path="Modules/Promotion/Tooba.Promotion.Endpoints/Tooba.Promotion.Endpoints.csproj" />
  <Project Path="Modules/Promotion/Tooba.Promotion.Tests/Tooba.Promotion.Tests.csproj" />
</Folder>
```

## Checks

| Check | Result |
| --- | --- |
| `/Modules/Promotion/` solution folder present | PASS |
| Every on-disk Promotion project present in the group | PASS (6/6) |
| Every group entry resolves to an existing `.csproj` | PASS (no `STALE_PROJECT_ENTRY`) |
| No Promotion project outside the group (flat/wrong folder) | PASS |
| No decorative Solution Folder disconnected from disk | PASS |
| `.slnx` edit required by this wave | NO — the wave moved no `.csproj` and no project boundary changed |

Project count/order note: the `.slnx` entry order (`Domain`, `Contracts`, `Application`, `Infrastructure`,
`Endpoints`, `Tests`) is the pre-existing module order and is unchanged by W2; the durable guard asserts
membership, not order, so no cosmetic churn was introduced.
