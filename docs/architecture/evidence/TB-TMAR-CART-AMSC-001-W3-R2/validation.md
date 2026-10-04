# TB-TMAR-CART-AMSC-001-W3-R2 — Validation

- Starting HEAD: `b5721b0f10c9f79319f329d7b97df9153f02eee9` (`== origin/main`)
- Mode: `BOUNDED_EXPECTED_FAILURE_CATALOG_REPAIR`

## Precheck (re-discovery)

- `HEAD == origin/main`: **YES** (`b5721b0f`).
- `CartErrorCodes.LineCurrencyMissing` value re-discovered: `cart.line.currency_missing` (unchanged).
- Production throw/use sites re-discovered (reachable, fail-closed):
  - `Tooba.Cart.Application/Presentation/CartPresentationComposer.cs:90`
  - `Tooba.Cart.Infrastructure/Directories/CartLineCurrency.cs:33`
- Both resx keys present: `CartErrors.resx` (en), `CartErrors.fa.resx` (fa).
- Contributor descriptor count before edit: **25**.
- `SafeErrorMapper` fallback confirmed: unknown code -> `MapUnexpected()` (`platform.unexpected`, HTTP 500).
- Semantic convention confirmed from the repo (Cart + Pricing): `Business/409` for missing-price-like line truth (`cart.pricing.quote_missing`); `Business/400` for invalid client input; `Platform/503` for unresolved store commerce.
- No material divergence -> no `RECOVERY_CONFLICT`.

## Bounded validation executed

| Check | Command | Result |
| --- | --- | --- |
| Focused Cart tests | `dotnet test Modules\Cart\Tooba.Cart.Tests\Tooba.Cart.Tests.csproj` | **PASS** 27 passed / 0 failed / 0 skipped |
| Cart W3 cert guard + error-catalog unique guard | `dotnet test Host\Tooba.Host.Tests --filter "CartModuleAmsc001W3CertGuardTests\|ErrorCatalogUniqueCodeGuardTests"` | **PASS** 15 passed / 0 failed / 0 skipped |
| SoT JSON parse | `node -e "require('./docs/architecture/tmar-current-state.json')"` | **OK** |
| Manifest JSON parse | `node -e "require('./docs/architecture/tmar-module-structure-manifests.json')"` | **OK** |
| Descriptor count after edit | grep `^\s*D\(CartErrorCodes\.` in contributor | **26** |
| `LineCurrencyMissing` occurrences in contributor | grep | **1** |

## `git diff` proof — bounded delta

Modified files:

| File | Kind |
| --- | --- |
| `src/backend/Modules/Cart/Tooba.Cart.Endpoints/Errors/CartErrorCatalogContributor.cs` | **production (exactly one)** |
| `src/backend/Host/Tooba.Host.Tests/Architecture/CartModuleAmsc001W3CertGuardTests.cs` | focused guard |
| `docs/architecture/tmar-current-state.json` | SoT |
| `docs/architecture/tmar-module-structure-manifests.json` | manifest `certificationNote` only |

Production `.cs` files changed: **exactly 1**.

Zero change to:

- routes / endpoints — `UNCHANGED`
- DTO shapes — `UNCHANGED`
- error-code **values** — `UNCHANGED`
- resx resources — `UNCHANGED` (existing keys reused)
- schema / migrations — `UNCHANGED`
- manifest structural fields (`projects`, `rootAllowlist`, `forbiddenRootFiles`, `forbiddenTopLevelFolders`, `structureCertified`, `lockVersion`) — `UNCHANGED`
- cross-module boundary — `CONTRACTS_ONLY`
- foreign Application/Infrastructure/Domain coupling — `ZERO`

## Count truth (after)

- declaredOrConsumedCodeCount = **27**
- Cart registered/owned descriptors = **26**
- `checkout.authentication_required` = Foundation-owned, consumed-not-registered
- duplicate descriptor ownership = **ZERO**
- unregistered reachable Cart-owned codes = **ZERO**

## Unrelated artifacts preserved

Pre-existing untracked artifacts (`TB-TMAR-ORDER-AMC-001-W5-R1/worker-result.txt`, `TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/*`, `TB-TMAR-CART-AMSC-001-W3-R1/RESULT.bridge.txt`, `TB-TMAR-CART-AMSC-001-W3-R1/post-result.js`) are **not** part of this task and were left untouched.

## Result

`PASS` — `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` STRUCTURE_CERTIFIED. One bounded expected-failure catalog repair; no behavior change beyond the previously-defective expected-failure path now resolving to its typed `Business/409` mapping instead of `platform.unexpected` 500.
