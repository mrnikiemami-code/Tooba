# TB-TMAR-CART-AMSC-001-W0 — Analyze (AMSC Wave 0)

- Task: `TB-TMAR-CART-AMSC-001-W0`
- Mode: `ANALYSIS_ONLY`
- Skill: `.cursor/skills/tooba-architecture-analyze/SKILL.md`
- Target: `src/backend/Modules/Cart/Tooba.Cart.*`
- Branch: `main`
- HEAD at analysis start: `17e95804aa0f261a2a98f55278f671aafd5e41d9` (== `origin/main`)
- Goal of the AMSC run: drive `Cart` to a professional, Visual-Studio-standard, zero-foreign-coupling
  surface that can be lifted out as an independent microservice.
- Structure handoff owner: `.cursor/skills/tooba-architecture-structure/SKILL.md`

## Scope and bounded recovery unit

Bounded to the **Cart module surface** — 5 production projects + 1 test project,
**41 hand-written production `.cs` files** + 2 `.resx` + 5 EF-generated migration/snapshot files.

The module is already `structureCertified: true` under `ARCH-COMPLETE-002` in
`docs/architecture/tmar-module-structure-manifests.json` (entry `module: "Cart"`), with accepted
disposition history:

- `TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001` (last accepted certification, commit `6e880942`)
- `TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1` (post-cert repair, commit `eea83df9`)
- `TB-TMAR-CART-MULTICURRENCY-LINES-001`, `TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001` (platform-side)

Ownership and coupling are **not** reopened as an ownership question — they are re-verified against
current disk. This wave audits current-disk **cohesion, canonical-mechanism, folder-granularity and
microservice-extractability** debt that the existing certificate must not hide, and re-verifies every
previously certified invariant at this HEAD.

## Canonical mechanism discovery (performed before judging)

See `canonical-mechanisms.md`. Summary of what was **discovered** (not assumed):

| Concern | Canonical mechanism found in repository | Reference used |
| --- | --- | --- |
| Result / expected failure | `Tooba.BuildingBlocks.Results.Result` / `Result<T>` + `SemanticError` | BuildingBlocks, AddressBook, AccessControl |
| Typed fault exception | `Tooba.BuildingBlocks.SemanticException(SemanticError)` (`.Error.Code`) | BuildingBlocks, Host `CheckoutIdentityGate` |
| Fault→Result composition | `Application/<X>ExceptionMapper.TryAsync(...)` (exact-code allowlist) | **Payment** (`PaymentExceptionMapper`, 27 codes + aliases) |
| API response mapping | `ApiResponseFactory.From(Result<T>)` / `From(Result)` / `Created` / `FromFailure` | BuildingBlocks, AddressBook, Offer |
| Error catalog | `IErrorCatalogContributor` + `ErrorDescriptor` + `IErrorDefinitionCatalog` + `SafeErrorMapper` | Offer, Cart (present) |
| Localization | `IErrorResourceSet` + `CartErrors.resx` / `.fa.resx` + `IErrorMessageLocalizer` | Offer, Cart (present) |
| Stable codes | `<Module>ErrorCodes` (module-local `Application.Errors` for Cart/Payment; `Contracts.Errors` for Offer/AddressBook) | Cart, Payment, Offer |
| Validation codes | `<Module>ValidationCodes` in `Application/Validation/` | Cart (present), Offer |
| CQRS | `AddToobaCqrsFoundation` (MediatR 12.5.0) | `TmarFoundation.cs` |
| Logging | `ILogger<T>` + `ObservabilityLogScope` | BuildingBlocks |
| Tracing / correlation | `ToobaTelemetry`, `IModuleCallTracer`, `ICorrelationIdProvider` (`X-Correlation-Id`) | BuildingBlocks |
| Size / cohesion guard | `TmarSourceSizeGuard` + `Baselines/tmar-source-size-baseline.json` (threshold 800) | `Tooba.Host.Tests` |

**Key discovery 1:** `Cart` already uses the canonical `Result<T>` / `SemanticError` /
`ApiResponseFactory` / catalog / resx stack end-to-end. Unlike the preceding AddressBook run, the
API-result, localization, catalog and correlation concerns are **already canonical**. The remaining
fault-boundary debt is the *mechanism* used to reach `Result`: `CartExceptionMapper` classifies
failures by **exact match on `InvalidOperationException.Message`**, and Domain/Infrastructure throw
`InvalidOperationException` whose `Message` *is* the machine code.

**Key discovery 2:** the repository has **two** established fault mechanisms and `Cart` uses the
*legacy* one:

