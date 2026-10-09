# Solution Explorer grouping (skill §17)

`Solution-Explorer-State = CANONICAL`

Filesystem and Solution Explorer were verified **separately**. Assembly names and `.csproj` paths were
not changed for visual reasons.

## `src/backend/Tooba.slnx`

```xml
<Folder Name="/Modules/Settlement/">
  <Project Path="Modules/Settlement/Tooba.Settlement.Domain/Tooba.Settlement.Domain.csproj" />
  <Project Path="Modules/Settlement/Tooba.Settlement.Application/Tooba.Settlement.Application.csproj" />
  <Project Path="Modules/Settlement/Tooba.Settlement.Contracts/Tooba.Settlement.Contracts.csproj" />
  <Project Path="Modules/Settlement/Tooba.Settlement.Infrastructure/Tooba.Settlement.Infrastructure.csproj" />
  <Project Path="Modules/Settlement/Tooba.Settlement.Endpoints/Tooba.Settlement.Endpoints.csproj" />
  <Project Path="Modules/Settlement/Tooba.Settlement.Tests/Tooba.Settlement.Tests.csproj" />
</Folder>
```

| Check | Result |
|---|---|
| Solution folder name is canonical | `<Folder Name="/Modules/Settlement/">` present |
| All six on-disk projects are members | 6/6 (Domain, Application, Contracts, Infrastructure, Endpoints, Tests) |
| No `.slnx` entry points at a deleted path | none — every `Project Path` resolves on disk |
| Endpoints project present (skill §17 hard rule) | yes |
| Decorative/disconnected Solution Folder | none |
| Project count on disk vs solution | 6 vs 6 |

## Disk ↔ solution consistency

```text
src/backend/Modules/Settlement/
  Tooba.Settlement.Contracts/       Tooba.Settlement.Contracts.csproj
  Tooba.Settlement.Domain/          Tooba.Settlement.Domain.csproj
  Tooba.Settlement.Application/     Tooba.Settlement.Application.csproj
  Tooba.Settlement.Infrastructure/  Tooba.Settlement.Infrastructure.csproj
  Tooba.Settlement.Endpoints/       Tooba.Settlement.Endpoints.csproj
  Tooba.Settlement.Tests/           Tooba.Settlement.Tests.csproj
```

Assembly names, target framework and project references are untouched by W2 (W1 only repointed the
Application/Infrastructure project references required by its moves).

## Durable enforcement

`SettlementModuleAmsc001W2StructureGuardTests.Solution_explorer_grouping_is_canonical` asserts the
`/Modules/Settlement/` block exists and contains all six project entries, so a future edit that drops
a project from the solution (or renames the folder) fails the guard.
