# TB-TMAR-CART-AMSC-001-W0 — Stale / Duplicate Physical Copy

## `Physical-Copy-State = STALE_COPY`

## 1. `Application/Models/CartPage.cs` — comment-only tombstone

```csharp
// Presentation DTOs moved to Tooba.Cart.Contracts (CartPage, CartLineView, ICartPresentationGateway).
// This file kept only so historical path comments remain discoverable; no types declared here.
namespace Tooba.Cart.Application.Models;
```

- **Zero** type declarations, **zero** behavior.
- The real types live in `Tooba.Cart.Contracts/Presentation/CartPresentationContracts.cs`.
- It exists purely so a historical path remains discoverable, which is not an architectural
  justification.
- It is the **only** reason `Application/Models/` still exists.

**Disposition:** delete in W2 (together with the now-empty `Models/` folder). No consumer, guard or
manifest requires it. Verified: `grep` for `Tooba.Cart.Application.Models` across
`src/backend` returns no production usage.

## 2. `Application/Ports/ICartPersistenceHoursSource.cs` — redundant empty alias interface

```csharp
namespace Tooba.Cart.Application.Ports;

/// <summary>
/// Cart-internal alias of <see cref="Tooba.Cart.Contracts.Lifetime.ICartPersistenceHoursSource"/>.
/// Cross-module consumers should prefer the Contracts port.
/// </summary>
public interface ICartPersistenceHoursSource : Tooba.Cart.Contracts.Lifetime.ICartPersistenceHoursSource
{
}
```

Two DI identities exist for one responsibility
(`CartModule.cs:46-48`):

```csharp
services.AddScoped<ICartPersistenceHoursSource>(sp => sp.GetRequiredService<CartPersistenceHoursSource>());
services.AddScoped<Tooba.Cart.Contracts.Lifetime.ICartPersistenceHoursSource>(
    sp => sp.GetRequiredService<CartPersistenceHoursSource>());
```

**Disposition:** this is a *duplicate boundary identity*, not a stale file — it declares a type and
is registered. Removing it is a behavior-preserving DI simplification, but it must be verified
against `CartArchitectureGuardTests` / `HostCartResidualGuardTests` and any DI assertion before
removal. W1 decides with evidence; if any guard asserts the Application alias registration, it stays
and is recorded as accepted.

## 3. Duplicate `using` in `Tooba.Cart.Tests/Behavior/CartPresentationAndErrorTests.cs`

```csharp
using Tooba.Cart.Contracts;
using Tooba.Cart.Application.Presentation;
using Tooba.Cart.Contracts;   // <-- duplicate, CS0105
```

**Disposition:** remove the duplicate in W1. It is the only build warning attributable to Cart.

## 4. Sibling git worktree `.tmp-baseline` (outside the module, but a scan artifact)

`D:/Users/User/source/repos/SarvNewVer/.tmp-baseline` is a **live git worktree** at commit
`87a22d7c` (detached). `TmarSourceSizeGuard.ScanHandWrittenSources` walks the repository root and
therefore scans this worktree's copy of every source file, producing dozens of spurious
`NEW_OVERSIZED_FILE` violations (including a `.tmp-baseline/.../CartDirectory.cs` entry).

`git worktree list` at HEAD:

```text
D:/Users/User/source/repos/SarvNewVer               17e95804 [main]
C:/Users/User/AppData/Local/Temp/order-base-wt      04c22c37 (detached HEAD)
D:/Users/User/source/repos/_order_head_baseline     04c22c37 (detached HEAD)
D:/Users/User/source/repos/SarvBaselineR3           a98aca6e (detached HEAD)
D:/Users/User/source/repos/SarvNewVer/.tmp-baseline 87a22d7c (detached HEAD)
D:/Users/User/source/repos/SarvNewVer-Codex         144d1828 [codex/p09]
```

**Disposition:** **OUT OF SCOPE.** Removing another task's worktree is a destructive repository
operation the AMSC run is not authorized to perform. Recorded as pre-existing repo-wide gate drift.
The Cart entry inside `.tmp-baseline` is a *copy* of the in-scope Cart file and disappears when W1
decomposes the real `CartDirectory.cs`.

## 5. Duplicate type scan — ZERO real duplicates

| Check | Result |
| --- | --- |
| Same type name declared twice in `Tooba.Cart.*` | **0** |
| `CartStatus` in both `Contracts` and `Domain.ValueObjects` | distinct types in distinct assemblies (Contracts snapshot enum vs Domain state enum), mapped explicitly by `(CartContract.CartStatus)(int)cart.Status` — intentional |
| `CartAccessKind` in both `Contracts` and `Domain.ValueObjects` | same pattern, intentional |
| `CartConversionIntent` in both `Contracts` and `Domain.ValueObjects` | same pattern, intentional |
| `ICartPersistenceHoursSource` in both `Contracts.Lifetime` and `Application.Ports` | **alias inheritance**, see item 2 |
| `CartExceptionMapper.TryMapExact` cases duplicating `CartErrorCodes` values | string duplication (G2), not a type duplicate |

The three `Contracts`/`Domain` enum pairs are the module's deliberate Contracts-boundary projection:
the Domain enums drive the state machine, the Contracts enums are the cross-module snapshot shape,
and `CartDirectory.ToSnapshotAsync` performs the explicit numeric cast. Removing either side would
leak Domain types across the boundary. **Preserved invariant.**

## 6. Untracked foreign artifacts in the working tree (not Cart)

```text
?? docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/RESULT.bridge.txt
?? docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/post-result.js
?? docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5-R1/worker-result.txt
```

Leftovers from the AddressBook and Order runs. **Not committed by this AMSC run.**

## Summary

| Item | Type | Disposition |
| --- | --- | --- |
| `Application/Models/CartPage.cs` | STALE_COPY | delete in W2 |
| `Application/Ports/ICartPersistenceHoursSource.cs` | DUPLICATE_BOUNDARY_IDENTITY | W1 decides with evidence |
| duplicate `using` in `CartPresentationAndErrorTests.cs` | build warning | fix in W1 |
| `.tmp-baseline` worktree | scan artifact, out of scope | report only |
| untracked AddressBook/Order artifacts | foreign residue, out of scope | report only |
