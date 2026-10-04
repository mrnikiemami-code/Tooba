# Offer validation — W0

## Baseline (pre-change)

Command:

```
dotnet build src/backend/Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj
dotnet test  src/backend/Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj --no-build
```

Result:

```
Build succeeded.  0 Warning(s)  0 Error(s)
Failed: 2, Passed: 77, Skipped: 0, Total: 79
```

## Pre-existing RED (certification drift — disclosed, not hidden)

### RED-1 `OfferPhysicalStructureGuardTests.ARCH_MODULE_PHYSICAL_001_offer_routes_absent_from_host_seller_endpoints`

```
Assert.DoesNotContain() Failure: Sub-string found
  at OfferPhysicalStructureGuardTests.cs:line 193
```

Guard text:

```csharp
var moduleEndpoints = Path.Combine(OfferRoot(), "Tooba.Offer.Endpoints", "Seller", "OfferSellerEndpoints.cs");
var text = File.ReadAllText(moduleEndpoints);
Assert.DoesNotContain("MapGet(\"/offers\"", text, StringComparison.Ordinal);
Assert.DoesNotContain("MapPost(\"/offers\"", text, StringComparison.Ordinal);
Assert.DoesNotContain("MapPatch(\"/offers/", text, StringComparison.Ordinal);
```

The guard is named `..._offer_routes_absent_from_host_seller_endpoints` and its own comment says *"no Host seller panel endpoint can own Offer routes"*, yet it asserts on the **module's own** `OfferSellerEndpoints.cs` — which legitimately owns those routes and must contain them (`OfferArchitectureGuardTests.Host_seller_panel_does_not_map_offer_routes` asserts `Assert.Contains("MapGet(\"/offers\"", offerEndpoints)`). The two guards are mutually contradictory. The `Host/Seller` folder is absent (asserted at line 185). Verdict: **stale guard intent**, not a product defect.

### RED-2 `OfferArchitectureGuardTests.Host_owns_no_offer_selection_policy_or_hidden_offer_alias`

```
System.InvalidOperationException : Sequence contains no matching element
  at OfferArchitectureGuardTests.cs:line 393
```

Guard text:

```csharp
var composer = hostSources.Single(x => x.Path.EndsWith(
    "Host/Tooba.Host/Storefront/StorefrontComposer.cs".Replace('/', Path.DirectorySeparatorChar),
    StringComparison.Ordinal));
```

`src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs` no longer exists. Storefront composition moved to `Modules/Catalog/Tooba.Catalog.Infrastructure/Storefront/StorefrontComposer.cs`, which the **same guard file already validates** at line 566 (`Extracted_host_surfaces_use_offer_query_gateway_not_persistence`). Verdict: **stale guard path**, not a product defect.

## Why this is in scope

`COMPLETE_REFERENCE_PATTERN` requires `..._GUARDS_SOT`. A reference module whose own architecture guards are RED cannot be certified. Repairing stale guard paths/intent (without weakening the invariant) is part of the AMSC run. Neither repair removes an assertion; RED-1 is retargeted to the Host path it names, RED-2 is retargeted to the composer's current module path.

## Post-change target (W1/W2/W3)

| Gate | Target |
| --- | --- |
| Offer build | 0 warnings / 0 errors |
| Offer tests | 0 failed (≥ 79 passed, plus new structure/cohesion guards) |
| Host build | 0 errors |
| Host.Tests (Offer/return-policy/checkout/seller-grid surfaces) | 0 failed |
| Order tests (`IReturnPolicyResolver` consumer) | 0 failed |
| Schema/migrations | unchanged |
| Foreign project references added | none |

## Focused (no broad solution-wide run)

Per the Structure skill's "Focused Validation" rule, only structure guards, affected project builds, the module's own test project, and the directly-affected consumer test projects are run.