1. `SemanticException(SemanticError)` — the framework-level typed fault, mapped by `SafeErrorMapper`
   via `exception.Error.Code`. Used by Host `CheckoutIdentityGate` for the shared
   `checkout.authentication_required` code.
2. `<Module>ExceptionMapper.TryAsync(...)` + `InvalidOperationException`-with-code-message — used by
   `Payment` and `Cart`. The Payment variant adds a `ContractOperationException` arm; Cart does not.

The message-as-code mechanism is what the Analyze/Certify skills classify as `STRING_HEURISTIC`
(`failure classification done by parsing exception.Message`) even though Cart's implementation is
exact-match rather than `Contains`-heuristic. W1 must converge Cart on the typed fault while
preserving the exact same external HTTP/status/code outcomes.

**Key discovery 3:** `CartErrorCodes` has **no catalogued descriptor for 5 codes it can actually
emit** (`QuantityInvalid`, `LineMissing`, `LineCurrencyMissing` are catalogued; but
`cart.commerce.*`, `cart.outbox.unmapped_event`, `cart.pricing.quote_missing`,
`cart.offer.missing|inactive|channel_mismatch`, `cart.line.quantity_positive|ceiling`,
`cart.quantity_policy.missing`, `cart.inventory.missing`, `offer.min_quantity.not_met`,
`offer.max_quantity.exceeded`, `cart.line.requires_active`, `cart.converted.not_expirable`,
`cart.assign.guest_only`, `cart.market.required`, `cart.currency.invalid`,
`cart.expiry.*`, `cart.user_id.required`, `cart.line.merge_via_quantity`, `cart.convert.order_required`,
`cart.guest_secret.hash_required`, `cart.version.stale`, `cart.guest_secret.invalid`) are **mapped by
the exception mapper into a catalogued code** or **silently become HTTP 500**.

## Structured state fields

1. **Foundation-State** — `FOUNDATION_READY` (5 projects present, `structureCertified: true`, all
   required capability folders exist).
2. **Ownership-State** — `correct` for all five layers. No `MUST_SPLIT` file by responsibility.
3. **File-Cohesion-State** — `OVERSIZED_ONLY` for `CartDirectory.cs` (831 LOC, single cohesive
   responsibility: Cart write/read directory over its own schema). Every other production file is
   `COHESIVE`. No mixed `*Contracts.cs` bundle, no god-file with unrelated responsibilities.
4. **Oversized/God-File-State** — one entry: `Tooba.Cart.Infrastructure/Directories/CartDirectory.cs`
   = **831 LOC** → `OVERSIZED_LEGACY` (over the 800 `thresholdNewFileLoc`, under the 1500
   `CRITICAL_GOD_FILE` line). **Not** in `tmar-source-size-baseline.json`, therefore a **live
   `NEW_OVERSIZED_FILE` violation** of `ARCH-SIZE-001`.
5. **Localization-State** — `CANONICAL`. All 13 user-facing codes resolve through
   `CartErrorResourceSet` → `CartErrors.resx` / `CartErrors.fa.resx`; no hard-coded user-facing text
   in Domain/Application/Endpoints/Infrastructure; no `exception.Message` used as a localized
   contract.
6. **API-Result-Pattern-State** — `CANONICAL`. All 7 routes map through `ApiResponseFactory`
   (`api.From(...)`); zero raw `Results.Json` / `Results.BadRequest` / `Results.Problem`; zero local
   `ProblemDetails` builder; zero `catch`-and-map in Endpoints.
7. **Stable-Error-Code-State** — `CATALOGUED` but **incomplete**:
   - 12 Cart codes catalogued in `CartErrorCatalogContributor`.
   - `checkout.authentication_required` correctly **not** re-registered (owned by
     `FoundationErrorCatalogContributor`), yet Cart **also carries a local duplicate key** in
     `CartErrors.resx` / `.fa.resx` and `CartErrorResourceSet.Owns(...)` claims it — a
     **localization-ownership duplicate**.
   - `CartErrorCodes` declares `Missing`, `GuestInvalid`, `AccessDenied`, `VersionConflict`,
     `Expired`, `QuantityInvalid`, `LineMissing`, `LineCurrencyMissing`, `OfferUnavailable`,
     `InventoryInsufficient`, `InventoryStale`, `Rejected`, `AuthenticationRequired` — i.e. the
     declared public vocabulary is **13**, while the internal failure vocabulary reached through
     `CartExceptionMapper` is **≈30 codes**, most of which are untyped free strings.
