# Solution Explorer / Tooba.slnx grouping — AccessControl (W0)

`Solution-Explorer-State = CANONICAL`

## Current `.slnx` entry (`src/backend/Tooba.slnx`, lines 91–96)

```text
<Folder Name="/Modules/AccessControl/">
  <Project Path="Modules/AccessControl/Tooba.AccessControl.Domain/Tooba.AccessControl.Domain.csproj" />
  <Project Path="Modules/AccessControl/Tooba.AccessControl.Contracts/Tooba.AccessControl.Contracts.csproj" />
  <Project Path="Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj" />
  <Project Path="Modules/AccessControl/Tooba.AccessControl.Infrastructure/Tooba.AccessControl.Infrastructure.csproj" />
  <Project Path="Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj" />
</Folder>
```

## Verification

| Check | Result |
| --- | --- |
| Dedicated `/Modules/AccessControl/` Solution Folder exists | YES |
| All five projects present exactly once | YES (`Application` ×1, `Endpoints` ×1, `Domain` ×1, `Contracts` ×1, `Infrastructure` ×1) |
| `Tooba.AccessControl.Endpoints` present in the solution | YES |
| Loose AccessControl entry under a flat `/Modules/` dump | NONE |
| Project paths exist on disk | YES (all five `.csproj` files verified) |
| Assembly names / project paths changed for visual grouping | NO |

## Pre-existing guard defect (F4)

`Tooba.Host.Tests/Architecture/AccessControlModuleAmcW1SolutionGuardTests.cs` fails with
`System.ArgumentOutOfRangeException` at line 20. Cause: the test looks for a flat
`<Folder Name="/Modules/">` window and then searches for `</Folder>` after it:

```csharp
var flatModulesStart = slnx.IndexOf("<Folder Name=\"/Modules/\">", StringComparison.Ordinal);
var flatModulesEnd = slnx.IndexOf("</Folder>", flatModulesStart, StringComparison.Ordinal);
var flatWindow = slnx.Substring(flatModulesStart, flatModulesEnd - flatModulesStart);
```

`Tooba.slnx` no longer contains a flat `<Folder Name="/Modules/">` root folder (modules are grouped
individually), so `IndexOf` returns `-1` and `Substring(-1, ...)` throws.

The guard's **intent** (no AccessControl project may remain in a flat `/Modules/` dump) is still
valid and is satisfied. The defect is in the guard's parser assumption, not in the solution.

W1 repairs this guard by asserting the same intent with a correct parser:

- the dedicated `/Modules/AccessControl/` folder exists,
- each of the five project paths appears exactly once,
- no `Modules/AccessControl/` path appears inside any flat `/Modules/` folder window (evaluated only
  when such a folder exists).

No assertion is removed and no threshold is relaxed.
