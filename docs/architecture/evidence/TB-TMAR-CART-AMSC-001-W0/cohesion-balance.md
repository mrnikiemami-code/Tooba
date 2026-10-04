# TB-TMAR-CART-AMSC-001-W0 — Cohesion Balance

## `File-Cohesion-State = OVERSIZED_ONLY` (one file) + one multi-responsibility Contracts bundle

## Size inventory (non-generated `.cs`, physical LOC)

| LOC | File | Classification |
| --- | --- | --- |
| **831** | `Tooba.Cart.Infrastructure/Directories/CartDirectory.cs` | **OVERSIZED_LEGACY** (800 < 831 ≤ 1500) — **not in baseline** ⇒ live `NEW_OVERSIZED_FILE` |
| 349 | `Tooba.Cart.Domain/Aggregates/ShoppingCart.cs` | NORMAL |
| 170 | `Tooba.Cart.Domain/Entities/CartLine.cs` | NORMAL |
| 163 | `Tooba.Cart.Infrastructure/Events/CartEvents.cs` | NORMAL |
| 150 | `Tooba.Cart.Application/Presentation/CartPresentationComposer.cs` | NORMAL |
| 128 | `Tooba.Cart.Infrastructure/Messaging/CartOutboxRegistration.cs` | NORMAL |
| 133 | `Tooba.Cart.Infrastructure/Persistence/CartDbContext.cs` | NORMAL |
| 122 | `Tooba.Cart.Infrastructure/Lifetime/CartExpiryWorker.cs` | NORMAL |
| 110 | `Tooba.Cart.Application/Errors/CartExceptionMapper.cs` | NORMAL |
| 82 | `Tooba.Cart.Application/Ports/ICartDirectory.cs` | NORMAL |
| < 80 | all remaining 31 production files | NORMAL |

`TmarSourceSizeGuard.Classify("cs", n)`: `> 1500` = `CRITICAL_GOD_FILE`; `> 800` =
`OVERSIZED_LEGACY`; `> 500` = `WATCH`; else `NORMAL`. Threshold for new files: **800**.

Cart has **no** `CRITICAL_GOD_FILE`, **no** `WATCH`, exactly **one** `OVERSIZED_LEGACY`.

## `CartDirectory.cs` — is it a god-file?

**No.** It implements exactly two interfaces (`ICartDirectory`, `CartContract.ICartQueryGateway`) over
exactly one persistence source (its own `Carts` DbSet + own `cart.carts` schema). It has a single
public identity and a single reason to exist.

However it is **not single-responsibility** in the internal sense. Its members cluster into three
independently-changing concerns:

### Concern 1 — Cart mutation / read orchestration

```text
GetCartAsync, CreateAuthenticatedAsync, CreateGuestAsync, AddOrIncreaseLineAsync,
ChangeLineQuantityAsync, RemoveLineAsync, AbandonAsync, ConvertAsync,
FindActiveAuthenticatedAsync, MergeAnonymousAfterLoginAsync,
LoadAsync, LoadRequiredAsync, EnsureAccess, SaveCartAsync, CreateAuthenticatedCoreAsync,
ChangeLineCoreAsync, RemoveLineCoreAsync, ReleaseAllAsync
```

Changes when the **cart lifecycle** changes.

### Concern 2 — Offer / Pricing / Inventory quote-and-validate collaboration

```text
ValidateOfferAndQuoteAsync, EnsureSellableAsync, RevalidateCampaignQuotesAsync,
MergeLineWithoutReservationAsync
```

Changes when the **foreign quote/stock contract** changes. `ValidateOfferAndQuoteAsync` alone
orchestrates `IOfferLookupGateway`, `IPriceLookupGateway`, `ICatalogCartQuantityPolicyGateway`,
`ICampaignCartPriceAuthority`, `IQuantityNormalizer`, `IInventoryAvailabilityGateway`.

### Concern 3 — Expiry batch scanning + snapshot projection

