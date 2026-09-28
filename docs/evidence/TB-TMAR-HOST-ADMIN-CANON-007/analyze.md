# TB-TMAR-HOST-ADMIN-CANON-007 — Analyze

## Defect

`src/backend/Host/Tooba.Host/Admin/AdminDevActorBootstrap.cs` carried:

1. `using Tooba.Identity.Infrastructure;` — a Host admin file referencing a foreign module's
   Infrastructure layer.
2. `catch (InvalidOperationException) { return; }` around the tuple-membership write — a generic
   exception used as expected-flow control.

## MANDATORY AUDIT

### 1. `AdminDevActorBootstrap.cs`

Referenced `ICurrentTenant`, `IIdentityAuthenticationService` (Identity.Contracts),
`RegisterUserCommand` / `LoginIdentifierKind` (Identity.Contracts), `IAuthorizationTupleWriter`
(BuildingBlocks). The only `Identity.Infrastructure` symbol actually needed was… **none**:
`IdentityDuplicateIdentifierFault` already lives in `Tooba.Identity.Contracts.Problems`. The
`using Tooba.Identity.Infrastructure;` was a dead/forbidden import.

### 2. `IIdentityAuthenticationService` and its Contracts faults

```text
src/backend/Modules/Identity/Tooba.Identity.Contracts/Auth/IdentityAuthenticationContracts.cs
  public interface IIdentityAuthenticationService (RegisterAsync, FindUserIdByIdentifierAsync, ...)

src/backend/Modules/Identity/Tooba.Identity.Contracts/Problems/IdentityDuplicateIdentifierFault.cs
  public sealed class IdentityDuplicateIdentifierFault : Exception   // typed duplicate fault
```

The duplicate-registration path is already typed via `IdentityDuplicateIdentifierFault`; the
existing catch of that exact fault is correct and retained.

### 3. Authorization tuple writer contract / idempotency

```text
src/backend/BuildingBlocks/Tooba.BuildingBlocks/Authorization.cs
  public interface IAuthorizationTupleWriter { Task WriteAsync(AuthorizationRelationshipWrite, CancellationToken); }
```

Implementations prove the write is an **idempotent upsert (Touch)**:

| Adapter | Behavior |
| --- | --- |
| `SpiceDbAuthorizationAdapter.WriteAsync` | `AuthorizationRelationshipOperation.Delete => Delete`, `_ => Touch` |
| `InMemoryAuthorizationAdapter.WriteAsync` | `_tuples[key] = 1` (ConcurrentDictionary upsert) |
| `FailClosedAuthorizationAdapter.WriteAsync` | `Task.FromException(new InvalidOperationException("authorization.unavailable"))` |

### 4. Existing typed exception/result for an already-existing relationship

None exists. No `ContractOperationException` is thrown by the tuple writer, and no typed
"relationship already exists" fault is defined. However **none is required**: the contract is
idempotent by construction, so a repeated membership write is not a failure. The catch was
protecting against the *unavailable* engine (`FailClosedAuthorizationAdapter`), which is not
"expected flow" — it is an infrastructure failure.

### 5. Development bootstrap call site

```text
Wallet/WalletDevelopmentSeedHost.cs:42
Support/SupportDevelopmentSeedHost.cs:45
Development/ProductWorkspaceDevelopmentBootstrap.cs:163, 289
Development/MarketplaceDevelopmentBootstrap.cs:63 (via MarketplaceAdminDevBootstrap)
Admin/AdminPanelEndpoints.cs:93 (reads Snapshot)
Settings/SettingsFoundationDevelopmentSeed.cs:93,107 (reads Snapshot)
```

`appsettings.Development.json` sets `Tooba:Authorization:Mode = InMemory`, whose writer is the
idempotent upsert. Call count and order are unchanged by this task.

## Decision

1. Remove `using Tooba.Identity.Infrastructure;` — Identity dependency becomes Contracts-only.
2. Remove the blanket `catch (InvalidOperationException)`:
   - the tuple writer is contractually idempotent (upsert/Touch) → the catch is unnecessary;
   - the typed duplicate path (`IdentityDuplicateIdentifierFault`) is kept for registration.
3. No message parsing, no new fault type, no new error semantics.

`using Tooba.BuildingBlocks;` is retained (it owns `IAuthorizationTupleWriter` and
`AuthorizationRelationshipWrite`). The previously unused `ContractOperationFault` idea is not
introduced: no typed tuple fault exists and none is needed.
