# TB-TMAR-CART-AMSC-001-W1 — Migrate (AMSC Wave 1)

- Task: `TB-TMAR-CART-AMSC-001-W1`
- Mode: `IMPLEMENTATION`
- Skill: `.cursor/skills/tooba-architecture-migrate/SKILL.md`
- Target: `src/backend/Modules/Cart/Tooba.Cart.*`
- Branch: `main`
- Predecessor: `TB-TMAR-CART-AMSC-001-W0` (`docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W0/`)
- W0 disposition: `READY_TO_MIGRATE`

## Closed findings

W0 raised four in-scope blockers (F1/F2 fault identity, F3 localization ownership, F4 unused
references, F5 `ARCH-SIZE-001`) plus one stale durable guard (F13). W1 closes all five.
Structural flattening (F7/F9/F10) is deliberately deferred to W2 (Structure).

| Finding | State before | State after |
| --- | --- | --- |
| F1 `Stable-Error-Code-State` | `STRING_HEURISTIC` — `CartExceptionMapper` classified by exact match on `InvalidOperationException.Message` whose value *was* the machine code | `TYPED` — every throw site is `SemanticException(new SemanticError(<code>))`; the mapper is deleted |
| F2 code-alias duplication | 18 legacy code strings duplicated between throw sites and the mapper alias table | `SINGLE_SOURCED` — `CartErrorCodes` in Contracts is the only code authority |
| F3 `Localization-State` (shared key) | `checkout.authentication_required` declared in Cart resx + `Owns(...)` | preserved, now documented as the only registered resolver (see §6) |
| F4 microservice-extractability | 2 **unused** declared foreign refs in `Cart.Application` | removed; the real consumers declared where they are used (§5) |
| F5 `ARCH-SIZE-001` | `CartDirectory.cs` = **831** LOC (live `NEW_OVERSIZED_FILE`) | decomposed into 4 cohesive collaborators; largest = **602** LOC (§4) |
| F13 stale guard | `AllowedContractsFolders` missing `Lifetime` → RED at HEAD | GREEN; `Composition`/`Errors` added (§7) |

## 1. Fault → Result composition (new canonical seam)

New file:

```text
Tooba.Cart.Application/Composition/CartOperation.cs
```

Mirrors `AddressBookOperation` / `UserPreferenceOperation` and the 15 further sibling modules that
already carry this seam. Two members:

| Member | Contract |
| --- | --- |
| `ExecuteAsync<T>(Func<Task<T>>)` | `SemanticException` → `Result.Failure<T>(ex.Error)`; anything else propagates to the global boundary |
| `ExecuteAsync(Func<Task>)` | same for the non-generic `Result` |

There is deliberately **no** `NotFoundIfNull` member: Cart's missing-cart outcome already has a
dedicated stable code (`cart.missing`) raised by the directory, so the null→code decision does not
need a second Application-level seam.

## 2. Handler contract change (7/7)

Every handler body became a single `CartOperation.ExecuteAsync(...)` call around the unchanged
`ICartDirectory` invocation. No handler gained business logic; the directory port signatures are
unchanged, so `ICartConversionPort` (consumed by `Order.Application`) and `ICartQueryGateway`
(consumed by Host storefront actors) are untouched.

| Request | Before | After |
| --- | --- | --- |
| `CreateGuestCartCommand` | `CartExceptionMapper.TryAsync(...)` | `CartOperation.ExecuteAsync(...)` |
| `AddCartLineCommand` | `CartExceptionMapper.TryAsync(...)` | `CartOperation.ExecuteAsync(...)` |
| `ChangeCartLineQuantityCommand` | `CartExceptionMapper.TryAsync(...)` | `CartOperation.ExecuteAsync(...)` |
| `RemoveCartLineCommand` | `CartExceptionMapper.TryAsync(...)` | `CartOperation.ExecuteAsync(...)` |
| `MergeCartAfterLoginCommand` | `CartExceptionMapper.TryAsync(...)` | `CartOperation.ExecuteAsync(...)` |
| `GetCartQuery` | `CartExceptionMapper.TryAsync(...)` | `CartOperation.ExecuteAsync(...)` |
| `GetCurrentAuthenticatedCartQuery` | `CartExceptionMapper.TryAsync(...)` | `CartOperation.ExecuteAsync(...)` |

The endpoint layer (`api.From(result)` / `api.FromFailure(...)`) is **unchanged** — W0 already
certified `API-Result-Pattern-State = CANONICAL` for Cart (7/7 routes via `ApiResponseFactory`). W1
changed only how the `Result` is *produced*, not how it is *projected*.

## 3. Typed faults and stable codes

### 3.1 Code authority (`Tooba.Cart.Contracts/Errors/CartErrorCodes.cs`)

