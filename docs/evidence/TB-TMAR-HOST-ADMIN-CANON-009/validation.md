# TB-TMAR-HOST-ADMIN-CANON-009 — Validation

## Build

| Command | Result |
| --- | --- |
| `dotnet build Host/Tooba.Host/Tooba.Host.csproj` | Build succeeded — 0 errors, 11 pre-existing warnings |
| `dotnet build Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj` | Build succeeded — 0 errors |

## Focused validation

| Command | Result |
| --- | --- |
| `dotnet test Host/Tooba.Host.Tests --filter "HostAdminCanon009GuardTests\|HostAdminCanon008GuardTests\|HostAdminCanon003GuardTests"` | **Passed — 26/26, 0 failed** |
| `dotnet test Modules/Support/Tooba.Support.Tests --filter "SupportArchitectureGuardTests"` | **Passed — 3/3, 0 failed** |
| `dotnet test Modules/Wallet/Tooba.Wallet.Tests --filter "WalletArchitectureGuardTests"` | **Passed — 6/6, 0 failed** |

CANON-009 guard facts (`HostAdminCanon009GuardTests`):

1. `Host_admin_authorizers_have_zero_foreign_application_reference`
2. `Stable_admin_auth_codes_are_unchanged`
3. `Admin_authorizers_keep_403_and_fail_closed_503_statuses`
4. `Unavailable_descriptors_remain_platform_503_and_exactly_once`
5. `Admin_recursive_file_count_is_15_and_canon008_structure_is_preserved`

## Evidence of the boundary fix

Host authorizers after the change (zero foreign `.Application`):

```text
HostSupportAdminAuthorizer.cs
  using Tooba.BuildingBlocks;
  using Tooba.BuildingBlocks.Presentation.Errors;
  using Tooba.BuildingBlocks.Security;
  using Tooba.Support.Endpoints.Admin;
  ... 503 -> SupportAdminAuthorizationCodes.AuthorizationUnavailable
  ... 403 -> FoundationErrorCodes.AdminAuthorizationDenied

HostWalletAdminAuthorizer.cs
  using Tooba.BuildingBlocks;
  using Tooba.BuildingBlocks.Presentation.Errors;
  using Tooba.BuildingBlocks.Security;
  using Tooba.Wallet.Endpoints.Admin;
  ... 503 -> WalletAdminAuthorizationCodes.AuthorizationUnavailable
  ... 403 -> FoundationErrorCodes.AdminAuthorizationDenied
```

`rg "SupportErrorCodes|WalletErrorCodes" src/backend/Host` now returns only
`HostSupportSellerAuthorizer.cs` (unrelated seller code, out of scope).

## Pre-existing drift observed (NOT introduced by CANON-009)

The two module architecture guard facts `Support_endpoints_cqrs_and_host_ownership_are_enforced` and
`Wallet_endpoints_cqrs_and_host_ownership_are_enforced` still asserted the **flat** pre-CANON-008
paths (`Admin/HostSupportAdminAuthorizer.cs`, `Admin/HostWalletAdminAuthorizer.cs`), which were
relocated by the Architect-accepted parent CANON-008. Confirmed pre-existing at the parent commit:

```text
git cat-file -e 33d6cef...:src/backend/Host/Tooba.Host/Admin/HostSupportAdminAuthorizer.cs
fatal: path does not exist  (exit 128)
```

These two assertions were corrected to the canonical CANON-008 paths, plus the module-specific
symbol assertions were updated to the new Endpoints-owned surface. No unrelated baseline suite was
touched.

## Interrupted first attempt

The first focused run failed 2 of 26 because the new guard matched source text against its own
descriptive comment and against a re-formatted `throw`. Both assertions were tightened to inspect
only descriptor lines / path-agnostic tokens, then rebuilt and rerun: 26/26 PASS. One repair
iteration, within budget.
