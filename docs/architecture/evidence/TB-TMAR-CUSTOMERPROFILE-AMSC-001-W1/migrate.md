# TB-TMAR-CUSTOMERPROFILE-AMSC-001-W1 — Migrate

## Mode

`ARCHITECT_DIRECT_AMSC` — Migrate. Behavior-preserving canonicalization; no route, DTO shape, status
code, schema or migration change.

Baseline: `HEAD == origin/main == 39a5de09` (W0).

## What changed

### 1. Single stable error-code owner (new)

`Tooba.CustomerProfile.Contracts/Errors/CustomerProfileErrorCodes.cs` — the module's single catalog
owner:

| Code | Meaning |
|---|---|
| `customer.session.required` | shared cross-cutting session code, **consumed** (Foundation owns the descriptor) |
| `customer.profile.actor_required` | server-trusted actor identity is not usable |
| `customer.profile.display_name_invalid` | display name outside 3..128 |
| `customer.profile.first_name_invalid` | first name > 64 |
| `customer.profile.last_name_invalid` | last name > 64 |
| `customer.profile.birth_date_invalid` | birth date > 32 |
| `customer.profile.bio_invalid` | bio > 200 |

### 2. Typed faults replace hard-coded Persian text + untyped exceptions

Before, `Domain/CustomerProfile.cs` and `Infrastructure/CustomerProfileDirectory.cs` threw
`InvalidOperationException` carrying 6 distinct Persian literals. Now both throw
`ContractOperationException(<stable code>)`, so a machine-stable code reaches the presentation layer
instead of prose. Zero hard-coded user-facing fault text remains in Domain/Infrastructure.

`Tooba.CustomerProfile.Domain.csproj` gained a reference to its **own** `Tooba.CustomerProfile.Contracts`
(AddressBook/Cart/Content precedent — self-module layering, not cross-module coupling).

### 3. Typed-fault → `Result` composition seam (new)

`Tooba.CustomerProfile.Application/Composition/CustomerProfileOperation.cs` — maps
`ContractOperationException` to `Result`/`Result<T>` failures by stable code; unknown exceptions still
propagate untouched to the global exception boundary. All three handlers now route through it, and the
two actor-only reads gained a defensive `ActorUserId != Guid.Empty` guard that returns
`customer.profile.actor_required` instead of letting `Guid.Empty` reach the directory (which previously
threw an untyped exception).

`UpsertCustomerProfileCommandHandler` persists through the seam and then delegates to the existing
profile-page query, so persistence faults and composition faults both map to stable codes.

### 4. Module error catalog + resources (new)

- `Endpoints/Errors/CustomerProfileErrorCatalogContributor.cs` — 6 `customer.profile.*` descriptors
  (`Validation` / `400`). `customer.session.required` is deliberately **not** re-registered; the
  Foundation contributor remains its single descriptor owner.
- `Endpoints/Resources/CustomerProfileErrorResources.cs` — `CustomerProfileErrorResourceSet` owning only
  the `customer.profile.` prefix, plus the `ResourceManager` marker.
- `Endpoints/Resources/CustomerProfileErrors.resx` + `CustomerProfileErrors.fa.resx` — both-culture text
  for all 6 codes.
- `CustomerProfileEndpointModule.AddCustomerProfileEndpointPresentation` now registers
  `IErrorCatalogContributor` + `IErrorResourceSet`.

### 5. Typed session code at the HTTP boundary

Both `Unauthorized(...)` helpers now emit
`new SemanticError(CustomerProfileErrorCodes.SessionRequired)` instead of the raw string literal
`"customer.session.required"`. Observable status/classification is unchanged (Foundation descriptor:
`Forbidden`).

## Validator coverage — investigated, deliberately unchanged

W0 flagged the two GET requests as a possible validator gap. Investigation against the certified
siblings changed the verdict:

- Cart W3 records `CreateGuestCartCommand`, `MergeCartAfterLoginCommand` and
  `GetCurrentAuthenticatedCartQuery` as `NO_VALIDATOR_REQUIRED` because they "carry no transport input
  (owner identity/access token resolved server-side)".
- AddressBook's actor is likewise server-trusted and its own validator docs state `ActorUserId` is not
  validated as payload.

The canonical repository rule is therefore **transport-shape** validation, not "a validator per
request". Both GETs carry only a server-trusted `ActorUserId` and remain
`NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT`. Adding `Guid.Empty` rules would have been a fabricated gap
fix; the equivalent protection is now delivered at the application seam (see §3).