8. **Logging-State** — `CANONICAL`. No `Console.WriteLine`, no `Debug.WriteLine`, no second pipeline.
   `CartExpiryWorker` / `CartExpiryReconciler` log through `ILogger<T>` where they log at all.
9. **Sensitive-Logging-State** — `NONE`. Guest secret is never logged; `CartCredentialHasher` stores
   only a SHA-256 hash.
10. **OpenTelemetry-State** — `CANONICAL`. No `ActivitySource.StartActivity`, no second `Meter`, no
    manual `traceparent`.
11. **Correlation-Trace-State** — `CANONICAL`. No `AsyncLocal`, no custom header, no
    `Guid.NewGuid()`-as-correlation.
12. **CQRS-State** — `COMPLIANT`. 7 real `IRequest<T>` / `IRequest<Result<T>>` types, 7 real
    `IRequestHandler<,>`, 7 `ISender` dispatch sites, zero direct directory/persistence access from
    Endpoints, `AddToobaCqrsFoundation` registration.
13. **Validator-Coverage-State** — `EXHAUSTIVE` (4 `VALIDATOR_REQUIRED` + 3
    `NO_VALIDATOR_REQUIRED`), guarded by `CartEndpointValidatorCoverageGuardTests`. No gap.
14. **Contracts-Boundary-State** — `CLEAN` (semantics) but `CONVENTION_DRIFT` (foldering/naming):
    `Contracts/Checkout/CartContracts.cs` holds **4 enums + 4 DTOs + 2 ports + 1 request/result pair**
    in one file; sibling modules name the same role `*Contracts.cs` per capability
    (`Catalog/Checkout/CatalogCheckoutLookupContracts.cs`,
    `Inventory/Cart/CartInventoryHoldContracts.cs`) — so the suffix convention is repository-wide, but
    the **file is a multi-responsibility bundle** (7 distinct reasons to change). Additionally
    `Contracts/Lifetime/ICartPersistenceHoursSource.cs` and `Contracts/Presentation/...` are
    capability folders while the root-level role name `Checkout` is semantically wrong for
    cart-lifecycle enums (`CartStatus`, `CartAccessKind`, `CartConversionIntent`, `CartAccess`).
15. **Cross-Module-Coupling-State** — `LEGAL_CONTRACTS_ONLY`. Zero foreign
    Application/Infrastructure/Domain references, zero foreign `DbContext`/`DbSet`, zero
    cross-module SQL/EF join. Foreign edges are Contracts-only. **But two declared edges are
    UNUSED**: `Cart.Application → Tooba.Pricing.Contracts` and
    `Cart.Application → Tooba.Inventory.Contracts` have zero production usage.
16. **Cross-Module-Join-State** — `NONE`. `CartDirectory` touches only its own `Carts` set; the only
    raw SQL is `SELECT ... FROM cart.carts` (own schema, `FOR UPDATE SKIP LOCKED`).
17. **Persistence-Ownership-State** — `CORRECT`. One `CartDbContext`, schema `cart`, module-owned
    migrations, module-owned outbox registration, no foreign FK/DbSet.
18. **Endpoint-Ownership-State** — `MODULE_OWNED`. 7 module-owned routes under `/v1/storefront`;
    Host Cart HTTP ownership ZERO.
19. **Host-Residue-State** — ZERO illegal. Remaining Host references are all legitimate
    `ALLOWED_COMPOSITION_ROOT` / `ALLOWED_CONTRACT_CONSUMPTION`:
    - `Program.cs` — `AddCartEndpointPresentation()`, `MapCartEndpoints()`, CQRS assembly scan,
      `CartModule` registration
    - `Order/HostOrderStorefrontActor.cs` — `CartAccess` contract construction (storefront actor seam)
    - `Tooba.Host.Tests/GlobalUsings.CartSettlementApp.cs` — test-only global using
20. **Schema-Migration-State** — `UNCHANGED`. 3 migrations
    (`20260823100150_InitialCart`, `20260909130200_DecimalCartQuantity`,
    `20260919183000_CartLineMerchandisingCampaignId`) + designer + snapshot. No drift planned; W1–W3
    must not regenerate them.
21. **Behavior-Preservation-Risk** — `LOW` for structure/rename/ref-prune work, **`MEDIUM` for the
    typed-fault convergence (F1/F2)**: it changes the *internal* fault type but must keep every
    observable HTTP status, error code and message identical. Any code that is currently mapped
    must stay mapped; any code that is currently `500 platform.unexpected` must stay `500`.