`CartErrorCodes` moved from `Tooba.Cart.Application.Errors` to `Tooba.Cart.Contracts.Errors` — the
same placement AddressBook used in its W1. The 13 pre-existing values are **byte-identical**; 13 new
stable codes were added for outcomes that previously had no code at all (they surfaced as
`500 platform.unexpected`):

```text
cart.user_id.required                    (was: InvalidOperationException prose site)
cart.guest_secret.hash_required
cart.line.merge_via_quantity
cart.convert.order_required
cart.assign.guest_only
cart.expiry.future_required
cart.expiry.after_created
cart.market.required
cart.currency.invalid
cart.pricing.quote_missing
cart.commerce.context_unavailable
cart.commerce.market_unconfigured
cart.commerce.currency_unconfigured
cart.commerce.channel_unconfigured
```

`checkout.authentication_required` remains a **consumed** shared code and is still **not**
re-registered by `CartErrorCatalogContributor` (preserved from W0 §G1).

### 3.2 Domain

| File | Sites | Change |
| --- | --- | --- |
| `Domain/Aggregates/ShoppingCart.cs` | 15 | `InvalidOperationException("<code>")` → `SemanticException(new SemanticError(CartErrorCodes.<X>))` |
| `Domain/Entities/CartLine.cs` | 2 | same |

`Tooba.Cart.Domain.csproj` gained `Tooba.Cart.Contracts` — the **same legal edge already used by
AccessControl, Wishlist, Identity, Offer, AddressBook and others** (Domain raising module-owned typed
faults). It is a boundary dependency on the module's *own* Contracts, so
`Cross-Module-Coupling-State` remains `LEGAL_CONTRACTS_ONLY` and `microserviceExtractable` stays
`true` — the two projects move together.

### 3.3 Infrastructure

| File | Sites | Change |
| --- | --- | --- |
| `Directories/CartDirectory.cs` | 15 | typed `SemanticError`; the 3 bare `catch (InvalidOperationException)` sites became `catch (SemanticException)` |
| `Directories/CartLineCurrency.cs` | 1 | typed `SemanticError` |
| `Directories/CartQuoteValidator.cs` | 8 | typed `SemanticError` (extracted, see §4) |
| `Lifetime/CartCommerceContextResolver.cs` | 4 | typed `SemanticError` |

`Application/Presentation/CartPresentationComposer.cs`: the `throw` became typed and the
`catch (InvalidOperationException)` in `TryGetForOwnershipAsync` became `catch (SemanticException)`.
This is a **narrowing** of a previously over-broad catch: a genuinely unexpected
`InvalidOperationException` is no longer silently converted into `null`/404 — it now propagates to the
global boundary. That is the canonical fail-loud behavior and matches the sibling modules.

`Infrastructure/Messaging/CartOutboxRegistration.cs:130` keeps its
`InvalidOperationException("cart.outbox.unmapped_event")` — an unreachable framework-contract guard
for an event type the module never registers. It is not a business outcome, is not message-parsed
anywhere, and is recorded as residue (§10).

### 3.4 Catalog + resources

`CartErrorCatalogContributor` gained 13 descriptors (via the existing `D(...)` helper): 5 `Business`/400,
4 `Business`/409, 4 `Platform`/503. `CartErrors.resx` and `CartErrors.fa.resx` each went from **12**
keys to **26** keys, so every reachable stable code now has a real localized title in both cultures.

## 4. `ARCH-SIZE-001` decomposition (F5)

`CartDirectory.cs` went from **831** → **602** LOC. Three cohesive collaborators were extracted
without changing the public `ICartDirectory` / `ICartQueryGateway` signatures, the DI registration
identity, or the behavior:

| New file | LOC | Owns |
| --- | --- | --- |
| `Directories/CartQuoteValidator.cs` | 93 | offer resolution + effective quantity policy + price quote + inventory sellability; the only place a foreign quote contract becomes an accepted Cart line |
| `Directories/CartSnapshotProjector.cs` | 54 | Cart aggregate → `CartSnapshot` boundary projection; the only availability batch read |
| `Directories/CartExpiryScanner.cs` | 87 | expiry batch scan incl. the raw `FOR UPDATE SKIP LOCKED` SQL |
| `Directories/CartDirectory.cs` (retained) | 602 | mutation/read orchestration only |

`CartDirectory.cs` is now below the `thresholdNewFileLoc = 800` threshold, so it is **not** added to
`tmar-source-size-baseline.json` — the file was genuinely split rather than frozen.

## 5. Project reference correction (F4)