**Final matrix: 3 endpoint-reachable requests — 1 `VALIDATOR_REQUIRED` (present) + 2 `NO_VALIDATOR_REQUIRED`.**

## Guard changes (narrowed, not weakened)

Three inherited Host-era guards encoded the pre-catalog state and would have blocked the canonical
catalog. Each was narrowed to its precise intent:

| Guard | Before | After |
|---|---|---|
| `HostCustomerFullClosureGuardTests.Customer_session_required_is_not_re_registered_by_CustomerProfile` | asserted **no** `IErrorCatalogContributor` may exist in Endpoints at all | asserts the Foundation-owned `customer.session.required` descriptor is never re-registered (matches `new ErrorDefinition`/`new ErrorDescriptor` with either the literal or `CustomerProfileErrorCodes.SessionRequired`) |
| `CustomerPanelCompositionTests.Customer_account_presentation_is_module_owned_and_contracts_only` | `Assert.Contains("customer.session.required", endpoints)` | `Assert.Contains("CustomerProfileErrorCodes.SessionRequired", endpoints)` |
| `CustomerProfileFoundationTests.Endpoint_uses_session_and_supports_profile_update` | `Assert.Contains("customer.session.required", source)` | `Assert.Contains("CustomerProfileErrorCodes.SessionRequired", source)` |
| `CustomerProfileFoundationTests.Invalid_values_are_rejected` | `Assert.ThrowsAsync<InvalidOperationException>` | `Assert.ThrowsAsync<ContractOperationException>` (the canonical typed fault) |

No guard was deleted, no baseline widened, and each guard still fails if the invariant it protects
regresses.

## Behavior preservation

| Surface | State |
|---|---|
| Routes / methods | `UNCHANGED` (`GET`/`PUT /v1/customer/profile`, `GET /v1/customer/dashboard`, `GET /v1/customer/dev-context`) |
| Response DTO shape / JSON field parity | `UNCHANGED` |
| dev-context `PLATFORM_DEV_ROUTE_EXCEPTION` (404 + raw anonymous JSON) | `PRESERVED` |
| Session failure status/classification | `UNCHANGED` (Foundation `Forbidden` descriptor) |
| Actor resolution precedence | `UNCHANGED` |
| Domain invariants (3..128, 64, 32, 200, name derivation) | `UNCHANGED` (values only became typed codes) |
| Schema / migrations | `UNCHANGED` (0 migration files touched) |
| DI lifetimes / outbox registration | `UNCHANGED` |
| Seed values / idempotency | `UNCHANGED` |
| Host composition seams | `UNCHANGED` |

## Validation

Focused surface (`CustomerProfile|CustomerPanel|StorefrontAccountIdentity|ErrorCatalogUniqueCode`):

```
Failed: 2, Passed: 28, Skipped: 4, Total: 34
```

The 2 failures are the **pre-existing** stale solution-grouping guards identified in W0
(`CustomerProfileSolutionGroupingGuardTests`, `HostCustomerProfileEvacuationGuardTests.Parent_R1_solution_grouping_remains_exact`);
both assert a flat `/Modules/` folder that does not exist in `Tooba.slnx`. They are repaired in W2.

Full Host suite, W1 tree vs a clean worktree at the W0 baseline `39a5de09`:

```
W0 baseline: Failed 85 / Passed 1816 / Skipped 130 / Total 2031
W1 tree:     Failed 85 / Passed 1816 / Skipped 130 / Total 2031
Compare-Object by test name -> IDENTICAL FAILURE SETS - ZERO NEW FAILURES
```

## Production-scope proof

Changed/added files are exactly: Contracts `Errors/CustomerProfileErrorCodes.cs`; Domain
`CustomerProfile.cs` + `.csproj`; Application `Composition/CustomerProfileOperation.cs`,
`Commands/UpsertCustomerProfile/UpsertCustomerProfileCommand.cs`,
`Queries/GetCustomerProfilePage/GetCustomerProfilePageQuery.cs`,
`Queries/GetCustomerAccountDashboard/GetCustomerAccountDashboardQuery.cs`; Endpoints
`Errors/CustomerProfileErrorCatalogContributor.cs`,
`Resources/CustomerProfileErrorResources.cs` + `CustomerProfileErrors{,.fa}.resx`,
`CustomerProfileEndpointModule.cs`, `Customer/CustomerProfileEndpoints.cs`,
`CustomerDashboard/CustomerAccountDashboardEndpoints.cs`; Infrastructure
`CustomerProfileDirectory.cs`; plus the four guard/behavior test files above.

No other module, no Host production file, no frontend, no schema/migration, no solution structure.