22. **Canonical-Reference-Used** — `AddressBook` (immediately preceding AMSC run: `Result<T>`
    handlers + capability-first flattening + `Errors/` + resx; the closest AMSC precedent),
    `Payment` (`PaymentExceptionMapper` exact-code allowlist shape), `AccessControl`
    (`Application/Validation/` + capability-first), `Offer` (catalog contributor + `Contracts/Errors`),
    BuildingBlocks (`Result`, `SemanticError`, `SemanticException`, `ApiResponseFactory`,
    `SafeErrorMapper`).
23. **Final-Disposition** — `READY_TO_MIGRATE`.

## Findings

### F1 — `CartExceptionMapper` classifies failures by exact match on exception *message* (`STRING_HEURISTIC`)

`CartExceptionMapper.TryMapExact` switches on `exception.Message` and treats the message as the
machine code. Domain (`ShoppingCart`, `CartLine`) and Infrastructure (`CartDirectory`,
`CartLineCurrency`, `CartCommerceContextResolver`, `CartOutboxRegistration`) all throw
`InvalidOperationException("<code>")`.

Observable outcomes are **correct** (every code either maps to a catalogued public code or falls
through to `500`), but the mechanism is the legacy string-identity mechanism:

- the failure identity is not type-safe — any refactor of a string literal silently changes
  classification;
- `CartExceptionMapper` must keep a growing alias table (`"cart.guest_secret.invalid"`,
  `"cart.version.stale"`, `"cart.line.quantity_positive"`, `"offer.min_quantity.not_met"`, …)
  that duplicates knowledge already expressed at the throw site;
- a `try/catch (InvalidOperationException)` in `CartPresentationComposer.TryGetForOwnershipAsync`,
  `CartDirectory.MergeLineWithoutReservationAsync` and
  `CartDirectory.RevalidateCampaignQuotesAsync` catches **all** `InvalidOperationException`,
  including genuinely unexpected ones.

W1 must converge the throw sites on `SemanticException(new SemanticError(<code>))` while preserving
the exact external outcome for every code (including codes that today deliberately fall through to
`500`).

### F2 — Alias/legacy code strings are duplicated between throw sites and the mapper

`CartExceptionMapper` maps 18 non-public alias strings that do not exist in `CartErrorCodes`
(e.g. `"cart.pricing.quote_missing"`, `"cart.commerce.context_unavailable"`,
`"cart.outbox.unmapped_event"`, `"cart.line.quantity_positive"`, `"offer.min_quantity.not_met"`,
`"cart.user_id.required"`, `"cart.convert.order_required"`, `"cart.assign.guest_only"`,
`"cart.market.required"`, `"cart.currency.invalid"`, `"cart.expiry.*"`,
`"cart.line.merge_via_quantity"`, `"cart.guest_secret.hash_required"`,
`"cart.line.requires_active"`, `"cart.converted.not_expirable"`,
`"cart.quantity_policy.missing"`, `"cart.inventory.missing"`,
`"cart.offer.missing|inactive|channel_mismatch"`).

Some are **intentionally** not public (`cart.commerce.*` is a platform misconfiguration, correctly
`500`); others are mapped to a public code. W1 must classify each explicitly as
`PUBLIC_MAPPED` or `INTERNAL_UNMAPPED_500`, express both as typed constants, and keep the outcome
identical.

### F3 — Localization ownership duplicate for the shared `checkout.authentication_required` code

