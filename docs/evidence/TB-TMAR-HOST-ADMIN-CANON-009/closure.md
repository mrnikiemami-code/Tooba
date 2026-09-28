# TB-TMAR-HOST-ADMIN-CANON-009 — Closure

## Summary

Removed the last two foreign `*.Application` dependencies from Host/Admin while preserving every
stable admin-auth machine code, HTTP status, descriptor classification, and fail-closed behavior.

## Deliverables

### New canonical seam-owned surface

* `src/backend/Modules/Support/Tooba.Support.Endpoints/Admin/SupportAdminAuthorizationCodes.cs`
  * `AdminAuthorizationDenied = "admin.authorization.denied"` (shared cross-cutting code, descriptor
    authority remains `FoundationErrorCodes`)
  * `AuthorizationUnavailable = "support.authorization.unavailable"`
* `src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Admin/WalletAdminAuthorizationCodes.cs`
  * `AdminAuthorizationDenied = "admin.authorization.denied"` (same shared code, same single
    descriptor authority)
  * `AuthorizationUnavailable = "wallet.authorization.unavailable"`

### Host authorizers

* `Admin/Access/Authorizers/HostSupportAdminAuthorizer.cs` — `Tooba.Support.Application.Errors`
  removed; 503 uses `SupportAdminAuthorizationCodes.AuthorizationUnavailable`, 403 uses
  `FoundationErrorCodes.AdminAuthorizationDenied`.
* `Admin/Access/Authorizers/HostWalletAdminAuthorizer.cs` — `Tooba.Wallet.Application.Errors`
  removed; 503 uses `WalletAdminAuthorizationCodes.AuthorizationUnavailable`, 403 uses
  `FoundationErrorCodes.AdminAuthorizationDenied`.

### Duplicate-authority cleanup

* `Tooba.Support.Application.Errors.SupportErrorCodes.AdminAuthorizationDenied` — removed
  (unreferenced; `FoundationErrorCodes` is the single authority). `AuthorizationUnavailable` kept as
  the module's canonical code value with an explicit seam-authority pointer.
* `Tooba.Wallet.Application.Errors.WalletErrorCodes.AdminAuthorizationDenied` — removed (same
  rationale). `AuthorizationUnavailable` kept with the same pointer. All unrelated business codes
  unchanged.

### Descriptor ownership

* `SupportErrorCatalogContributor` / `WalletErrorCatalogContributor` now reference the seam-owned
  constants; classification remains `Platform` / `503`; exactly one descriptor per unavailable code;
  zero `admin.authorization.denied` descriptors (still owned by the Foundation catalog).

### Guards and tests

* `Host/Tooba.Host.Tests/Architecture/HostAdminCanon009GuardTests.cs` — new durable guard (5 facts).
* `Host/Tooba.Host.Tests/Architecture/HostAdminCanon003GuardTests.cs` — updated to the seam-owned
  symbols plus explicit zero-foreign-Application assertions.
* `SupportArchitectureGuardTests` / `WalletArchitectureGuardTests` — host-admin path + symbol
  assertions aligned with the CANON-008 structure and CANON-009 seam surface.

## Required state

| Field | Value |
| --- | --- |
| Canon008-Preservation-State | PRESERVED |
| Host-Admin-File-Count-Before | 15 |
| Host-Admin-File-Count-After | 15 |
| Support-ForeignApplication-State | ZERO |
| Wallet-ForeignApplication-State | ZERO |
| Support-ErrorCode-Authority-State | SUPPORT_ENDPOINTS_ADMIN_SEAM |
| Wallet-ErrorCode-Authority-State | WALLET_ENDPOINTS_ADMIN_SEAM |
| Support-Denial-Code-State | admin.authorization.denied (UNCHANGED) |
| Support-Unavailable-Code-State | support.authorization.unavailable (UNCHANGED) |
| Wallet-Denial-Code-State | admin.authorization.denied (UNCHANGED) |
| Wallet-Unavailable-Code-State | wallet.authorization.unavailable (UNCHANGED) |
| Unavailable-Descriptor-State | PLATFORM_503_PRESERVED |
| Duplicate-Descriptor-Introduced-State | NONE |
| FailClosed-Behavior-State | PRESERVED |
| Focused-Validation | 26/26 host CANON guards + 3/3 Support + 6/6 Wallet PASS |
| SoT-State | hostAdminCanon009 appended |

## Out of scope / not performed

No final Host/Admin certification, no capability-policy change, no Order authorizer change, no
Host/Admin structural change, no unrelated baseline repair, no frontend/schema work, no Support or
Wallet module redesign.

## Workflow stop

`USER_REVIEW_HOST_ADMIN_CANON_009` — awaiting Architect review. STOP after result submission.
