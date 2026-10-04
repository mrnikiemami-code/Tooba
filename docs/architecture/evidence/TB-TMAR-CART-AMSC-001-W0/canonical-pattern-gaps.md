# TB-TMAR-CART-AMSC-001-W0 — Canonical Pattern Gaps

Each gap is stated as: **current → canonical → exact sites**. No gap is invented; every claim is
reproducible from the current HEAD.

---

## G1 — `Localization-State` duplicate ownership for the shared `checkout.authentication_required` key

**Current**

- `CartErrorCodes.AuthenticationRequired = "checkout.authentication_required"` (Application-local constant).
- `CartErrors.resx` / `CartErrors.fa.resx` each declare a `checkout.authentication_required` key
  (`CartErrors.resx:51`, `CartErrors.fa.resx:51`).
- `CartErrorResourceSet.Owns(...)` returns true for that key (`CartErrorResources.cs:22`).
- `CartErrorCatalogContributor` correctly **does not** register the descriptor
  (`CartErrorCatalogContributor.cs:35` comment).

**Discovered facts**

- `FoundationErrorResourceSet.Owns(...)` matches only `validation.*`, `platform.*`, `admin.*`
  (`ResourceErrorMessageLocalizer.cs:29-31`).
- `FoundationErrors.resx` / `.fa.resx` contain **no** `checkout.*` key.
- Therefore the **only** registered `IErrorResourceSet` that can localize
  `checkout.authentication_required` is `CartErrorResourceSet`.

**Canonical**

The code is a genuinely cross-cutting checkout-boundary code owned by
`FoundationErrorCatalogContributor` for *descriptor* purposes. Its *localization* ownership is
currently satisfied by `CartErrorResourceSet` and (for the Order surface) by
`OrderErrorResourceSet.Owns(...)` which matches `checkout.*`
(`OrderErrorResources.cs:26`).

**Gap**

Cart holds a **descriptor-adjacent localization ownership** for a code it does not own. If
`FoundationErrorResourceSet` is ever extended to own `checkout.*`, `ResourceErrorMessageLocalizer`
iterates `_resourceSets` in registration order and **first non-empty wins** — a silent
localization-ownership race.

**W1 obligation**

Verify empirically which resource set resolves the key today; record it; and make the ownership
explicit and single-sourced. Do **not** remove the Cart key without proving another set resolves it
(removal would change the localized title for the Cart 401 response — a behavior change).

---

## G2 — `Stable-Error-Code-State` / fault identity: message-as-code (`STRING_HEURISTIC`)

**Current throw sites (all `InvalidOperationException("<code>")`)**

| File | Lines | Codes |
| --- | --- | --- |
| `Domain/Aggregates/ShoppingCart.cs` | 115, 137, 152, 170, 201, 216, 232, 250, 256, 271, 286, 303, 310, 317, 340 | `cart.user_id.required`, `cart.guest_secret.hash_required`, `cart.line.merge_via_quantity`, `cart.line.missing`, `cart.converted.not_expirable`, `cart.convert.order_required`, `cart.assign.guest_only`, `cart.expiry.future_required`, `cart.version.stale`, `cart.market.required`, `cart.currency.invalid`, `cart.expiry.after_created`, `cart.line.requires_active` |
| `Domain/Entities/CartLine.cs` | 162, 167 | `cart.line.quantity_positive`, `cart.line.quantity_ceiling` |
| `Infrastructure/Directories/CartDirectory.cs` | 325, 333, 341, 359, 499, 500, 507, 510, 517, 522, 554, 697, 704, 713, 725 | `cart.user_id.required`, `cart.guest_secret.invalid`, `cart.access.denied`, `cart.commerce.context_unavailable`, `cart.offer.missing`, `cart.offer.inactive`, `cart.offer.channel_mismatch`, `cart.quantity_policy.missing`, `offer.min_quantity.not_met`, `offer.max_quantity.exceeded`, `cart.pricing.quote_missing`, `cart.missing`, `cart.inventory.missing`, `cart.inventory.insufficient`, `cart.version.stale` |
| `Infrastructure/Directories/CartLineCurrency.cs` | 33 | `cart.line.currency_missing` |
| `Infrastructure/Lifetime/CartCommerceContextResolver.cs` | 30, 36, 43, 54 | `cart.commerce.context_unavailable`, `cart.commerce.market_unconfigured`, `cart.commerce.currency_unconfigured`, `cart.commerce.channel_unconfigured` |
| `Infrastructure/Messaging/CartOutboxRegistration.cs` | 158 | `cart.outbox.unmapped_event` |