`CartErrorCatalogContributor` correctly declines to re-register the descriptor (comment documents
Foundation ownership). However `CartErrors.resx` and `CartErrors.fa.resx` each declare a
`checkout.authentication_required` key, and `CartErrorResourceSet.Owns(...)` claims it. This is
**duplicate descriptor-adjacent ownership at the localization layer** and a localization-key
collision risk with `FoundationErrorCatalogContributor`. Recorded as a W1 repair candidate:
either the Foundation resource set already resolves it (then Cart's key + `Owns` clause are dead
weight) or it does not (then Cart's key is load-bearing and the correct fix is different). W1 must
**verify empirically** before removing anything.

### F4 — Two UNUSED declared foreign project references (`Cart.Application`)

```text
Tooba.Cart.Application -> Tooba.Pricing.Contracts      (0 production usages)
Tooba.Cart.Application -> Tooba.Inventory.Contracts    (0 production usages)
```

Both are declared in `Tooba.Cart.Application.csproj` but never referenced by any Cart production
source file. They widen the module's declared dependency surface without any behavioural benefit and
are a direct **microservice-extractability** defect (the stated end goal). W1 removes them
(behaviour-preserving; a build of the full solution proves no transitive reliance).

### F5 — `CartDirectory.cs` is 831 LOC, over `ARCH-SIZE-001` threshold 800

`CartDirectory.cs` is `OVERSIZED_LEGACY` (831 > 800) and is **absent** from
`tmar-source-size-baseline.json`, so it is a live `NEW_OVERSIZED_FILE` violation. It is *not* a
god-file — it is one cohesive `ICartDirectory` + `ICartQueryGateway` implementation over one schema
— but it carries **three separable concerns** that the reference modules place in their own
collaborators:

1. Cart mutation/read operations (create, add, change, remove, abandon, merge, convert).
2. Offer/Pricing/Inventory **quote-and-validate** collaboration (`ValidateOfferAndQuoteAsync`,
   `EnsureSellableAsync`, `RevalidateCampaignQuotesAsync`).
3. Expiry batch scanning (`ExpireDueCartsAsync`, `ExpireDueBatchAsync`, raw `FOR UPDATE SKIP LOCKED`
   SQL) + snapshot projection (`ToSnapshotAsync`).

W1 must decompose it into cohesive collaborators **inside the same `Directories/` capability
folder** (or a sibling `Directories/`-scoped folder), preserving every public port signature, the
`ICartDirectory`/`ICartQueryGateway` DI registration identity and all behaviour. This is a
cohesion repair, not cosmetic splitting: each extracted file gets one reason to change.

### F6 — `Application/Ports/ICartPersistenceHoursSource.cs` is a redundant alias of the Contracts port

```csharp
public interface ICartPersistenceHoursSource : Tooba.Cart.Contracts.Lifetime.ICartPersistenceHoursSource
{
}
```

An empty Application-internal sub-interface of the Contracts port. `CartModule` registers **both**
the Application alias and the Contracts port, both forwarding to `CartPersistenceHoursSource`. The
alias adds a second DI identity for one responsibility with no behaviour. Recorded for W1/W2 as a
candidate for removal (must be verified against the DI registration and the architecture guard that
asserts the Contracts port registration).

### F7 — `Application/Commands/<UseCase>/` + `Application/Queries/<UseCase>/` technical-axis-first tree

```text
Application/Commands/AddCartLine/{AddCartLineCommand.cs, AddCartLineCommandValidator.cs}
Application/Commands/ChangeCartLineQuantity/{...Command.cs, ...CommandValidator.cs}
Application/Commands/CreateGuestCart/CreateGuestCartCommand.cs
Application/Commands/MergeCartAfterLogin/MergeCartAfterLoginCommand.cs
Application/Commands/RemoveCartLine/{...Command.cs, ...CommandValidator.cs}
Application/Queries/GetCart/{GetCartQuery.cs, GetCartQueryValidator.cs}
Application/Queries/GetCurrentCart/GetCurrentAuthenticatedCartQuery.cs
```

- `Application/Commands/` and `Application/Queries/` are **top-level technical axes** — exactly the
  shape the Structure skill rejects by default.
- 5 of the 7 use-case leaves hold **one** production source file
  (`CreateGuestCart`, `MergeCartAfterLogin`, `GetCurrentCart`) or are named after one use case and
  hold the request + validator only — `OVER_FOLDERED` by the source-file counting rule.
- Cart is a **single-capability** module (the storefront shopping cart), so the canonical target is a
  plural capability folder with secondary `Commands` / `Queries` / `Validators` under it, matching
  `AddressBook/Addresses/`, `UserPreference/LocalePreferences/`, `Wishlist/Customer/`,
  `BulkInquiry/Storefront/`.

Note: `AddressBook/Addresses/` was the immediately preceding accepted AMSC outcome, and
`Offer/Offers/` uses the same plural-capability shape.

### F8 — `Application/Errors/` holds the module's stable public error vocabulary

`CartErrorCodes` lives in `Tooba.Cart.Application.Errors`. This is **not** drift: `Payment` does the
same (`Tooba.Payment.Application.Errors.PaymentErrorCodes`), and the Architecture guard
`CartArchitectureGuardTests` plus `AllowedApplicationFolders` both accept `Errors`. Recorded as
`CANONICAL_BY_SIBLING_PRECEDENT` — W1 must not move it to `Contracts` merely to match Offer.

### F9 — `Application/Models/CartPage.cs` is a comment-only tombstone file

```csharp
// Presentation DTOs moved to Tooba.Cart.Contracts (CartPage, CartLineView, ICartPresentationGateway).
// This file kept only so historical path comments remain discoverable; no types declared here.
namespace Tooba.Cart.Application.Models;
```

Zero types, zero behaviour, exists only to preserve a path. It is a **stale physical copy** of a
historical location and must be deleted in W2 (the manifest/guard also must not keep an `Models`
allowlist entry justified by it).

### F10 — `Contracts/Checkout/CartContracts.cs` is a multi-responsibility Contracts bundle

One file declares: `CartStatus`, `CartAccessKind`, `CartConversionIntent`, `CartLineAvailabilityKind`
(enums), `CartAccess`, `CartLineSnapshot`, `CartSnapshot` (DTOs), `ICartQueryGateway` (port),
`CartConversionRequest`, `CartConversionResult`, `ICartConversionPort` (conversion seam) — i.e. the
**cart lifecycle + cart read model + checkout conversion** capability families in one file.

The folder name `Checkout` is semantically wrong for `CartStatus`/`CartAccessKind`/`CartAccess`
(cart lifecycle, not checkout). W2 must split by capability and align folder names, keeping every
type's **namespace** stable where an external consumer depends on it (external consumers:
`Order.Contracts`, `Order.Application`, `Order.Infrastructure`, `Catalog.Application`,
`Fulfillment.Endpoints`, Host `HostOrderStorefrontActor`, `Tooba.Host.Tests`). Because
`Cart.Contracts` types are consumed through `using Tooba.Cart.Contracts;` (root namespace), any
folder split must either keep the root namespace or update all consumers — W2 must choose the
smaller-diff, evidence-backed option and must not silently break the root-namespace contract.

