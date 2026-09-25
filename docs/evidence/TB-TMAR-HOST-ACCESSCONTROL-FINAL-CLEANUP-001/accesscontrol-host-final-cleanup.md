# TB-TMAR-HOST-ACCESSCONTROL-FINAL-CLEANUP-001 — Host AccessControl final cleanup evidence

Parent: `TB-TMAR-ACCESSCONTROL-CATALOG-BOUNDARY-REPAIR-001` (commit `6dd61ab5`)
Track: `HOST_FIRST_ACCESSCONTROL`
Status: PASS (backend only)

## 1. Host files before → after

| File | Before | After |
|------|--------|-------|
| `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs` | present | DELETED |
| `src/backend/Host/Tooba.Host/AccessControl/AccessControlDevelopmentSeed.cs` | present | DELETED |
| `src/backend/Host/Tooba.Host/AccessControl/AccessControlDemoSnapshot.cs` | present | DELETED |

`Host-AccessControl-File-Count` = **0**.
`src/backend/Host/Tooba.Host/AccessControl` → **folder absent**.

## 2. Program.cs residue removed

Removed:
- `using Tooba.Host.AccessControl;` (now dead after route mapping removal).
- The Development legacy bootstrap try/catch:
  ```csharp
  try { await AccessControlDevelopmentSeed.ApplyAsync(app.Services); }
  catch (Exception ex) { app.Logger.LogError(ex, "AccessControlDevelopmentSeed failed; ..."); }
  ```
  It lived inside the `app.Environment.IsDevelopment() && catalogDemoOptions.RunLegacyBootstraps`
  branch; sibling bootstraps (`ProductWorkspaceDevelopmentBootstrap`,
  `StorefrontDemoCatalogBootstrap`, `CatalogAttributeSchemaDevelopmentBootstrap`, …) are untouched.
- `app.MapAccessControlEndpoints();`

Preserved:
- `using Tooba.AccessControl.Endpoints;`
- `app.MapAccessControlModuleEndpoints();` — the live module mapping.

## 3. Module map preserved

`Program.cs` now contains exactly one AccessControl mapping call:
`app.MapAccessControlModuleEndpoints();`.
No module route was altered; no module route was deleted.

## 4. demo-preview / seed / snapshot retirement decision

`GET /v1/admin/access-control/demo-preview` and its Development-only
snapshot/seed infrastructure were intentionally **retired**, not rehomed.

Explicit Development-only proof (pre-deletion behavior):
- `AdminDemoPreviewAsync` returned `Results.NotFound()` whenever `!env.IsDevelopment()`.
- The preview JSON came solely from `AccessControlDemoSnapshot.Current`.
- `AccessControlDemoSnapshot` was populated solely by `AccessControlDevelopmentSeed`.
- `AccessControlDevelopmentSeed.ApplyAsync` was invoked solely from the
  `app.Environment.IsDevelopment()` + `CatalogDemoOptions.RunLegacyBootstraps` branch.

Therefore the deleted surface was never production AccessControl authority, and no
production HTTP contract was lost. No demo-preview re-creation, no seed rehoming into
`AccessControl.Application`/`Infrastructure`, and no generic demo abstraction was added.

## 5. Zero audit (production)

`rg` over `src/backend` (excluding `obj/bin`):

| Symbol | Production occurrences |
|--------|------------------------|
| `namespace Tooba.Host.AccessControl` | ZERO |
| `MapAccessControlEndpoints` | ZERO |
| `AccessControlDevelopmentSeed` | ZERO |
| `AccessControlDemoSnapshot` | ZERO |
| `AccessControlDemoContext` | ZERO |
| Host `AccessControl` directory | absent (ZERO files) |

`Program.cs` → `app.MapAccessControlModuleEndpoints();` present exactly once.

## 6. Shared platform seams preserved (not AccessControl Host ownership)

These remain and are generic platform security seams used across modules:
- `IAdminPanelAccess`
- `ISellerPanelAccess`
- `IPlatformEffectiveAccessReader` (registered to AccessControl.Infrastructure
  `PlatformEffectiveAccessReader` in `Program.cs`; a module-owned implementation consumed
  by Host composition — a composition seam, not Host AccessControl ownership).

They were not removed and are classified as shared platform seams.

## 7. Route ownership

No non-demo module route ownership change.
All AccessControl HTTP is module-owned:
- Admin/Seller role/assignment/effective/user-search/scope-resources → `AccessControlAdminEndpoints` / `AccessControlSellerEndpoints`.
- Only Host-specific AccessControl mapping (`MapAccessControlEndpoints`) was removed.

## 8. Focused builds/tests

| Command | Result |
|---------|--------|
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build .../Tooba.AccessControl.Endpoints.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet test Tooba.Host.Tests --filter "…AccessControl_module_boundary_static_checks|…Host_Cart_naming_files_are_explicitly_allowlisted"` | Passed 2, Failed 0 |

Test maintenance performed (only stale assertions directly caused by this cleanup):
- `AccessControlFoundationTests.AccessControl_module_boundary_static_checks` — replaced
  Host file-text assertions with: Host AccessControl folder must not exist; every Host
  `*.cs` must contain no `namespace Tooba.Host.AccessControl`,
  `AccessControlDevelopmentSeed`, or `AccessControlDemoSnapshot`; `Program.cs` must contain
  `MapAccessControlModuleEndpoints()` and not `MapAccessControlEndpoints()`.
- `SettingsFoundationTests.Mobile_operator_role_seed_does_not_grant_settings_manage` —
  removed (asserted only against the now-deleted Host seed file).
- `HostCartResidualGuardTests` allowlist — removed the
  `AccessControl/AccessControlDevelopmentSeed.cs` entry for the deleted file.

## 9. Host evacuation state

Host AccessControl evacuation = COMPLETE.
`src/backend/Host/Tooba.Host/AccessControl` = ZERO files.

## 10. Structure certification state

**NOT** certified in this task. AccessControl is **not** added to `certifiedModules`.
No `COMPLETE_REFERENCE_PATTERN`, no `ARCH-COMPLETE-002` STRUCTURE_CERTIFIED claim.
Recovery SoT was not broadly rewritten here.

Next recommended step: focused **AccessControl final certification + Recovery SoT closure**
(a dedicated task after Architect acceptance), which will update canonical recovery pointers.
