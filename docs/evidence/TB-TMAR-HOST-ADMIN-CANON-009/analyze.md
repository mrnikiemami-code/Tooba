# TB-TMAR-HOST-ADMIN-CANON-009 — Analyze

## Concern (single)

Remove the two remaining foreign `*.Application` dependencies from the Host/Admin authorizers
without changing any auth behavior, machine code, or error-catalog classification.

| File | Offending import | Symbols consumed |
| --- | --- | --- |
| `Admin/Access/Authorizers/HostSupportAdminAuthorizer.cs` | `Tooba.Support.Application.Errors` | `SupportErrorCodes.AuthorizationUnavailable`, `SupportErrorCodes.AdminAuthorizationDenied` |
| `Admin/Access/Authorizers/HostWalletAdminAuthorizer.cs` | `Tooba.Wallet.Application.Errors` | `WalletErrorCodes.AuthorizationUnavailable`, `WalletErrorCodes.AdminAuthorizationDenied` |

## Audit findings

### Machine codes actually emitted by the two Host authorizers

| Path | Status | Machine code |
| --- | --- | --- |
| Support denial | 403 | `admin.authorization.denied` |
| Support unavailable | 503 | `support.authorization.unavailable` |
| Wallet denial | 403 | `admin.authorization.denied` |
| Wallet unavailable | 503 | `wallet.authorization.unavailable` |

### Existing authorities

* `admin.authorization.denied` — **already has a single canonical owner**:
  `Tooba.BuildingBlocks.Presentation.Errors.FoundationErrorCodes.AdminAuthorizationDenied`.
  `SupportErrorCatalogContributor` and `WalletErrorCatalogContributor` both carry an explicit comment
  that this is a shared cross-cutting code owned by `FoundationErrorCatalogContributor`, and they do
  **not** register a descriptor for it. The constants in `SupportErrorCodes` / `WalletErrorCodes`
  were therefore redundant duplicate authorities, not the registration source.
* `support.authorization.unavailable` — owned by Support; registered once in
  `SupportErrorCatalogContributor` as `Platform` / `503`.
* `wallet.authorization.unavailable` — owned by Wallet; registered once in
  `WalletErrorCatalogContributor` as `Platform` / `503`.

### Consumers of the two admin-auth constants

Full-repository scan (`rg`, excluding `bin`/`obj`):

* `SupportErrorCodes.AdminAuthorizationDenied` — only `HostSupportAdminAuthorizer` + CANON-003 guard.
* `WalletErrorCodes.AdminAuthorizationDenied` — only `HostWalletAdminAuthorizer` + CANON-003 guard.
* `SupportErrorCodes.AuthorizationUnavailable` — only `HostSupportAdminAuthorizer`, the Support
  catalog contributor, CANON-003 guard, and `SupportArchitectureGuardTests`.
* `WalletErrorCodes.AuthorizationUnavailable` — only `HostWalletAdminAuthorizer`, the Wallet
  catalog contributor, CANON-003 guard, and `WalletArchitectureGuardTests`.
* `SupportErrorCodes.SellerAuthorizationDenied` (Host seller authorizer) — unrelated, left untouched.

No other consumer exists anywhere in the repository, so relocating these three symbols is safe and
bounded.

### Boundary precedent

`Tooba.Order.Endpoints.Errors.OrderErrorCodes`
(`order.operation.denied`, `order.authorization.unavailable`) is the established pattern: the
Host-facing admin auth code surface lives in the module's **Endpoints** project, beside the
authorizer seam. `ISupportAdminAuthorizer` / `IWalletAdminAuthorizer` already live in
`Tooba.Support.Endpoints.Admin` / `Tooba.Wallet.Endpoints.Admin`, and `Host/Tooba.Host.csproj`
already references both Endpoints projects (and deliberately not the Application projects).

## Chosen smallest lawful owner

Colocate the module-specific admin auth code surface with the authorizer seam:

* `Tooba.Support.Endpoints.Admin.SupportAdminAuthorizationCodes`
* `Tooba.Wallet.Endpoints.Admin.WalletAdminAuthorizationCodes`

Each exposes:

* `AuthorizationUnavailable` — the module-specific 503 code.
* `AdminAuthorizationDenied` — a seam-local re-statement of the shared cross-cutting 403 code, so the
  module's auth surface is self-describing at the boundary. The **descriptor authority stays single**
  (`FoundationErrorCodes`); `SupportErrorCatalogContributor` / `WalletErrorCatalogContributor` still
  register no descriptor for it, so **no duplicate descriptor is introduced**.

The old Application constants are reduced to their canonical single definition (a JSDoc pointer to
the seam authority), and the now-unreferenced `AdminAuthorizationDenied` constant is deleted from
both Application error-code classes.

## Out of scope (untouched)

capability policy, fail-closed semantics, routes/interfaces/signatures, the Order authorizer,
Host/Admin physical structure, unrelated Support/Wallet business error codes, frontend/schema,
module redesign.