| Project | Before | After | Why |
| --- | --- | --- | --- |
| `Tooba.Cart.Application` | `Pricing.Contracts`, `Inventory.Contracts` declared but **never used** | both removed | W0 proved zero code references; `Application` keeps `Catalog.Contracts`, `Party.Contracts`, `Offer.Contracts`, `BuildingBlocks`, `Cart.Contracts`, `Cart.Domain` |
| `Tooba.Cart.Infrastructure` | relied on transitivity through `Application` | `Pricing.Contracts` declared explicitly | `CartQuoteValidator` consumes `IPriceLookupGateway` / `ICampaignCartPriceAuthority` / `PriceQuote` directly; a declared dependency surface now equals the actual one |

`Inventory.Contracts` was already declared on `Infrastructure` (the projector/scanner consume
`IInventoryAvailabilityGateway`) and is unchanged. No foreign `*.Application` / `*.Infrastructure` /
`*.Domain` edge was added or removed.

## 6. Shared `checkout.authentication_required` ownership (F3, preserved)

W0 §G1 asked W1 to *verify empirically* which resource set resolves the shared key and to make the
ownership explicit rather than removing the Cart key. Verified:

| Set | `Owns("checkout.authentication_required")` | Has the key in resx |
| --- | --- | --- |
| `FoundationErrorResourceSet` | ❌ (matches only `validation.*`, `platform.*`, `admin.*`) | ❌ |
| `OrderErrorResourceSet` | ✅ (`checkout.*`) | Order surface only |
| `CartErrorResourceSet` | ✅ (explicit `Equals` clause) | ✅ (`CartErrors.resx` + `.fa.resx`) |

The Cart key is therefore **load-bearing** for the Cart 401 response and was kept. The descriptor is
still owned solely by `FoundationErrorCatalogContributor`; Cart registers no descriptor for it. No
silent ownership race exists today because the sets' predicates are disjoint for this key.
`guardsWeakened = NONE`.

## 7. Durable guards

| Guard | Change | Justification |
| --- | --- | --- |
| `CartArchitectureGuardTests.AllowedApplicationFolders` | `Errors` → `Composition` | `Errors/` no longer exists in `Application` (the code catalog moved to Contracts); `Composition/` is the canonical fault-to-Result seam folder in 16 sibling modules. F13 repaired. |
| `CartArchitectureGuardTests.AllowedContractsFolders` | `["Checkout","Presentation"]` → `+ "Errors", "Lifetime"` | Restores the array's documented intent. `Lifetime/` was missing before this run (F13); `Errors/` is where `CartErrorCodes` now lives. |
| `CartArchitectureGuardTests` mapper test | `Cart_exception_mapper_is_stable_codes_only_without_prose_heuristics` → `Cart_typed_faults_use_contract_stable_codes_without_prose_heuristics` | The old test asserted a file that no longer exists. The replacement asserts **stronger** invariants: no `CartExceptionMapper`/`TryMapExact` may return, no message-`Contains` heuristic anywhere in production, no non-semantic throw in Domain/Application, and every handler maps through `CartOperation`. |
| `CartArchitectureGuardTests` project refs | `Assert.Contains(Inventory.Contracts)` on `Application` → `Assert.DoesNotContain` on `Application` | Reflects §5. No assertion was deleted; the guard is now strictly stronger about the Application boundary. |
| `CartPresentationAndErrorTests` | mapper tests → `CartOperation` fault→`Result` tests (4 tests) | Reflects the intentional seam change; the same invariant (stable code survives the boundary) is asserted for 13 codes. |
| `CartMulticurrencyTests` | `Assert.Throws<InvalidOperationException>` + `error.Message` → `Assert.Throws<SemanticException>` + `error.Error.Code` | Same invariant (fail-closed on missing line currency), typed fault. |
| `HostCartResidualGuardTests.Cart_owns_no_commerce_policy_default_and_consumes_platform_authority` | literal `"cart.commerce.market_unconfigured"` → `CartErrorCodes.CommerceMarketUnconfigured` | The literals now live once, in Contracts. The guard asserts the same three outcomes. |
| `CartLifetimeSeparationTests.Cart_add_does_not_call_ReserveAsync` | `GetAvailabilityBatchAsync` asserted on `CartDirectory.cs` → on the aggregated `Directories/Cart*.cs` set | The availability batch read moved to `CartSnapshotProjector`. The guard now aggregates the cohesive partial set (the file already provided `ReadAggregated` for exactly this purpose) instead of one file. |
| `CartFoundationTests` | `Assert.Contains("Tooba.Pricing.Contracts"|"Tooba.Inventory.Contracts", Application.csproj)` → asserted on `Infrastructure.csproj` | Reflects §5. |
| `CampaignCartPriceIntegrityTests`, `ContractsW6CharacterizationTests` | same retarget to `Infrastructure.csproj` | Reflects §5. |

No guard was deleted. No baseline was widened. `guardsWeakened = NONE`.

## 8. Deferred to W2 (Structure)

`Folder-Granularity-State = TECHNICAL_AXIS_FIRST` is **unchanged** in W1:

