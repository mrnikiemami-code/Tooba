# Solution-Explorer-State

**Verdict: `CANONICAL`**

## 1. Solution file

`src/backend/Tooba.slnx` — the module is grouped under a single canonical solution folder.

```xml
<Folder Name="/Modules/Returns/">
  <Project Path="Modules/Returns/Tooba.Returns.Domain/Tooba.Returns.Domain.csproj" />
  <Project Path="Modules/Returns/Tooba.Returns.Contracts/Tooba.Returns.Contracts.csproj" />
  <Project Path="Modules/Returns/Tooba.Returns.Application/Tooba.Returns.Application.csproj" />
  <Project Path="Modules/Returns/Tooba.Returns.Infrastructure/Tooba.Returns.Infrastructure.csproj" />
  <Project Path="Modules/Returns/Tooba.Returns.Endpoints/Tooba.Returns.Endpoints.csproj" />
  <Project Path="Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj" />
</Folder>
```

## 2. Verification

| Check | Result |
|---|---|
| `/Modules/Returns/` folder exists in `.slnx` | YES |
| Projects listed under it | 6 |
| Projects present on disk | 6 |
| Disk projects **missing** from `.slnx` | 0 |
| `.slnx` entries pointing at non-existent paths | 0 |
| Decorative Solution Folders disconnected from disk | 0 |
| Assemblies renamed for visual effect | 0 |
| `.csproj` moved for visual effect | 0 |
| Endpoints project present on disk and in `.slnx` | YES (section 17 requirement) |

Enumerated `.csproj` on disk:

```text
src/backend/Modules/Returns/Tooba.Returns.Application/Tooba.Returns.Application.csproj
src/backend/Modules/Returns/Tooba.Returns.Contracts/Tooba.Returns.Contracts.csproj
src/backend/Modules/Returns/Tooba.Returns.Domain/Tooba.Returns.Domain.csproj
src/backend/Modules/Returns/Tooba.Returns.Endpoints/Tooba.Returns.Endpoints.csproj
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Tooba.Returns.Infrastructure.csproj
src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj
```

## 3. Visual Studio result

In Solution Explorer the module now collapses to exactly one node per project under
`Modules/Returns/`, with the application tree reading:

```text
Tooba.Returns.Application
  Composition
    ReturnsOperation.cs
    ReturnRefundDestinationParser.cs
  ReturnRequests
    Commands      (4 request files)
    Models        (9 model files)
    Ports         (4 port files)
    Queries       (7 query files)
  Validation
    ReturnsRequestValidators.cs
    ReturnsValidationCodes.cs
```

No `Commands/<UseCase>/` chain of single-file folders remains, so the previous 11-deep collapsed
tree is gone and the project no longer reads as a technical-axis-first layout.

## 4. Enforcement

`ReturnsModuleAmsc001W2StructureGuardTests.Solution_explorer_grouping_is_canonical` asserts the
`<Folder Name="/Modules/Returns/">` block contains all six project paths. Any future regression
(missing grouping, dropped Endpoints entry, stale path) fails the durable guard.