**Canonical**

`SemanticException(new SemanticError(<code>))` — a typed fault whose identity is `Error.Code`, not a
message string. Host `CheckoutIdentityGate` already does this for the shared code.

**Gap**

Failure identity is not type-safe; `CartExceptionMapper` must maintain a growing alias table that
duplicates knowledge held at the throw site; and three bare `catch (InvalidOperationException)`
sites (`CartPresentationComposer.TryGetForOwnershipAsync:50`,
`CartDirectory.MergeLineWithoutReservationAsync:628`,
`CartDirectory.RevalidateCampaignQuotesAsync:821`) swallow **any** `InvalidOperationException`,
including genuinely unexpected ones.

**W1 obligation**

Introduce typed Cart faults, convert every throw site, and preserve the exact external outcome for
every code (mapped → same mapped code; unmapped → still `500 platform.unexpected`).

---

## G3 — `Microservice-Extractability`: two UNUSED declared foreign project references

**Current** (`Tooba.Cart.Application.csproj`)

```xml
<ProjectReference Include="..\..\Pricing\Tooba.Pricing.Contracts\Tooba.Pricing.Contracts.csproj" />
<ProjectReference Include="..\..\Inventory\Tooba.Inventory.Contracts\Tooba.Inventory.Contracts.csproj" />
```

**Evidence of non-use** — a full recursive scan of every non-generated `.cs` in
`Tooba.Cart.Application` finds **zero** occurrences of `Tooba.Pricing.Contracts` or
`Tooba.Inventory.Contracts` (the only `Pricing` hit is inside an XML `<summary>` doc comment in
`Ports/ICartDirectory.cs:51`).

**Canonical**

Declared dependency surface equals actual dependency surface. The microservice-extractability goal
stated by the Architect requires the smallest lawful boundary.

**W1 obligation** — remove both references and prove the full solution still builds.

---

## G4 — `ARCH-SIZE-001`: `CartDirectory.cs` = 831 LOC, absent from the size baseline

**Current**

- `Tooba.Cart.Infrastructure/Directories/CartDirectory.cs` = **831 physical LOC**.
- `TmarSourceSizeGuard.Classify("cs", 831)` → `OVERSIZED_LEGACY` (800 < 831 ≤ 1500).
- `tmar-source-size-baseline.json` contains **no** Cart entry.
- `TmarSourceSizeGuard.Evaluate` therefore reports
  `NEW_OVERSIZED_FILE ... currentLoc=832 ... threshold=800`.

**Canonical**

`thresholdNewFileLoc = 800`. A file above it must either be in the baseline (frozen) or be split.
`CartDirectory.cs` is **one cohesive `ICartDirectory` + `ICartQueryGateway` implementation** over one
schema — it is not a god-file — but it carries three separable reasons to change:

1. Cart mutation/read orchestration (create, add, change, remove, abandon, merge, convert, abandon).
2. Offer/Pricing/Inventory **quote-and-validate** collaboration
   (`ValidateOfferAndQuoteAsync`, `EnsureSellableAsync`, `RevalidateCampaignQuotesAsync`).
3. Expiry batch scanning with raw `FOR UPDATE SKIP LOCKED` SQL
   (`ExpireDueCartsAsync`, `ExpireDueBatchAsync`) and snapshot projection (`ToSnapshotAsync`).

**W1 obligation** — decompose into cohesive collaborators with **unchanged** public port signatures,
DI registration identity and behavior; do **not** add the file to the baseline as a way to pass.

---

## G5 — `Folder-Granularity-State = TECHNICAL_AXIS_FIRST` with 5 over-foldered leaves

See `folder-granularity.md` for the full measurement. Summary:

