# TB-SHIPPING-CHECKOUT-BINDING-049 — Shipping → Checkout binding seam (milestone 4/6)

Status: **PARTIAL** (compilable, focused-test-verified, contract-safe slice; live cutover explicitly **BLOCKED**)
Base-SHA: `426a1cb488cb54186f717e3a01428caf8d6d7f6b`

## 1. What the live checkout does today (unchanged)

`StorefrontShippingService.LoadEligibleMethodsAsync` reads the Fulfillment service/option catalogue
(`IShippingCatalogReader`), filters it by `ShippingMethodRegistry.Enabled(ShippingMethodsOptions)`, and
prices each entry through `StorefrontShippingCalculator`:

```csharp
        var rate = StorefrontShippingCalculator.ResolveRate(methodCode, _shippingOptions);
        if (!StorefrontShippingCalculator.IsDestinationAllowed(rate, provinceName)
            && !string.IsNullOrWhiteSpace(provinceName))
        {
            return;
        }

        var price = StorefrontShippingCalculator.QuotePrice(rate, cartSubtotal);
        result.Add(new StorefrontShippingMethodView(
            methodCode,
            label,
            serviceCode ?? methodCode.Split(':')[0],
            iconKey,
            price,
            rate.LeadDays,
            price == 0m,
            rate.LeadDays == 0 ? "آماده امروز" : $"حدود {ToPersianLeadDays(rate.LeadDays)} روز"));
    }
```

The chosen price/label/lead days are then snapshotted into `CartShippingDraft` and replayed into
`CheckoutSnapshot` on commit. **This path is byte-for-byte untouched by this task**: no constructor, DI,
catalogue, draft, snapshot, payment-allocation or endpoint surface was modified.

## 2. What was implemented (IMPLEMENTED)

New Order-owned, Contracts-only observation seam under
`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/RateAuthority/`:

| File | Role |
| --- | --- |
| `StorefrontShippingRateAuthorityOptions.cs` | Two independent, fail-safe flags: `ShadowEnabled` (default **false**) and `PrerequisitesConfirmed` (default **false**). |
| `StorefrontShippingRateAuthorityPolicy.cs` | `StorefrontShippingRateAuthorityGate` enum + policy gate. Only `Disabled` / `PrerequisitesUnconfirmed` / `Allowed`. |
| `StorefrontShippingRateShadowContracts.cs` | `StorefrontShippingRateShadowInput`, `…Outcome`, `…Report`. |
| `StorefrontShippingRateShadowComparer.cs` | Pure comparator producing explicit outcomes. Never returns anything that can influence price. |
| `StorefrontShippingRateShadowEvaluator.cs` | Gate → build `ShippingQuoteRequest` → call Shipping's `IShippingRateQuoteLookup` → compare. Fail-safe. |

Design properties that satisfy the mission:

* **Shipping remains commercial rate authority.** Order only calls `IShippingRateQuoteLookup` (a
  `Tooba.Shipping.Contracts` port) and never re-implements rate rules. No Shipping `DbContext` or
  domain type is touched, and no Fulfillment CRUD/identity/display is duplicated.
* **Two-key gate (mission B).** Comparing anything requires `ShadowEnabled` **and**
  `PrerequisitesConfirmed`. Setting only the enable flag cannot bypass the prerequisite claim, because
  empty coverage is never read as "unrestricted" — the gate simply refuses and the reason is recorded.
* **No silent fallback.** The evaluator never substitutes a Shipping amount for a legacy amount. Its
  only outputs are a `…Report`; there is no code path from the report back into `CartShippingDraft`,
  `CheckoutSnapshot` or the payment allocation.
* **Fail-closed on absence (mission D4).** A missing `shipping` schema, missing table, connection error
  or any other non-cancellation exception degrades to `ShippingUnavailable`; nothing propagates to the
  live path. `OperationCanceledException` is still honoured.
* **No destination guessing.** `ShippingQuoteRequest` needs `DestinationIdentifiers`; the live legacy
  path only has localised province/city *strings* and ADR-036 Phase 2 / D8.4 text→id resolution is not
  authorized. Where identity is absent the outcome is explicitly `NotComparable`, never an inferred
  destination.
* **Expected coverage gap is reported, not absorbed.** Because coverage grants are empty while the new
  engine is fail-closed, `legacy allowed / shipping NotCovered` maps to the dedicated
  `LegacyAllowedShippingDenied` outcome for explicit reporting rather than being coerced to `Match`.

## 3. Focused tests (IMPLEMENTED, executed)

