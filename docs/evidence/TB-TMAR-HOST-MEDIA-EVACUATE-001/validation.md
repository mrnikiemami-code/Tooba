# TB-TMAR-HOST-MEDIA-EVACUATE-001 — Validation

Budget: `MAX_REPAIR_ITERATIONS = 1` (consumed: 1), `MAX_VALIDATION_COMMAND_RUNS = 6` (consumed: 5).

## Commands

| # | Command | Result |
| --- | --- | --- |
| 1 | `dotnet build Modules/Media/Tooba.Media.Endpoints/Tooba.Media.Endpoints.csproj` | FAIL (11× CS0246, missing ASP.NET using directives) — repair 1 |
| 2 | `dotnet build Modules/Media/Tooba.Media.Endpoints/...` (after repair 1) | PASS, 0 errors |
| 3 | `dotnet build Host/Tooba.Host/Tooba.Host.csproj` | PASS, 0 errors |
| 4 | `dotnet build Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj` | PASS, 0 errors |
| 5 | `dotnet test Host/Tooba.Host.Tests --no-build --filter "HostMediaEvacuationGuardTests\|MediaDamTests\|AdminPanelCompositionTests\|HostAdminCanonicalCertificationGuardTests"` | **PASS 32, Skipped 1, Failed 0** |

No broad Host suite. No solution-wide test run.

## Focused test outcome

```
Passed! - Failed: 0, Passed: 32, Skipped: 1, Total: 33 - Tooba.Host.Tests.dll (net8.0)
```

Skipped: `MediaDamTests.Upload_jpeg_success_rejects_plain_and_oversized_and_pages_library`
— `SkippableFact` guarded on Docker/Testcontainers PostgreSQL availability (pre-existing
environmental skip, unchanged by this task).

## Repair log (1/1)

- Repair 1: `MediaAdminEndpoints.cs` / `MediaAssetServing.cs` needed explicit
  `Microsoft.AspNetCore.Http` (+ Builder/Routing for the handler file) usings, because the
  building-blocks global usings do not cover ASP.NET Core namespaces in this project.
  Also removed the unnecessary `InternalsVisibleTo Include="Tooba.Host.Tests"` from the
  Media.Endpoints csproj so the project carries a literal zero `Tooba.Host` reference.
- Subsequent builds/tests passed; no second repair was required.

## Guard coverage proved by HostMediaEvacuationGuardTests

1. Host `Media/MediaEndpoints.cs` absent, Host `Media/` folder absent.
2. No `namespace Tooba.Host.Media` remains in Host sources.
3. `Program.cs` no longer imports/maps Host Media; maps `MapMediaModuleEndpoints()`.
4. All four route patterns + `DisableAntiforgery()` present under Media.Endpoints.
5. Media.Endpoints sources/csproj have zero `Tooba.Host` reference.
6. Three admin handlers use the neutral `IAdminPanelAccess` seam.
7. `media.upload.failed` / `media.missing` preserved; range processing + SVG fallback preserved.
8. Storefront now consumes `Tooba.Media.Endpoints.Admin.MediaAssetServing`.
9. Admin certification untouched: `Host/Tooba.Host/Admin` = 15 nested files, 0 top-level,
   `HostAdminCanonicalCertificationGuardTests.cs` present.
10. `Tooba.Media.Endpoints.csproj` is grouped in `Tooba.slnx`.
