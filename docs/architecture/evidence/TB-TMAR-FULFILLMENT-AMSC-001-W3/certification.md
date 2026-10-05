# TB-TMAR-FULFILLMENT-AMSC-001 — W3 certification detail

## 1. Gate-by-gate certification

| ARCH-COMPLETE-002 gate | State | Evidence |
| --- | --- | --- |
| Module ownership (single capability set) | PASS | Shipping catalog / fulfillment lifecycle / work queue / checkout |
| Capability-first structure | PASS | `Checkout` `Composition` `Errors` `Fulfillments` `Shipping` `Validators` `WorkQueue` |
| No technical-axis root | PASS | no `Commands/` `Queries/` `Models/` `Ports/` at Application root |
| No single-file use-case leaf folders | PASS | 15 legacy leaf folders absent; shallow axes |
| Path ↔ namespace exact | PASS | `FulfillmentArchitectureGuardTests.AssertNamespacesAlign` |
| Root allowlists + forbidden flattened files | PASS | `Fulfillment_root_allowlists_and_forbidden_flattened_files_are_enforced` |
| File cohesion / ARCH-SIZE-001 | PASS | `FulfillmentDirectory.cs` 794, `.Packages.cs` 440 (< 800) |
| CQRS + MediatR | PASS | 15 `IRequest` / 15 `IRequestHandler`, dispatched via `ISender` |
| Exhaustive validator coverage | PASS | 10 required + 5 `NO_VALIDATOR_REQUIRED` |
| Canonical Result + `ApiResponseFactory` | PASS | no raw `Results.BadRequest`/`Problem`; `api.From(...)` |
| No message/prose classification | PASS | `FulfillmentExceptionMapper` deleted; zero `.Message` parsing |
| Typed stable error codes | PASS | 86 declared; 84 registered descriptors |
| Unique descriptor ownership | PASS | 84 Fulfillment + 2 Order-owned consumed-not-registered |
| Localization infrastructure | PASS | `IErrorResourceSet` + 86 keys × (en, fa) |
| Module endpoint ownership | PASS | 21 routes, `FulfillmentEndpointModule` |
| Host HTTP/business/persistence ownership | ZERO | Host Fulfillment-specific files = 0 |
| Cross-module Contracts-only boundary | PASS | no foreign App/Infra/Domain using/ref |
| No cross-module join / foreign DbSet | PASS | single `FulfillmentDbContext`, own `fulfillment` schema |
| Schema + migrations preserved | PASS | 10 migration files unchanged |
| Telemetry + correlation | PASS | `FulfillmentInstrumentation`, no second source |
| Manifest ↔ disk reconciliation | EXACT | 5 production projects |
| Microservice extractability | PASS | zero foreign coupling |
| Durable certification guard | PASS | `FulfillmentModuleAmsc001W3CertGuardTests` |

## 2. Canonical typed-fault seam

`Application/Composition/FulfillmentOperation.cs`:

```csharp
catch (ContractOperationException ex) when (FulfillmentErrors.IsKnown(ex.Code))
{
    return Result.Failure<T>(new SemanticError(ex.Code));
}
catch (SemanticException ex)
{
    return Result.Failure<T>(ex.Error);
}
```

Fulfillment is the only module in the pipeline with **two** typed, code-carrying fault mechanisms:

- `ContractOperationException` (stable `.Code`) — the Domain aggregates and the Infrastructure
  `FulfillmentDirectory` / `FulfillmentDirectory.Packages` / `ShippingServiceDirectory` guard sites.
- `SemanticException` (`SemanticError.Code`) — the Application throw sites, the shipping aggregates, and
  the Infrastructure language gate / outbox registration.

Both are mapped by their declared stable code. Unknown exceptions (and a `ContractOperationException`
carrying a non-Fulfillment code) propagate untouched to the canonical global exception boundary.

## 3. Defect found and repaired during certification (F1)

**Regression risk:** replacing `FulfillmentExceptionMapper` with a `SemanticException`-only seam would
have silently dropped the `ContractOperationException` mapping. Because ~45 Domain/Infrastructure guard
sites throw `ContractOperationException`, every one of those expected failures would have escaped the
seam and surfaced as an unexpected `platform.unexpected` 500.

**Repair:** `FulfillmentOperation` maps **both** typed mechanisms (dual catch). A durable test
(`Fulfillment_operation_maps_domain_contract_faults_by_stable_code`) and the Host cert guard
(`Fulfillment_typed_fault_seam_maps_both_code_carrying_mechanisms`) lock it.

## 4. Defect found and repaired during certification (F2)

**Regression:** `FulfillmentCustomerAuthorizer.EnsureCanViewCheckoutAsync` caught
`InvalidOperationException`, but the Cart gateway it calls (`ICartQueryGateway.GetCartAsync` →
`CartDirectory.EnsureAccess`) signals denial with `SemanticException` — exactly what Cart's own
`CartPresentationComposer.TryGetForOwnershipAsync` already catches. The stale catch let a denial escape
the authorizer as an unexpected failure.

**Repair:** narrowed the catch to `SemanticException`. The invalid-guest-secret path now returns its
intended catalogued `customer.order.missing` (404). No previously-allowed request becomes denied.

**Recorded honestly:** this is a bounded expected-failure repair (`500 -> 404`), so W3 records
`behaviorChange = BOUNDED_EXPECTED_FAILURE_REPAIR_CUSTOMER_AUTHORIZER_STALE_CATCH` and
`statusCodesChanged = BOUNDED_EXPECTED_FAILURE_REMAP_500_TO_404_ON_STALE_CATCH_ALIGNMENT` — never a
blanket `NONE` (the exact class of contradiction the Cart W3-R1 reconciliation corrected).

## 5. ContractOperationException is not the Cart F1 defect

Cart's W0 finding F1 was classification by **exact match on `InvalidOperationException.Message`** (a
prose heuristic). Fulfillment's `ContractOperationException` is different: it is a typed exception whose
`.Code` property is the stable machine code. Classification reads the typed property, never the message.
The Domain's raw code string literals are therefore recorded as a non-blocking style watch (R1), not as a
canonical-mechanism violation.
