# TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1 — trusted edition/tenant authorization matrix

Real adapter under test: `Tooba.Shipping.Endpoints.Admin.ShippingAdminAuthorizer` (constructed directly with the
production constructor: `IAdminPanelAccess`, `IAuthorizationService`, `ICurrentEdition`, `ICurrentTenant`).
Fake only the platform seams; the module code path is the production one.

Order of operations in the adapter: `IAdminPanelAccess` authentication/panel gate → context resolution →
`IAuthorizationService.CanAsync`. Context resolution never parses the request and never falls back to a literal.

| # | Edition context (`ICurrentEdition.Current`) | Tenant context (`ICurrentTenant.Current`) | Decision | Expected adapter outcome | Outgoing `AuthorizationCallContext` | Reaches `CanAsync` |
|---|---|---|---|---|---|---|
| 1 | `SingleStore` | Active, `store-alpha` | Allow | actor returned | `SingleStore` / `store-alpha` | yes |
| 2 | `SingleStore` | Active, `"  store-alpha  "` | Allow | actor returned | `SingleStore` / `store-alpha` (trimmed, not rewritten) | yes |
| 3 | `Marketplace` | none | Allow | actor returned | `Marketplace` / `null` | yes |
| 4 | unresolved (`null`) | Active, `store-alpha` | Allow (never used) | `admin.authorization.unavailable` | — | **no** |
| 5 | `Unset` | Active, `store-alpha` | Allow (never used) | `admin.authorization.unavailable` | — | **no** |
| 6 | unsupported (`(ToobaEdition)99`) | Active, `store-alpha` | Allow (never used) | `admin.authorization.unavailable` | — | **no** |
| 7 | `SingleStore` | none | Allow (never used) | `admin.authorization.unavailable` | — | **no** |
| 8 | `SingleStore` | Active, `""` | Allow (never used) | `admin.authorization.unavailable` | — | **no** |
| 9 | `SingleStore` | Active, `"   "` | Allow (never used) | `admin.authorization.unavailable` | — | **no** |
| 10 | `SingleStore` | `Disabled` | Allow (never used) | `admin.authorization.unavailable` | — | **no** |
| 11 | `SingleStore` | `Suspended` | Allow (never used) | `admin.authorization.unavailable` | — | **no** |
| 12 | `Marketplace` | Active, `store-alpha` (inconsistent) | Allow (never used) | `admin.authorization.unavailable` | — | **no** |
| 13 | `SingleStore` | Active, `store-alpha` | Deny | `admin.authorization.denied` | `SingleStore` / `store-alpha` | yes |
| 14 | `SingleStore` | Active, `store-alpha` | Unavailable | `admin.authorization.unavailable` (never Allow) | `SingleStore` / `store-alpha` | yes |
| 15 | `SingleStore` | Active, `store-alpha` | panel gate throws `admin.actor.missing` | propagated unchanged | — | **no** |
| 16 | `SingleStore` | Active, `store-alpha` | `CanAsync` throws `OperationCanceledException` | propagated unchanged (not swallowed) | `SingleStore` / `store-alpha` | yes |

## Request-supplied override resistance

Four header/query spoof attempts (`x-tooba-edition: Marketplace`, `x-tooba-tenant: attacker-tenant`,
`edition=Marketplace`, `tenantId=attacker-tenant`, plus a query string) are added to the `HttpContext`;
the outgoing check still carries the platform-resolved `SingleStore` / `store-alpha`. No header, cookie,
query or host value is a mode/tenant authority.

## Static proof

`ShippingAdminAuthorizer.cs` contains no `Request.Headers`, no `QueryString`, no `IConfiguration`, no
`"unknown"` tenant literal, and builds `CallContext = callContext` from the resolved value — it never
constructs an inline `AuthorizationCallContext` literal.

## What this repair does NOT establish

`ICurrentEdition`/`ICurrentTenant` prove the installation mode and the resolved tenant, not a merchant or
seller owner. Marketplace still has no seller-scoped shipping authoring and no per-seller data isolation;
the single-store canonical merchant owner has no lawful identity seam in the repository. Those remain the
deferred D1 business/data decisions.

## Test class

`Tooba.Shipping.Tests.Admin.ShippingAdminAuthorizerContextTests` — 20 cases (16 matrix rows across theories,
4 spoof attempts share one theory) plus the static proof.
