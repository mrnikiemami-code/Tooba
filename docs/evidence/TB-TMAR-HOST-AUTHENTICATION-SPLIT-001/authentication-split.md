# TB-TMAR-HOST-AUTHENTICATION-SPLIT-001 — behavior-preserving split of the Host Authentication folder

Task: `TB-TMAR-HOST-AUTHENTICATION-SPLIT-001`
Parent: `TB-TMAR-HOST-AUTHENTICATION-INVENTORY-001` at `67f5ddbf`
Mode: BACKEND_ONLY. Option A of the Authentication decision (retain as Host global auth; bounded internal split only).
Change class: **structural split only — zero ownership move, zero behavior change, zero route change, zero guard weakening.**

## 1. Why the split

`AuthenticationHttpBoundary.cs` was **824 physical LOC** and was carried in
`src/backend/Host/Tooba.Host.Tests/Baselines/tmar-source-size-baseline.json` as `OVERSIZED_LEGACY`
(shrink-only under ARCH-SIZE-002). It packed eight responsibilities:

1. `CurrentAuthenticatedSession` (per-request principal)
2. `IAuthenticationThrottleSeam` (seam interface)
3. `SessionAuthenticationMiddleware` (global auth middleware)
4. `AuthenticationHttpModels` (14 wire DTO/records)
5. `AuthenticationEndpointMapper` (13 `/v1/auth` routes)
6. `AuthenticationEndpointMapper`'s private helpers
7. `RejectUntrustedTenant` / `RejectIfThrottled` (cross-cutting guards)
8. `AuthProblem` (ProblemDetails mapping)

This violated ARCH-MODULE-FILE-001 (one cohesive responsibility per file) and kept Host above the
soft watch line. Option A accepts the ownership as-is and therefore only decomposes the file.

## 2. Resulting tree (same folder, same namespace `Tooba.Host`)

| File | LOC | Responsibility |
| --- | --- | --- |
| `Authentication/AuthenticationHttpBoundary.cs` | **447** (was 824) | `AuthenticationEndpointMapper` + 13 route handlers + private helpers |
| `Authentication/AuthenticationHttpModels.cs` | 294 | wire DTOs/records + `AuthenticationHttpProblem` guard/ProblemDetails mapping |
| `Authentication/SessionAuthenticationMiddleware.cs` | 59 | global bearer/cookie session resolution middleware |
| `Authentication/CurrentAuthenticatedSession.cs` | 46 | per-request principal state |
| `Authentication/IAuthenticationThrottleSeam.cs` | 13 | throttle seam interface |
| `Authentication/AuthenticationInstrumentation.cs` | 23 | unchanged (pre-existing, retained) |
| `Authentication/AuthenticationRateLimitThrottleSeam.cs` | 54 | unchanged (pre-existing, retained) |
| `Authentication/HostCurrentAuthenticatedUser.cs` | 14 | unchanged (pre-existing, retained) |

Every extracted type keeps its exact `internal`/`public` visibility and its `Tooba.Host` namespace, so
all existing Host consumers and the two path-pinned guards keep compiling and passing unchanged.
The file that the guards read by path (`AuthenticationHttpBoundary.cs`) still exists and still contains
every asserted token (`/otp-login/request`, `/otp-login/complete`, `ICustomerProfileDirectory`,
`IIdentityContactLookup`, `StorefrontAccountIdentity.CanonicalName`). **No guard or test was edited.**

## 3. Behavior-preservation parity proof

Compared the pre-split file (`git show HEAD:…`) against the post-split union:

| Behavior surface | Original | After split | Verdict |
| --- | --- | --- | --- |
| Route registrations `group.Map(Post\|Get)` | 13 | 13 | identical (Compare-Object = 0) |
| Status codes | 201, 400, 401, 409, 429 | 201, 409 (boundary) + 400, 401, 429 (problem mapper) | identical union |
| `identity.*` error codes | 12 | 12 | identical |
| `identity.*` log event names | 14 (incl. `identity.rate_limited`) | 14 across files | identical |
| Throttle operation names | 8 | 8 | identical |
| `/health`, `/ready` bypass | present | present | identical |
| `tooba_session` cookie + `Bearer ` header parsing | present | present | identical |
| Type names | 7 | 7 | identical |