### F11 — `Application/Ports/` mixes lifetime, commerce-context, directory and persistence-hours ports

`Ports/` currently holds `ICartDirectory`, `ICartCommerceContextResolver`, `CartPersistenceHours`,
`ICartPersistenceHoursResolver`, `ICartPersistenceHoursSource`. Four of these belong to the
**cart-lifetime** capability that already has its own `Application/Lifetime/` folder
(`CartExpiryOptions`, `CartLifetimeOptions`, `ICartExpiryReconciler`). W2 should consolidate the
lifetime/persistence-policy ports with `Lifetime/` so one capability has one home.

### F12 — `Cart.Tests` has no `AssemblyInfo`-level exemption and duplicates a `using`

`CartPresentationAndErrorTests.cs` line 5 emits `warning CS0105: The using directive for
'Tooba.Cart.Contracts' appeared previously in this namespace` — a duplicate `using` introduced when
the file was split. Cosmetic but it is the only build warning attributable to Cart. W1/W2 cleans it.

### F13 — Pre-existing stale `CartArchitectureGuardTests` allowlist (RED at HEAD)

`CartArchitectureGuardTests.Cart_golden_boundaries_and_physical_layout_remain_clean` is **RED at this
HEAD** (reproduced: 22 passed / 1 failed / 0 skipped):

```text
Assert.Contains() Failure: Item not found in collection
Collection: ["Checkout", "Presentation"]
Not found:  "Lifetime"
   at CartArchitectureGuardTests.AssertNoRootDump(...) line 296
   at CartArchitectureGuardTests.Cart_golden_boundaries_and_physical_layout_remain_clean() line 49
```

The guard's `AllowedContractsFolders` was never updated when
`Contracts/Lifetime/ICartPersistenceHoursSource.cs` was added. This is guard **staleness**, not an
architecture regression (the folder is legitimate). W1/W2 must repair the allowlist to its
documented intent while keeping the Contracts assertions honest.

### F14 — Pre-existing repo-wide gate drift (proven pre-existing, out of scope)

Reproduced at this HEAD:

- `TmarSourceSizeAndInfraAppTests.Hand_written_source_size_does_not_expand_beyond_baseline` — 3
  failures, driven by (a) the stale sibling worktree `.tmp-baseline` being scanned, (b) the
  **pre-existing Cart `CartDirectory.cs` 831 LOC** entry (F5), (c) unrelated Catalog/Order/Payment
  drift.
- `TmarSourceSizeAndInfraAppTests.Source_size_inventory_evidence_exists_and_matches_scan_count` —
  inventory count 7260 vs scanned 1845 (stale sibling worktree + inventory evidence).
- `TmarSourceSizeAndInfraAppTests.Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline`
  — 3 `Tooba.Promotion.Infrastructure -> *` edges (unrelated module).

