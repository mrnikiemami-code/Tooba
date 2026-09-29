# Host/Seller — Seller-R3 — Validation

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R3
**Environment:** local PowerShell + `git`, `dotnet`, `D:\Users\User\source\repos\SarvNewVer`
**Scope:** focused only. No solution-wide test run. No unrelated module suites.

## 1. Focused builds

| Project | Result |
| --- | --- |
| `Tooba.Party.Application` | PASS — 0 errors |
| `Tooba.Party.Infrastructure` | PASS — 0 errors |
| `Tooba.Party.Endpoints` | PASS — 0 errors |
| `Tooba.Host` | PASS — 0 warnings, 0 errors |

## 2. Focused tests

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj \
  --filter "HostSellerAmcR3GuardTests|HostSellerAmcR1GuardTests|HostSellerAmcR2GuardTests|SettingsFoundationTests"
```

Result: **Passed! Failed: 0, Passed: 31, Skipped: 4, Total: 35.**

The new durable guard `HostSellerAmcR3GuardTests` passes its full set:

| Guard fact | Proves |
| --- | --- |
| `Party_endpoints_own_both_seller_settings_routes_exactly_once` | both templates in Party; `ISender`/`ApiResponseFactory`/`IPartySellerAuthorizer`/`canManage` present; no `DbContext`/`IAccessControlDirectory`/`Results.Json`; exactly two mappings |
| `Party_module_registers_and_maps_the_seller_settings_surface_once` | single `PartySellerSettingsEndpoints.Map`; catalog/resource registration; `Program.cs` maps Party and no longer maps the Host settings endpoint |
| `Host_seller_settings_file_is_absent_and_folder_shrinks_to_four_files` | `SellerSettingsEndpoints.cs` absent; Host/Seller = exactly 4 files |
| `Host_seller_owns_exactly_two_routes_and_zero_settings_route` | Host/Seller = `/dashboard` + `/dev-contexts` only; no `settings`; exactly two mappings |
| `Host_seller_has_zero_settings_layer_leakage` | full-folder line scan for `AccessControl.Application`/`Domain`, `IAccessControlDirectory`, `SellerSettingsEndpoints`, `OrganizationProfileWriteRequest`, `EnsureSellerCapabilityAsync` = ZERO |
| `Party_owns_the_seller_settings_cqrs_and_validator_classification` | query + command + models present; write validator present; read validator absent; adapter has no EF/`PartyDbContext`; module registration |
| `Seller_settings_error_codes_keep_parity_and_no_duplicate_descriptor_is_registered` | `seller.settings.missing`/`rejected` Party-owned; `seller.authorization.denied` NOT re-registered by Party |
| `Host_party_seller_authorizer_is_a_thin_neutral_security_adapter_inside_the_r1a_boundary` | adapter in `Host/Security/Seller`; `ISellerPanelAccess` + `IPlatformEffectiveAccessReader`; no `RequestServices`/`DbContext`/Party layer; `Program.cs` registration |
| `Behavior_parity_and_recovery_invariants_are_preserved` | both routes, verb count, group path, R1A gate codes intact; R2 Catalog invariants intact; no sink-folder regression |

`HostSellerAmcR1GuardTests` (R1A boundary) and `HostSellerAmcR2GuardTests` (R2 Catalog evacuation)
still pass — neither prior boundary was weakened. `SettingsFoundationTests` passes after being
re-pointed to the Party surface + `HostPartySellerAuthorizer`.

## 3. Recovery durable guard

`TmarDurableGuardTests` is the durable recovery-SoT freshness guard. Its current-checkpoint
assertions were advanced in place to the R3 checkpoint (`lastAcceptedTask` = R3, top-level and
`currentHostEvacuation` `workflowStop`/`nextTask` = `USER_REVIEW_HOST_SELLER_AMC_001_R3`,
`currentTask` = R3), the historical Seller R2/R1A/R1B and Development assertions were preserved,
and the R3 stop uniqueness counts are asserted. The two SHA/pointer facts (`implementationCommit`,
`sotStamp` exist on `main` and are ancestors of HEAD) are satisfied after the R3 commit/push; the
guard reads those SHAs from `tmar-current-state.json`.

## 4. Structural verification

| Command | Result |
| --- | --- |
| `dir src/backend/Host/Tooba.Host/Seller` | exactly 4 business files |
| `dirname HostPartySellerAuthorizer.cs` | `Host/Security/Seller` (inside the R1A boundary) |
| `Select-String -Path Host/Seller/*.cs -Pattern IAccessControlDirectory` | 0 matches |
| `Select-String -Path Host/Seller/*.cs -Pattern 'Tooba.AccessControl.(Application|Domain)'` | 0 matches |
| `Select-String -Path Party.Endpoints/**/*.cs -Pattern DbContext` | 0 matches |
| `git diff --name-only -- src/frontend` | empty — frontend unchanged |

## 5. Scope discipline

No frontend change. No schema/migration change. No reset/clean/rebase/force-push. Seller-R4 not
started. User work preserved (untracked user artifacts and pre-existing modifications left
untouched).