No route, status code, error code, log event, throttle operation, cookie/header name, or response
shape changed. `Program.cs` needed **no edit** — every type is still `Tooba.Host`-scoped.

## 4. Baseline reconciliation (rule-conformant, no weakening)

The freeze list only accepts files classified `OVERSIZED_LEGACY` / `CRITICAL_GOD_FILE`
(`TmarSourceSizeAndInfraAppTests.Source_size_inventory_evidence_exists_and_matches_scan_count`
asserts `baselinePaths.Count == scanned.Count(OVERSIZED_LEGACY or CRITICAL_GOD_FILE)`).
The split file is now `NORMAL` (447 ≤ 500), so it must leave the freeze list:

- `tmar-source-size-baseline.json`: entry for `AuthenticationHttpBoundary.cs` **removed**
  (54 → 53 entries).
- **No baseline `loc` was raised. No threshold changed. No other entry touched.**
- `BASELINE_ENTRY_MISSING_FILE` for this path is now correct-by-construction: the file is
  no longer a frozen oversized file.
- `docs/evidence/TB-TMAR-BOUNDARY-V1-R1/source-size-inventory.json/.md` were deliberately **not**
  re-synthesized: they are a stale repository-wide snapshot (915 other violations exist today)
  and hand-editing them without a real re-scan of all 1848 files would be fabrication.

## 5. Focused validation

| Check | Result |
| --- | --- |
| `dotnet build Host/Tooba.Host/Tooba.Host.csproj` | **succeeded, 0 errors** |
| Auth-focused guards (24 tests: CheckoutIdentityContract, StorefrontAccountIdentity, HostFolderStructure, TmarDurableGuard, HostModuleEndpointOwnership, TmarCompleteReferenceStructureGate) | **24/24 pass** |
| Auth-consumer guards (AdminPanel/SellerPanel/AddressBook/ContentPermission/Story/Reviews/StorefrontPaymentActor) | 65 pass, 1 pre-existing failure (see §6) |

## 6. Pre-existing failures NOT caused by this change (proven at clean HEAD)

Verified in a detached `git worktree` at `67f5ddbf` (same failure counts before any of my edits):

1. `TmarSourceSizeAndInfraAppTests.Hand_written_source_size_does_not_expand_beyond_baseline` and
   `Source_size_inventory_evidence_exists_and_matches_scan_count` — **red at HEAD** (`Failed: 3`,
   `Total: 6`). Root causes are unrelated files/folders (AccessControl, Catalog, Fulfillment, Order,
   Settlement, Admin composers, Cart) plus the stale inventory evidence. This is a pre-existing
   repository condition and out of scope for the Authentication folder.
2. `TmarSourceSizeAndInfraAppTests.Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline`
   — **red at HEAD**, identical edge list before and after (`Tooba.Order.Infrastructure ->
   Tooba.AccessControl.Application`, `-> Tooba.Fulfillment.Application` missing from baseline;
   16 stale baseline entries). No `.csproj` was changed by this task.
3. `ReviewsFoundationTests.Seller_host_list_exists_without_seller_response_or_moderation_routes` —
   `ReviewEndpoints.cs` is untouched since `178d888e` and no longer contains the asserted literals.
   Not caused by this task.

None of these are Authentication-scoped; all were left untouched rather than silently "fixed".

## 7. Verdict

`COMPLETE_REFERENCE_PATTERN`-neutral: this task performs **no module certification**. It is a
Host-local, behavior-preserving decomposition that removes one `OVERSIZED_LEGACY` entry and
satisfies ARCH-MODULE-FILE-001 for the Authentication folder.

- Production ownership change: **NONE**
- Route/behavior change: **NONE**
- Guard weakened: **NONE**
- New file over 800 LOC: **NONE**
- Cross-module coupling/joins introduced: **NONE**
