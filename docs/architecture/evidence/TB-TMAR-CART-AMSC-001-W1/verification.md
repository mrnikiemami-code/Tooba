# TB-TMAR-CART-AMSC-001-W1 — Verification

## 1. Environment

| Item | Value |
| --- | --- |
| Branch | `main` |
| HEAD at start | `f1d98ec2` (Cart W0) |
| `origin/main` at start | `f1d98ec2` |
| HEAD == origin/main | ✅ |
| Solution | `src/backend/Tooba.slnx` |
| SDK | .NET 8 |
| Module | `src/backend/Modules/Cart/` |

## 2. Build

```text
dotnet build src/backend/Tooba.slnx -v q --nologo
→ Build succeeded. 0 Error(s)   (112 warnings, all pre-existing xUnit2013 in Tooba.Host.Tests)
```

The W0 F12 finding (`CS0105` duplicate `using Tooba.Cart.Contracts` in
`CartPresentationAndErrorTests.cs`) is **closed** by the rewrite of that file in this wave.

## 3. Cart test suite (authoritative gate)

```text
dotnet test src/backend/Modules/Cart/Tooba.Cart.Tests/Tooba.Cart.Tests.csproj -v q --nologo
→ Passed!  - Failed: 0, Passed: 24, Skipped: 0, Total: 24
```

| Wave | Result |
| --- | --- |
| W0 baseline | Failed **1**, Passed 22, Total 23 |
| W1 after | Failed **0**, Passed **24**, Total 24 |

Delta: F13 guard staleness repaired (`Cart_golden_boundaries_and_physical_layout_remain_clean`
GREEN), the obsolete mapper test replaced by 4 `CartOperation` contract tests, and the multicurrency
test retargeted to the typed fault.

### Test-by-test

| Test | Result |
| --- | --- |
| `CartArchitectureGuardTests.Cart_golden_boundaries_and_physical_layout_remain_clean` | ✅ (was ❌ F13) |
| `CartArchitectureGuardTests.Cart_endpoints_cqrs_and_host_ownership_are_enforced` | ✅ |
| `CartArchitectureGuardTests.Cart_typed_faults_use_contract_stable_codes_without_prose_heuristics` | ✅ (new) |
| `CartArchitectureGuardTests.Cart_line_currency_authority_and_per_currency_totals_cannot_regress` | ✅ |
| `CartPresentationAndErrorTests` (4) | ✅ |
| `CartMulticurrencyTests` (all) | ✅ |
| `CartEndpointValidatorCoverageGuardTests` (all) | ✅ |
| `CartEndpointOwnershipTests` (all) | ✅ |

## 4. Host suite (regression proof)

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -v q --nologo
→ Failed: 91, Passed: 1761, Skipped: 130, Total: 1982
```

### Baseline comparison (set-identical)

The failing-test **name set** was captured before and after the change and diffed:

```text
git stash push -u  → run Host tests → 91 failures (baseline set)
git stash pop      → run Host tests → 91 failures (after set)
Compare-Object baseline after → (empty)
```

`Compare-Object` returned **no differences**: the W1 change introduces **zero** Host regressions.
The 5 Cart guards that went red during the intermediate build
(`CartFoundationTests`, `CartLifetimeSeparationTests`, `CampaignCartPriceIntegrityTests`,
`ContractsW6CharacterizationTests`, `HostCartResidualGuardTests`) were realigned in §7 of `migrate.md`
and are green again.

## 5. Static scans (post-change)

| Scan | Result |
| --- | --- |
| `CartExceptionMapper` / `TryMapExact` in Cart | **0** |
| `Tooba.Cart.Application.Errors` namespace | **0** (folder deleted) |
| `InvalidOperationException` in `Cart.Domain` / `Cart.Application` | **0** |
| `catch (InvalidOperationException)` in Cart | **0** |
| `catch (Exception)` / empty catch in Cart production | **0** |
| `.Message.Contains(` / `text.Contains(` in Cart production | **0** |
| `throw new <X>Exception(` (non-`SemanticException`) in Domain/Application | **0** |
| Foreign `*.Application` / `*.Infrastructure` / `*.Domain` using in Cart | **0** |
| Foreign `DbContext` access in Cart | **0** |
| `Results.Json` / `Results.BadRequest` / `Results.Problem` in Endpoints | **0** |
| `exception.Message` / `ex.Message` as a contract in Endpoints | **0** |
| `Guid.NewGuid()` as a correlation id | **0** |
| `DateTimeOffset.UtcNow` / `DateTime.UtcNow` in Cart production | **0** |
| `StartActivity(` / `new ActivitySource` / `new Meter` in Cart | **0** |

## 6. Size gate

| File | W0 LOC | W1 LOC | Verdict |
| --- | --- | --- | --- |
| `Infrastructure/Directories/CartDirectory.cs` | 831 | **602** | under the 800 threshold; **not** baselined |
| `Infrastructure/Directories/CartQuoteValidator.cs` | — | 93 | new, cohesive |
| `Infrastructure/Directories/CartExpiryScanner.cs` | — | 87 | new, cohesive |
| `Infrastructure/Directories/CartSnapshotProjector.cs` | — | 54 | new, cohesive |
| `Infrastructure/Directories/CartLineCurrency.cs` | 34 | 34 | unchanged |

No entry was added to `tmar-source-size-baseline.json`. The `NEW_OVERSIZED_FILE` classification for
`CartDirectory.cs` is resolved by real decomposition, not by freezing.

## 7. Wave gate

| Wave | Gate | State |
| --- | --- | --- |
| W0 | evidence, SoT checkpoint, commit + push | ✅ `f1d98ec2` |
| **W1 (this)** | Cart tests **24 passed / 0 failed**; build 0 errors; `CartDirectory.cs` < 800 LOC; unused refs removed; typed faults contract-sourced; Host failing set **not widened**; commit + push | ✅ |
| W2 | capability-first `Application/Cart/{Commands,Queries,Validators}`; folder-granularity `PROFESSIONAL_SHALLOW`; `Models` tombstone deleted; manifest reconciled; commit + push | ⏳ |
| W3 | `Cart` re-certified `COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002`; durable cert guard green; SoT closure; commit + push | ⏳ |

## 8. Recovery status

`HEAD == origin/main` at wave start. Working tree contained this wave's changes plus the three
pre-existing untracked foreign artifacts (W0 F15), which are **not** committed by this run.

No `RECOVERY_CONFLICT`.