```text
ExpireDueCartsAsync, ExpireDueBatchAsync, ToSnapshotAsync
```

Changes when the **expiry/sweep policy** or the **Cart→Contracts projection** changes.
`ExpireDueBatchAsync` owns the only raw SQL in the module and its own transaction boundary.

### Cross-cutting collaborators currently embedded

`CartLineCurrency` was already extracted to its own file — proving the module's own convention is to
extract cohesive collaborators out of `CartDirectory`.

## Splitting plan (W1) — cohesion, not cosmetics

```text
Tooba.Cart.Infrastructure/Directories/
  CartDirectory.cs             (Concern 1 only — orchestration, target < 500 LOC)
  CartQuoteValidator.cs        (Concern 2 — internal sealed collaborator)
  CartSnapshotProjector.cs     (Concern 3a — Cart -> Contracts projection)
  CartExpiryScanner.cs         (Concern 3b — batch claim + expiry + hold release)
  CartLineCurrency.cs          (unchanged)
```

Rules for W1:

1. Every public port member keeps its exact signature and behavior.
2. `ICartDirectory` and `CartContract.ICartQueryGateway` are still implemented by `CartDirectory`
   (so `services.AddScoped<ICartQueryGateway>(sp => (CartDirectory)sp.GetRequiredService<ICartDirectory>())`
   keeps working) — the extracted collaborators are **internal** and injected as constructor
   dependencies.
3. The raw `FOR UPDATE SKIP LOCKED` SQL and its transaction boundary move **verbatim**.
4. `CartDirectory` must end **below 800 LOC** so no baseline entry is needed.
5. No new public type is introduced; the DI registration surface is unchanged.

## Multi-responsibility Contracts bundle (G7)

`Tooba.Cart.Contracts/Checkout/CartContracts.cs` = **141 LOC declaring 11 types** across three
capability families (lifecycle enums, read model, checkout conversion). Its reason-to-change count is
11, not 1. This is the `MULTI_RESPONSIBILITY_COHESION_VIOLATION` shape for Contracts (a bundle of
DTOs/snapshots/ports with different reasons to change), even though it is under the LOC threshold.

W2 splits it by capability. **Constraint:** all types currently live in the **root** namespace
`Tooba.Cart.Contracts`, and external consumers (`Order.*`, `Catalog.Application`,
`Fulfillment.Endpoints`, Host, tests) import `using Tooba.Cart.Contracts;`. W2 must keep the root
namespace for these types or update every consumer — the smaller, evidence-backed option wins.

## Other cohesion observations (no action required)

| File | Note |
| --- | --- |
| `Domain/Events/*` (6 files) | one event per file — correct, matches Offer's `OfferDomainEvents.cs` being the outlier, not Cart |
| `Infrastructure/Events/CartEvents.cs` (163 LOC) | 6 integration events in one file; cohesive (one outbox contract family). `Settlement`/`Order` do the same. `OVERSIZED_ONLY`-adjacent but under threshold — **no action** |
| `Application/Models/CartPage.cs` | 3 lines, zero types — **stale tombstone** (G6), delete in W2 |
| `Application/Ports/ICartPersistenceHoursSource.cs` | empty alias interface — hygiene candidate |

## Guard interaction

`TmarSourceSizeAndInfraAppTests.Hand_written_source_size_does_not_expand_beyond_baseline` currently
reports the Cart entry:

```text
[NEW_OVERSIZED_FILE] file=src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Directories/CartDirectory.cs
  currentLoc=832 baselineLoc=n/a threshold=800
```

The same test also reports a `.tmp-baseline/...` copy (stale sibling git worktree at
`D:/Users/User/source/repos/SarvNewVer/.tmp-baseline`, commit `87a22d7c`) and unrelated
Catalog/Order/Payment entries. Only the Cart entry is in scope.

**W1 must make the Cart entry disappear by decomposition — never by adding a baseline entry.**
