# Validation — TB-TMAR-HOST-CONTENT-AMC-001

## Builds (focused)

| Project | Result |
| --- | --- |
| Tooba.Content.Endpoints | PASSED (0 errors) |
| Tooba.Content.Infrastructure | PASSED (0 errors) |
| Tooba.Host | PASSED (0 errors; pre-existing warnings only) |
| Tooba.Host.Tests | PASSED (0 errors; pre-existing warnings only) |

Commands:

```text
dotnet build src/backend/Modules/Content/Tooba.Content.Endpoints/Tooba.Content.Endpoints.csproj
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj
```

## Guard tests

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~HostContentAmcGuardTests
```

Result: **Passed: 4, Failed: 0**

Coverage:

1. Host/Content `.cs` count == 0 (folder absent)
2. Program maps `MapContentModuleEndpoints` / `AddContentEndpointPresentation`; no leftover MapContent* Host calls
3. Content.Endpoints project + ContentEndpointModule exist
4. Content grid engines live in Infrastructure, not Host/Grid

## Repairs during focused validation

1. CS1591 on public `AdminContentGridQueryEngine` ctor/QueryAsync → XML comments added
2. Missing AspNetCore usings in Endpoints class library → `GlobalUsings.cs`

No second-round failures after repairs.