Only the **Cart** part (F5) is in scope. The `.tmp-baseline` scan, the Promotion edges and the
inventory count are pre-existing repo-wide drift and are reported, not repaired.

### F15 — Untracked foreign artifacts in the working tree

```
?? docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/RESULT.bridge.txt
?? docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/post-result.js
?? docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5-R1/worker-result.txt
```

Leftovers from the AddressBook and Order runs. They will **not** be committed by this AMSC run.

## Illegal dependencies

**None.**

- Zero foreign `*.Application`, `*.Infrastructure`, `*.Domain` references (csproj and `using`).
- Zero foreign `DbContext` / `DbSet` access; `CartDirectory` touches only its own `Carts`.
- Zero cross-module SQL/EF join; the only raw SQL reads `cart.carts`.
- Zero Host business or persistence authority.

Foreign edge inventory (`Cart` → foreign, Contracts only):

| From | To | Production usage |
| --- | --- | --- |
| `Cart.Contracts` | `Offer.Contracts` | `SalesChannel`, `OfferReference`/`OfferStatus` (via contracts) — USED |
| `Cart.Domain` | `Offer.Contracts` | `SalesChannel`, `OfferStatus` — USED |
| `Cart.Application` | `Catalog.Contracts` | `ICatalogCartPresentationLookup`, `ICatalogCartQuantityPolicyGateway` — USED |
| `Cart.Application` | `Party.Contracts` | `IPartyLookup` — USED |
| `Cart.Application` | `Offer.Contracts` | `SalesChannel` — USED |
| `Cart.Application` | `Pricing.Contracts` | **UNUSED (F4)** |
| `Cart.Application` | `Inventory.Contracts` | **UNUSED (F4)** |
| `Cart.Infrastructure` | `Catalog.Contracts` | quantity-policy gateway, store hours reader — USED |
| `Cart.Infrastructure` | `Inventory.Contracts` | availability + hold ports — USED |
| `Cart.Infrastructure` | `StoreContext.Contracts` | `ICurrentStoreCommerceContext` — USED |
| `Cart.Infrastructure` | `ModuleContracts` / `Persistence` | module/persistence platform — USED |

Reverse edge inventory (foreign → Cart, Contracts only): `Order.Contracts`,
`Order.Application`, `Order.Infrastructure` (`ICartQueryGateway`, `ICartPresentationGateway`,
`CartAccess`), `Catalog.Application` (`ICartPersistenceHoursSource`), `Fulfillment.Endpoints`
(`ICartQueryGateway`, `CartAccess`), Host `HostOrderStorefrontActor` (`CartAccess`).

## Cross-module join inventory

**None.**

## Contracts-only replacement map

No ownership replacement required. The existing boundaries are already the smallest lawful shape.
W1/W2 hygiene only:

- remove the 2 UNUSED declared edges (F4);
- consider removing the empty `Application` alias port (F6);
- split the `Contracts/Checkout` bundle by capability and correct folder semantics (F10) **without**
  changing the root-namespace contract.

## Target paths for W1 / W2

```text
Tooba.Cart.Application/
  Cart/                                   (plural capability folder — the module's single real capability)
    Commands/
      AddCartLineCommand.cs               (+ validator in Validators/)
      ChangeCartLineQuantityCommand.cs
      CreateGuestCartCommand.cs
      MergeCartAfterLoginCommand.cs
      RemoveCartLineCommand.cs
    Queries/
      GetCartQuery.cs
      GetCurrentAuthenticatedCartQuery.cs
    Validators/
      AddCartLineCommandValidator.cs
      ChangeCartLineQuantityCommandValidator.cs
      GetCartQueryValidator.cs
      RemoveCartLineCommandValidator.cs
  Composition/                            (fault→Result, mirrors AddressBook/AccessControl/Payment)
  Errors/CartErrorCodes.cs                (extended, existing values unchanged)
  Lifetime/                               (consolidated lifetime + persistence-policy ports)
  Ports/                                  (ICartDirectory, ICartCommerceContextResolver)
  Presentation/                           (unchanged)
  Validation/                             (shared cross-cutting rules — unchanged)
  Models/                                 (DELETED — F9 tombstone)
```

```text
Tooba.Cart.Infrastructure/Directories/
  CartDirectory.cs                        (mutation/read orchestration — under 800 LOC)
  CartQuoteValidator.cs                   (NEW — offer/price/stock validation seam, F5)
  CartSnapshotProjector.cs                (NEW — snapshot projection, F5)
  CartExpiryScanner.cs                    (NEW — batch expiry + SKIP LOCKED scan, F5)
  CartLineCurrency.cs                     (unchanged)
```