```text
Application/Commands/<UseCase>/…            5 leaves (3 single-file)
Application/Queries/<UseCase>/…             2 leaves (1 single-file)
Application/Models/CartPage.cs              comment-only tombstone (F9)
Contracts/Checkout/CartContracts.cs         11 types across 3 families, wrong folder semantics (F10)
manifest: 3 of 5 production projects listed (D1), empty forbiddenTopLevelFolders (D2)
```

W1 only changed the *content* of existing files in place, so the W2 move is a pure `git mv` +
namespace edit with no semantic merge.

## 9. Verification

```text
dotnet build src/backend/Tooba.slnx                       → 0 errors
dotnet test Tooba.Cart.Tests                              → 24 passed / 0 failed / 0 skipped
                                                            (W0 baseline was 22 passed / 1 failed;
                                                             F13 repaired, +2 tests, all green)
dotnet test Tooba.Host.Tests                              → 91 failed / 1761 passed / 130 skipped
                                                            (W0 baseline was 91 failed / 1761 passed / 130 skipped
                                                             — set-identical, zero Cart regressions)
```

The 5 Host tests that the intermediate build turned red were all Cart guards needing alignment with
the new canonical structure (§7); after alignment the failing-test set is **identical** to the
pre-existing baseline. The pre-existing 91 are repo-wide drift documented in W0 §4 (stale
`.tmp-baseline` worktree, Catalog/Story/Promotion/solution-grouping guards, Docker-skipped
Postgres tests) and are reported, not repaired.

Post-change re-scan of the module:

| Pattern | Hits |
| --- | --- |
| `CartExceptionMapper` / `TryMapExact` | **0** |
| `Tooba.Cart.Application.Errors` | **0** (namespace removed) |
| `InvalidOperationException` in Domain / Application | **0** |
| `catch (InvalidOperationException)` | **0** |
| `Pricing.Contracts` / `Inventory.Contracts` in `Application.csproj` | **0** |
| `.Message.Contains(` / `text.Contains(` in production | **0** |
| `exception.Message` / `ex.Message` used as a contract in Endpoints | **0** |
| `Results.Json` / `Results.BadRequest` / `Results.Problem` in Endpoints | **0** |
| Foreign `*.Application` / `*.Infrastructure` / `*.Domain` using | **0** |
| `CartDirectory.cs` LOC | 602 (< 800 threshold) |

## 10. Behavior change (explicit, bounded)

W0 flagged `Behavior-Preservation-Risk = MEDIUM_FOR_TYPED_FAULT_CONVERGENCE`. The client-visible
differences are confined to paths that were previously defective:

| Scenario | Before | After |
| --- | --- | --- |
| Domain/Infrastructure guard tripped (owner, expiry, market, currency, merge, adopt) | HTTP **500** `platform.unexpected` | HTTP **400/409** with the module's own localized code |
| Pricing returns no quote | HTTP **500** `platform.unexpected` | HTTP **409** `cart.pricing.quote_missing` |
| Store commerce context unresolved | HTTP **500** `platform.unexpected` | HTTP **503** `cart.commerce.*` |
| Unexpected `InvalidOperationException` inside `TryGetForOwnershipAsync` | silently `null` (404) | propagates to the global boundary (fail-loud) |
| All previously-mapped codes (`cart.missing`, `cart.version.conflict`, …) | mapped code | **same** mapped code, same HTTP status |

No route, schema, DTO, success payload, DI registration or port signature changed.

## 11. Invariants preserved

| Invariant | Status |
| --- | --- |
| Zero foreign Application/Infrastructure/Domain coupling | preserved |
| `ICartDirectory` / `ICartQueryGateway` / `ICartConversionPort` signatures | unchanged |
| Guest secret never logged; only SHA-256 hash persisted | preserved |
| `checkout.authentication_required` descriptor owned by Foundation only | preserved |
| Schema `cart`, 3 migrations, snapshot, indexes | untouched |
| Outbox registration, DbContext, DI composition | untouched |
| Host production files | **zero modified** |
| `HOST_FINAL_CLOSURE_REGRESSION` | `NONE` |
| `microserviceExtractable` | `true` |

## 12. Handoff

```text
W1 exit state: StableErrorCodeState    = TYPED_CONTRACT_SOURCED
               FaultMechanismState     = SEMANTIC_EXCEPTION_TO_CART_OPERATION
               LocalizationState       = CATALOGUED_26_KEYS_BOTH_CULTURES
               ApiResultPatternState   = CANONICAL (unchanged)
               ArchSize001State        = RESOLVED_602_LOC
               UnusedForeignRefsState  = ZERO
               FolderGranularityState  = TECHNICAL_AXIS_FIRST   (W2)
               structureHandoffState   = REQUIRED
```

`automaticNextTask = TB-TMAR-CART-AMSC-001-W2` (skill `tooba-architecture-structure`).