```text
Application/Commands/            <-- top-level technical axis
Application/Commands/AddCartLine/                 (2 files)
Application/Commands/ChangeCartLineQuantity/      (2 files)
Application/Commands/CreateGuestCart/             (1 file)  OVER_FOLDERED
Application/Commands/MergeCartAfterLogin/         (1 file)  OVER_FOLDERED
Application/Commands/RemoveCartLine/              (2 files)
Application/Queries/             <-- top-level technical axis
Application/Queries/GetCart/                      (2 files)
Application/Queries/GetCurrentCart/               (1 file)  OVER_FOLDERED
```

`Cart` is a **single-capability** module (the storefront shopping cart), so the canonical target is a
plural capability folder with secondary axes under it — matching `AddressBook/Addresses/`,
`Offer/Offers/`, `UserPreference/LocalePreferences/`, `Wishlist/Customer/`,
`BulkInquiry/Storefront/`.

---

## G6 — `Physical-Copy-State = STALE_COPY`: comment-only tombstone file

`Tooba.Cart.Application/Models/CartPage.cs` declares no types; it exists only to keep a historical
path discoverable. The manifest does not need it; the guard does not need it. It is a stale physical
copy of a location that no longer owns anything.

---

## G7 — `Contracts-Boundary-State`: multi-responsibility bundle + semantically wrong folder

`Tooba.Cart.Contracts/Checkout/CartContracts.cs` declares 11 types across three capability families:

| Family | Types |
| --- | --- |
| Cart lifecycle | `CartStatus`, `CartAccessKind`, `CartConversionIntent`, `CartAccess` |
| Cart read model | `CartLineAvailabilityKind`, `CartLineSnapshot`, `CartSnapshot`, `ICartQueryGateway` |
| Checkout conversion | `CartConversionRequest`, `CartConversionResult`, `ICartConversionPort` |

The folder is named `Checkout`, which is semantically wrong for the lifecycle types. The
`Presentation/` sibling (`CartPresentationContracts.cs`) is the correct shape.

**Constraint for W2:** external consumers import the **root** namespace
(`using Tooba.Cart.Contracts;`) — `Order.Contracts`, `Order.Application`, `Order.Infrastructure`,
`Catalog.Application`, `Fulfillment.Endpoints`, Host `HostOrderStorefrontActor`, `Tooba.Host.Tests`.
Any split must either keep every type's namespace stable or update all consumers. W2 must pick the
smaller, evidence-backed option and must not silently break the root-namespace contract.

---

## G8 — `Path-Namespace` / allowlist drift: `Contracts/Lifetime/` not in the architecture guard

`CartArchitectureGuardTests.AllowedContractsFolders = ["Checkout", "Presentation"]` — `Lifetime` is
missing, so `Cart_golden_boundaries_and_physical_layout_remain_clean` is **RED at HEAD**:

```text
Assert.Contains() Failure: Item not found in collection
Collection: ["Checkout", "Presentation"]
Not found:  "Lifetime"
```

The folder is legitimate; the guard is stale. Repair the guard to its documented intent.

---

## Non-gaps (verified canonical — no action)

| Concern | Verdict |
| --- | --- |
| API result mapping | `CANONICAL` — 7/7 routes via `ApiResponseFactory`, zero raw results |
| Localization text | `CANONICAL` — no hard-coded user-facing text; no `exception.Message` as contract |
| Logging | `CANONICAL` — `ILogger<T>` only; no `Console`/`Debug` |
| Sensitive data | `NONE` — guest secret never logged; only SHA-256 hash persisted |
| OpenTelemetry | `CANONICAL` — no second source/meter, no `StartActivity` |
| Correlation | `CANONICAL` — no parallel mechanism |
| CQRS | `COMPLIANT` — 7 requests, 7 handlers, 7 `ISender` sites |
| Validators | `EXHAUSTIVE` — 4 required + 3 not-required, guarded |
| Cross-module joins | `NONE` |
| Persistence ownership | `CORRECT` — own schema `cart`, own migrations |
| Endpoint ownership | `MODULE_OWNED` — 7 routes, Host zero |
| Schema | `UNCHANGED` — 3 migrations untouched |
| `CartErrorCodes` in `Application.Errors` | `CANONICAL_BY_SIBLING_PRECEDENT` — `Payment` does the same |