```text
Tooba.Cart.Contracts/
  Cart/CartLifecycleContracts.cs          (CartStatus, CartAccessKind, CartConversionIntent, CartAccess)
  Cart/CartReadContracts.cs               (CartLineSnapshot, CartSnapshot, CartLineAvailabilityKind, ICartQueryGateway)
  Checkout/CartConversionContracts.cs     (CartConversionRequest/Result, ICartConversionPort)
  Lifetime/ICartPersistenceHoursSource.cs (unchanged)
  Presentation/CartPresentationContracts.cs (unchanged)
```

## Migration order (W1 — behavior-preserving canonicalization)

1. Add typed Cart fault constants for every code reachable through the exception mapper and classify
   each as `PUBLIC_MAPPED` or `INTERNAL_UNMAPPED_500` (F2).
2. Convert Domain/Infrastructure throw sites to `SemanticException(new SemanticError(<code>))`
   (F1); keep `CartExceptionMapper` as the compatibility seam for `InvalidOperationException` so the
   external outcome set is provably unchanged, and narrow the bare `catch (InvalidOperationException)`
   sites to the mapped set only.
3. Verify empirically whether the Foundation resource set resolves `checkout.authentication_required`
   and repair the Cart localization duplicate accordingly (F3).
4. Remove the 2 UNUSED `Cart.Application` project references (F4).
5. Decompose `CartDirectory.cs` into cohesive collaborators, preserving every port signature and DI
   identity (F5).
6. Clean the duplicate `using` (F12) and repair the stale Contracts allowlist in
   `CartArchitectureGuardTests` (F13).
7. Focused build + focused guards + Cart behavior tests; commit and push W1.

W2 then flattens the Application tree (F7), consolidates `Ports`/`Lifetime` (F11), deletes the
`Models` tombstone (F9), splits the Contracts bundle by capability (F10) and updates the manifest +
durable structure guard. W3 certifies.

## Verification plan

- `dotnet build src/backend/Modules/Cart/Tooba.Cart.Tests/Tooba.Cart.Tests.csproj` → baseline
  0 errors / 1 warning.
- `dotnet test src/backend/Modules/Cart/Tooba.Cart.Tests/Tooba.Cart.Tests.csproj` → baseline
  22 passed / 1 failed (F13) / 0 skipped; W1–W2 must reach 23 passed / 0 failed.
- `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~HostCartResidualGuardTests`.
- `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~TmarSourceSizeAndInfraAppTests`
  → must prove the **Cart** entry is repaired and that no other violation was widened.
- Re-run the F14 gate set to prove the pre-existing repo-wide drift is unchanged (not widened).
- Post-change re-scan for: `InvalidOperationException` in Domain/Infrastructure/Application, raw
  `Results.Json` in Endpoints, `Tooba.Pricing.Contracts` / `Tooba.Inventory.Contracts` in
  `Cart.Application`, and single-file leaf folders under `Application/`.

## Certification blockers (to be closed in W1–W3)

1. **F1/F2** — `STRING_HEURISTIC` fault identity; untyped alias strings duplicated between throw
   sites and the mapper.
2. **F3** — localization ownership duplicate for `checkout.authentication_required`.
3. **F4** — 2 UNUSED declared foreign project references (microservice-extractability defect).
4. **F5** — `CartDirectory.cs` 831 LOC, live `ARCH-SIZE-001` `NEW_OVERSIZED_FILE` violation.
5. **F7** — `TECHNICAL_AXIS_FIRST` Application tree with over-foldered use-case leaves.
6. **F9** — stale comment-only `Models/CartPage.cs` tombstone.
7. **F10** — multi-responsibility `Contracts/Checkout/CartContracts.cs` bundle with wrong folder
   semantics.
8. **F13** — stale `CartArchitectureGuardTests` Contracts allowlist (RED at HEAD).

F6, F8, F11, F12, F14, F15 are recorded observations; F6/F11/F12 are cheap hygiene, F8 is
canonical-by-precedent (no action), F14/F15 are out of scope.

## Post-Host-Final-Closure regression check

`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` / `HOST_ROOT_FINAL_CERTIFIED` are present in SoT.
This wave adds/modifies **no** Host production file. All Host references to Cart remain
`ALLOWED_COMPOSITION_ROOT` / `ALLOWED_CONTRACT_CONSUMPTION`. `HOST_FINAL_CLOSURE_REGRESSION = NONE`.