`src/backend/Modules/Order/Tooba.Order.Tests/Storefront/StorefrontShippingRateShadowTests.cs` — **8/8 passed**.

| Mission requirement | Test |
| --- | --- |
| D1 — legacy price/promise unchanged while the switch is disabled | `Gate_Is_Disabled_By_Default_And_Legacy_Price_Is_Not_Touched`, `Enabled_Shadow_Without_Confirmed_Prerequisites_Still_Does_Not_Query_Shipping` |
| D2 — unknown/unsupported destination is not silently covered | `Uncovered_Destination_Is_Reported_Not_Treated_As_Unrestricted_Or_Allowed`, `Missing_Destination_Identifiers_Are_Not_Comparable_And_Never_Guessed_From_Text` |
| D3 — quote maps to Order without altering payment allocation / committed snapshots | `Shipping_Quote_Is_Observation_Only_And_Committed_Amount_Stays_Legacy` (Shipping says 999,999; authoritative price and minimum-delivery promise stay at the legacy values) |
| D4 — absence of Shipping tables/data cannot crash the legacy path | `Shipping_Failure_Or_Absent_Tables_Degrade_To_Unavailable_And_Never_Throw`, `Unconfigured_Lookup_Is_Unavailable_Without_Any_Query` |
| Contracts-only seam integrity | `Service_Code_Is_Forwarded_Untouched_To_The_Shipping_Authority` |

Evidence: `docs/architecture/evidence/TB-SHIPPING-CHECKOUT-BINDING-049/log-*.txt`.
Regression re-run: `Tooba.Shipping.Tests` 34/34 passed; `Tooba.Order.Tests` architecture guards 114/114 passed.

## 4. PREPARED

* The seam is a drop-in unit: a later authorized task registers `IShippingRateQuoteLookup` (Shipping's
  `ShippingRateQuoteLookup`) plus `StorefrontShippingRateAuthorityOptions` and calls
  `EvaluateAsync` from `StorefrontShippingService` purely for telemetry. No signature of the live
  service needs to change for that.
* `Tooba.Shipping.Contracts` is now reachable from `Tooba.Order.Application` through a single additive
  `ProjectReference` (Contracts-only, mirroring the existing Fulfillment/AddressBook/Localization lines).

## 5. BLOCKED — exact prerequisites for a live cutover

1. **Shipping schema is not persisted.** `ShippingDbContext` has no generated migration and is not in
   `ModuleMigrationRegistry` / `ModuleSchemaMigrationOrder`. Both are **forbidden** in this task
   (explicit FORBIDDEN list), so even the migration source was not generated here.
2. **Coverage grants are empty.** No `shipping.coverage_grants` rows exist for any effective enabled
   legacy service/destination. Until they do, the new engine legitimately answers `NotCovered` for
   every request and switching the checkout would deny all shipping.
3. **Destination identity is not resolvable on the live path.** ADR-036 Phase 2 (D8.4) text→id
   resolution is not authorized; today a real comparison can only run on a deterministic offline
   fixture that already carries `DestinationIdentifiers`.
4. **DI wiring is out of scope.** `OrderModule`/Host composition edits are forbidden in this task, so
   the seam is deliberately unreferenced at runtime. It cannot affect production behaviour by
   construction.

## 6. Allowlist disclosure (one deviation)

Two files outside the declared allowlist were required for the slice to compile and be testable, and
are staged and reported explicitly:

* `src/backend/Modules/Order/Tooba.Order.Application/Tooba.Order.Application.csproj` — one additive
  `ProjectReference` to `Tooba.Shipping.Contracts`. Without it a "Contracts-only Order → Shipping
  quote seam" cannot exist at all.
* `src/backend/Modules/Order/Tooba.Order.Tests/Storefront/StorefrontShippingRateShadowTests.cs` — the
  focused tests prove the acceptance properties, so their home is beside the seam.

Neither path is in the FORBIDDEN list; neither touches Host, `Program.cs`, module composition, the
migration registry, Fulfillment, Geography, frontend, or any dependency-injection root.

## 7. Non-goals honoured

No DB writes, no DDL, no Host restart/deploy, no endpoint, no admin page, no fabricated rates, no
nationwide implied coverage, no provider slots, no paid orders, no architecture-test weakening, no
cross-module `DbContext` access, no follow-on task.

## 8. Next milestone (5)

Milestone 5 must decide whether to (a) generate + register + apply the Shipping migration and seed
explicit coverage grants for the existing enabled service/destination pairs, and (b) run the shadow in
observe-only mode on a deterministic offline fixture, before any customer-facing switch is even
proposed. Neither step is authorized by this task.
