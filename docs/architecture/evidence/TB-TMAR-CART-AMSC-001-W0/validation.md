# TB-TMAR-CART-AMSC-001-W0 — Validation

## 1. Environment

| Item | Value |
| --- | --- |
| Branch | `main` |
| HEAD at start | `17e95804aa0f261a2a98f55278f671aafd5e41d9` |
| `origin/main` at start | `17e95804aa0f261a2a98f55278f671aafd5e41d9` |
| HEAD == origin/main | ✅ |
| Solution | `src/backend/Tooba.slnx` |
| SDK | .NET 8 |
| Module | `src/backend/Modules/Cart/` |

## 2. Build baseline

Cart projects are part of `src/backend/Tooba.slnx`. The Cart test project builds with **0 errors**
and **1 warning** attributable to Cart:

```text
warning CS0105: The using directive for 'Tooba.Cart.Contracts' appeared previously in this namespace
  → Tooba.Cart.Tests/Behavior/CartPresentationAndErrorTests.cs
```

Recorded as finding F12 (cheap hygiene, W1).

## 3. Cart test baseline (reproduced at this HEAD)

```text
dotnet test src/backend/Modules/Cart/Tooba.Cart.Tests/Tooba.Cart.Tests.csproj -v q --nologo
→ Failed: 1, Passed: 22, Skipped: 0, Total: 23, Duration: 75 ms
```

| Test | Result |
| --- | --- |
| `CartArchitectureGuardTests.Cart_golden_boundaries_and_physical_layout_remain_clean` | ❌ **FAIL** (F13) |
| `CartArchitectureGuardTests.Cart_endpoints_cqrs_and_host_ownership_are_enforced` | ✅ PASS |
| `CartArchitectureGuardTests.Cart_exception_mapper_is_stable_codes_only_without_prose_heuristics` | ✅ PASS |
| `CartArchitectureGuardTests.Cart_line_currency_authority_and_per_currency_totals_cannot_regress` | ✅ PASS |
| `CartEndpointValidatorCoverageGuardTests` (all 7) | ✅ PASS |
| `CartEndpointOwnershipTests` (all) | ✅ PASS |
| `CartMulticurrencyTests` (all) | ✅ PASS |
| `CartPresentationAndErrorTests` (all) | ✅ PASS |

### Failure detail (reproduced, deterministic)

```text
Failed Tooba.Cart.Tests.Architecture.CartArchitectureGuardTests.Cart_golden_boundaries_and_physical_layout_remain_clean
  Assert.Contains() Failure: Item not found in collection
  Collection: ["Checkout", "Presentation"]
  Not found:  "Lifetime"
     at CartArchitectureGuardTests.AssertNoRootDump(...) line 296
     at CartArchitectureGuardTests.Cart_golden_boundaries_and_physical_layout_remain_clean() line 49
```

Root cause: `AllowedContractsFolders` was never updated when
`Tooba.Cart.Contracts/Lifetime/ICartPersistenceHoursSource.cs` was added. Guard **staleness**, not an
architecture regression — the same file's namespace (`Tooba.Cart.Contracts.Lifetime`) passes
`AssertNamespacesAlign`. W1/W2 repair the array to its documented intent without weakening the
assertion.

## 4. Pre-existing repo-wide gate baseline (proven pre-existing, out of scope)

```text
dotnet test Tooba.Host.Tests --filter FullyQualifiedName~TmarSourceSizeAndInfraAppTests
→ 3 failures, all pre-existing at this HEAD
```

| Test | Classification |
| --- | --- |
| `Hand_written_source_size_does_not_expand_beyond_baseline` | pre-existing: (a) stale sibling worktree `.tmp-baseline` scanned, (b) **Cart `CartDirectory.cs` 831 LOC (F5, IN SCOPE)**, (c) unrelated Catalog/Order/Payment drift |
| `Source_size_inventory_evidence_exists_and_matches_scan_count` | pre-existing (stale `.tmp-baseline` + inventory evidence) |
| `Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline` | pre-existing (`Tooba.Promotion.Infrastructure -> *`, unrelated module) |

Only the **Cart** contributor (F5) is in scope. The `.tmp-baseline` scan artifact, the Promotion edges
and the inventory-count drift are repo-wide pre-existing drift and are reported, not repaired
(bounded scope).

## 5. Static scans performed

| Scan | Result |
| --- | --- |
| Foreign `using Tooba.<Module>.Application\|Infrastructure\|Domain` in Cart | **0** |
| Foreign `DbContext` / `DbSet` access in Cart | **0** |
| Cross-module SQL/EF join | **0** (only raw SQL is `cart.carts`, own schema) |
| `Tooba.Pricing.Contracts` / `Tooba.Inventory.Contracts` usage in `Cart.Application` | **0** — 2 UNUSED declared refs (F4) |
| `Results.Json` / `Results.BadRequest` / `Results.Problem` in Cart.Endpoints | **0** (all `api.From(...)`) |
| `exception.Message` / `ex.Message` used as a classification or user-facing contract in Endpoints | **0** |
| Hard-coded Persian user-facing text in production C# | **0** |
| `Console.` / `Debug.Write` / `new ActivitySource` / `new Meter` / `AsyncLocal` / `traceparent` | **0** |
| `Guid.NewGuid()` used as a correlation id | **0** |
| Root `.cs` files | 5, all allowlisted (`GlobalUsings.*` ×4, `CartEndpointModule.cs` ×1) |
| Single-file use-case leaf folders under `Application/` | 5 (F7) |
| Duplicate physical type declarations | **0** (3 intentional Contracts/Domain enum projection pairs, 1 DI alias port F6) |
| Cart entries in `tmar-source-size-baseline.json` | **0** — `CartDirectory.cs` is a live `NEW_OVERSIZED_FILE` (F5) |
| `Cart` manifest project coverage | 3 of 5 production projects (F-manifest) |

## 6. Wave gates

| Wave | Gate |
| --- | --- |
| W0 (this) | evidence written, SoT checkpoint, commit + push |
| W1 | Cart tests **23 passed / 0 failed**; build 0 errors / 0 Cart warnings; `CartDirectory.cs` < 800 LOC; 2 unused refs removed; pre-existing gate set **not widened** |
| W2 | capability-first `Application/Cart/{Commands,Queries,Validators}`; folder-granularity `PROFESSIONAL_SHALLOW`; `Models` tombstone deleted; manifest + durable guard updated honestly; `CartArchitectureGuardTests` green |
| W3 | `Cart` re-certified `COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002`; durable cert guard green; SoT closure with all wave SHAs |

## 7. Recovery status

`HEAD == origin/main`. Working tree contains this wave's new evidence files plus the three
pre-existing untracked foreign artifacts (F15), which are **not** committed by this run.
No `RECOVERY_CONFLICT`.
