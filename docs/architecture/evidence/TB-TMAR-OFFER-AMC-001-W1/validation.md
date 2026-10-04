# TB-TMAR-OFFER-AMC-001 — W1 VALIDATION

## 1. Build

```text
dotnet build src/backend/Tooba.slnx -c Debug
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Full backend solution build (not just the Offer project) so every consumer of the moved Offer types is
compiled: Offer, Order, Cart, Catalog, Inventory, Pricing, Party, Promotion, Payment, Fulfillment, Host,
MigrationRunner and their test projects.

## 2. Offer module tests

```text
dotnet test src/backend/Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj
Passed! - Failed: 0, Passed: 80, Skipped: 0, Total: 80
```

Baseline at W0 was `Failed: 2, Passed: 77, Total: 79`. After W1:

| Metric | W0 | W1 |
| --- | --- | --- |
| Total | 79 | 80 |
| Passed | 77 | 80 |
| Failed | 2 | 0 |

`80 = 79 baseline + 1 new folder-granularity guard`; both pre-existing RED guards now pass. No guard was
skipped, deleted, or weakened.

## 3. Dependent module tests

```text
dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj
Passed! - Failed: 0, Passed: 143, Skipped: 0, Total: 143
```

Order consumes `IReturnPolicyResolver` through `Tooba.Offer.Contracts` only, so it is the primary
regression witness for the cohesion split.

## 4. Host tests

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj
Failed! - Failed: 93, Passed: 1727, Skipped: 130, Total: 1950
```

### Every one of the 93 failures is pre-existing and unrelated to Offer

Verified by three independent checks against the untouched `HEAD` tree:

1. **Namespace failure inside a foreign module.**
   `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
   fails at `Tooba.Catalog.Contracts/Cart/*.cs`: expected `Tooba.Catalog.Contracts.Cart`, actual
   `Tooba.Catalog.Contracts`. Identical text at `HEAD` (`git show HEAD:...CatalogCartQuantityContracts.cs`),
   and `git status` shows **no** change under `src/backend/Modules/Catalog`.
2. **Content domain contract faults.** `ContentCategoryTreeRules` already throws
   `ContractOperationException` at `HEAD` (introduced by `c4c44dad`), while
   `ContentCategoryTreeRulesTests` still expects `InvalidOperationException` — untouched by this wave.
3. **Manifest / slnx / baseline drift.** `TmarSourceSizeAndInfraAppTests`,
   `TmarCompleteReferenceStructureGateTests.Manifest_...` (a `Catalog` entry present in the manifest but
   absent from the gate's expected list), `HostAdminAmc*` Host-count guards, `CustomerProfile` solution
   grouping, and the fuzz/integration failures all reproduce on files that this wave never touched.

Positive signal specific to Offer: **no** failing Host test name contains `Offer`, `ReturnPolicy`,
`ContractsW6` or `OfferReturnPolicyResolver` — the Host tests that exercise the Offer return-policy surface
pass.

Full Host-test repair is outside this task's scope (Offer module). The 93 failures are recorded here
honestly as pre-existing repository debt, not as an Offer regression.

## 5. Behavior-preservation spot checks

```text
routes in OfferSellerEndpoints.cs        = unchanged (GET/POST /offers, GET/PATCH /offers/{id:guid}, price, inventory)
17 Offer error codes                     = unchanged (OfferErrorCatalogContributor)
22 validation codes                      = unchanged (OfferValidationCodes)
offer schema / migrations                = unchanged (3 migrations, no new file)
OfferDbContext                           = unchanged
```

`git status` under `Tooba.Offer.Infrastructure/Persistence` shows no modification.

## 6. Final state

```text
MIGRATION_STATE              = COMPLETE
BEHAVIOR_PRESERVED           = true
OFFER_TESTS                  = 80/80
ORDER_TESTS                  = 143/143
FULL_SOLUTION_BUILD          = 0 errors
TECHNICAL_AXIS_FIRST_APP     = REMOVED
MULTI_RESPONSIBILITY_CONTRACTS_FILE = SPLIT
STRUCTURE_HANDOFF            = REQUIRED (W2)
```
