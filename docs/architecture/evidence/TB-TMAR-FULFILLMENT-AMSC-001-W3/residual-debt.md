# TB-TMAR-FULFILLMENT-AMSC-001 — W3 residual debt (non-blocking)

Blocking residual debt: **ZERO**. The following are recorded watches only; none blocks certification and
none weakens a guard or widens a baseline.

## R1 — Domain raw code string literals (style watch)

The Fulfillment **Domain** aggregates throw `ContractOperationException` with raw stable-code string
literals, e.g.:

```csharp
throw new ContractOperationException("fulfillment.pack.qty_exceeds");
```

This is **not** the Cart W0 F1 defect. Fulfillment's fault is *typed*: `ContractOperationException`
carries a stable `.Code` **property**, and classification reads that property — never the message. There
is no `InvalidOperationException.Message` exact-match heuristic and no prose parsing anywhere in the
module (the legacy `FulfillmentExceptionMapper` was deleted in W3).

Migrating the Domain literals to `FulfillmentErrorCodes.*` constants would require the Domain project to
reference `Tooba.Fulfillment.Contracts` (a layering change: the Domain currently references only
`Tooba.BuildingBlocks`), which is out of scope for a certification wave and is not required for
`COMPLETE_REFERENCE_PATTERN`.

## R2 — Domain codes are validated at the Application seam, not the Domain layer

Because R1 leaves the Domain literals uncompiled against the declared set, a mis-declared Domain code
would not fail at the Domain throw site. It would fail loud at the Application seam
(`FulfillmentErrors.IsKnown` in `FulfillmentOperation`) as an unexpected platform error rather than being
silently misclassified. Fail-loud is the safe direction; no silent misclassification is possible.

## R3 — Cohesive oversized-watch files

`Domain/Aggregates/FulfillmentUnit.cs` (~489 LOC) and
`Infrastructure/Queries/AdminFulfillmentWorkQueueQueryEngine.cs` (~495 LOC) are cohesive
single-responsibility types, both under the 800 LOC ARCH-SIZE-001 ceiling and with no size-baseline
entry. WATCH only.

## R4 — Pre-existing repo-wide Host drift

`TmarSourceSizeAndInfraAppTests`, `TmarFoundationTests`, `TmarCompleteReferenceStructureGateTests`,
`TmarDurableGuardTests`, `AdminDbNativeGridQueryTests`, `PaidProjectionFinancialTests` fail on
repo-wide drift in unrelated modules (stale baselines, `.tmp-baseline/` cruft, other modules' SoT
updates). Their failure set is byte-identical before and after W3 and is unrelated to Fulfillment.

## R5 — Untracked foreign artifacts

`docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/*`,
`TB-TMAR-CART-AMSC-001-W3-R1/*`, `TB-TMAR-CART-AMSC-001-W3-R2/*` and
`TB-TMAR-ORDER-AMC-001-W5-R1/*` remain untracked in the working tree. They belong to other modules'
runs and are not part of the Fulfillment scope.

## R6 — Domain `ContractOperationException` parity Host tests

The three Host tests (`FulfillmentLineQuantityOpsTests`, `FulfillmentLineQuantityOperationsTests`,
`FulfillmentShipmentAllocationTests`) assert `InvalidOperationException` with Persian prose
(`Assert.Contains("باقیمانده", ex.Message)`) while the Domain has long thrown the typed
`ContractOperationException`. This is the pre-existing parity divergence recorded in W0/W1; W3 did not
touch those Domain sites (doing so would itself be a behavior/type change outside a certification wave).
