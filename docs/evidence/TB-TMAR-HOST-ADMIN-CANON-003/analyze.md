# TB-TMAR-HOST-ADMIN-CANON-003 — Analyze

## Goal

Fix ONLY the Support and Wallet Host Admin authorizers so capability enforcement is explicit,
stable and **fail-closed**, while preserving existing endpoint contracts.

## Defect (pre-state)

Both `HostSupportAdminAuthorizer` and `HostWalletAdminAuthorizer`:

1. resolved `CurrentAuthenticatedSession` / `ICurrentTenant` / `IAuthorizationGuard` /
   `IHostEnvironment` from `HttpContext.RequestServices`;
2. called the static `AdminPanelAccess.RequireAuthorizedAsync(...)` directly;
3. evaluated the module capability with `IAuthorizationService.CanAsync`;
4. **failed open** on `AuthorizationDecisionKind.Unavailable`:

```csharp
if (decision.Kind == AuthorizationDecisionKind.Unavailable)
    return;   // silences the gate
```

The comment justified this as "until capability tuples become stable". That is a security
hole: when the authorization engine is unreachable, any panel-visible operator passes the
capability gate.

## Target shape

```csharp
public sealed class HostSupportAdminAuthorizer(
    IAdminPanelAccess adminAccess,
    IAuthorizationService authz,
    ICurrentTenant tenant) : ISupportAdminAuthorizer
{
    public async Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, string permissionId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

        var actor = await adminAccess.RequireAuthorizedAsync(httpContext.Request, ct);
        await EnsureAdminCapabilityAsync(actor, permissionId, ct);
        return actor;
    }
}
```

Capability outcomes:

| Decision kind | Result |
| --- | --- |
| `Allow` | return the actor (endpoint contract preserved) |
| `Deny` | 403 + module stable denial code |
| `Unavailable` | 503 + module stable unavailability code |

## Error codes (audit first)

Existing stable denial codes were preserved verbatim:

- Support: `SupportErrorCodes.AdminAuthorizationDenied` = `admin.authorization.denied`
- Wallet: `WalletErrorCodes.AdminAuthorizationDenied` = `admin.authorization.denied`

No stable 503 code existed in the Support/Wallet catalogs and no module 503 code with the
right ownership was reusable (`admin.authorization.unavailable` is only a litera in
`AdminPanelAccess`, which is explicitly out of scope). Catalog ownership for a code that
Support/Wallet consume must live in the owning module's endpoint catalog —
`SupportErrorCatalogContributor` / `WalletErrorCatalogContributor` already own their module
503 codes. Therefore the smallest appropriate stable codes were added in the owning modules:

- `SupportErrorCodes.AuthorizationUnavailable` = `support.authorization.unavailable`
- `WalletErrorCodes.AuthorizationUnavailable` = `wallet.authorization.unavailable`

Both are registered as `ErrorClassification.Platform`, `503`, in their module contributor.
No `ex.Message` classification, no localized-text classification, no 403 code reused for 503.

## Edition / tenant semantics

The capability `CallContext` was preserved exactly: `Edition = ToobaEdition.SingleStore`,
`TenantId = tenant.Current?.TenantId.Value ?? "unknown"`. No Marketplace capability semantics
were invented. Limitation (unchanged from before this task): for a Development/Marketplace
synthetic-tenant admin the capability context still reports `unknown` tenant, because the
platform seam does not expose a Marketplace capability tenant; broadening that requires a
separate design task and was not attempted here.

## Interface contract text

`ISupportAdminAuthorizer` / `IWalletAdminAuthorizer` XML docs previously advertised
"Preserves Unavailable fail-open compatibility". That sentence documented the defect and was
updated to "fails closed (503)".

## File scope

Host/Admin count before = 15, after = 15.
Out-of-scope (untouched): Order authorizers, `AdminPanelAccess`, `HostAdminPanelAccess`,
`AdminPanelComposer`, foldering, DevActor, module business logic.
