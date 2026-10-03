# TB-TMAR-ORDER-AMC-001-W4 — Certify: verdict and blocker report

## Verdict

```text
NOT_CERTIFIED
```

The mandatory **Structure gate is PASSED** (`READY_FOR_CERTIFY`, see `structure-gate.md`) and the W0–W3
blockers (structure, validator coverage, cross-module boundary, API mapping, god files) are closed. Certification
cannot return `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED` because **expected-failure
classification by parsing `InvalidOperationException.Message` still exists in the Order surface**, and no
canonical architecture lock exempts it.

No guard, test, baseline, or manifest entry was weakened to reach this state. Manifest promotion and SoT
`STRUCTURE_CERTIFIED` were deliberately **not** applied, because certification did not pass.

## Blocker inventory

### B1 — Expected-failure classification by Message (Application, storefront)

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontOrderResult.cs`

```csharp
catch (InvalidOperationException exception) when (TryMapCheckoutDirectoryCode(exception.Message, out var code))
```

`TryMapCheckoutDirectoryCode` splits the exception **message** and matches `inventory.*`, `PRICE_CHANGED`,
`PROMOTION_CHANGED`, `TAX_*`, `checkout.*`, `shipping.*`, `order.*`, `pending.*`, `payment.*`. This is exactly
the pattern Certify §8 rejects: *"no failure classification by parsing `ex.Message` (e.g. `when (ex.Message...)`)"*.

Search across `docs/architecture/TMAR-architecture-locks.md` and both SoT JSON files returns **no** exemption for
Order storefront message classification.

### B2 — Expected-failure classification by Message (Infrastructure, admin fulfillment)

`src/backend/Modules/Order/Tooba.Order.Infrastructure/Admin/Fulfillment/AdminOrderFulfillmentOperations.cs`

```csharp
catch (InvalidOperationException ex) when (TryMapStableMachineCode(ex.Message, out var mapped))
```

`TryMapStableMachineCode` matches `fulfillment.*`, `inventory.*`, `shipping_service.*`, `order.*` on the message,
plus the `fulfillment.cancel.already_dispatched → fulfillment.dispatch.already_dispatched` remap.
`shipping_service.*` is owned by **Fulfillment** (`ShippingServiceDirectory`), so Order is classifying a foreign
module's message text.

### B3 — Expected flows thrown as untyped `InvalidOperationException`

The canonical typed fault already exists and is already used by Order admin operations:

- `Tooba.BuildingBlocks.ContractOperationException(string code)` (used by `CheckoutDirectory.cs` restore path,
  `ReservationCycleCoordinator`, `AdminOrderOperationsRecoverySupplyAdapters`, and by Fulfillment/Inventory/Settlement
  adapters).

The following expected flows still throw untyped `InvalidOperationException` with a stable-code message, which is
what forces B1/B2 into existence:

| File | Codes thrown |
|---|---|
| `Infrastructure/Checkout/Persistence/CheckoutDirectory.Reservations.cs` | `inventory.supply.unavailable`, `PRICE_CHANGED`, `TAX_NO_APPLICABLE_RULE`, `TAX_CALCULATION_ERROR`, `PROMOTION_CHANGED` |
| `Infrastructure/Integrations/Payment/OrderUnpaidRetrySupplyBridge.cs` | `payment.unpaid.supply_unavailable`, `inventory.reservation.retry_limit_reached` |
| `Infrastructure/Admin/Supply/OrderSupplyCheckoutStore.cs` | `order.operation.invalid` |
| `Domain/Aggregates/SellerOrder.cs` | `order.restore.invalid_state`, `order.restore.missing_snapshot`, `order.payment.unconfirm.invalid_state` |
| `Domain/Checkout/CartShippingDraft.cs` | `shipping.note.too_long` |

### Blocking rule

- Certify §8 and the Hard Rules: *"Never PASS while any applicable violation … remains outside an explicit
  canonical lock exemption"*, *"Never accept failure classification by parsing `ex.Message`"*.
- Certify §0: *"known prerequisite violations cannot be converted to residual debt to justify PASS."*

Therefore W4 returns `NOT_CERTIFIED` with a repair plan instead of a PASS.

## Certified-PASS surface (verified this wave)

| Check | State | Evidence |
|---|---|---|
| Structure gate | `READY_FOR_CERTIFY` | `structure-gate.md` (folder granularity, root allowlists, path↔namespace 324 files / 0 mismatch, physical copies, slnx) |
| Folder granularity | `PROFESSIONAL_SHALLOW` | zero single-file Commands/Queries leaves; no technical-axis-first root; new durable guard |
| Root allowlists | `ENFORCED` | all five projects |
| Path ↔ namespace | `EXACT` | 324 files, 0 mismatch (3 `GlobalUsings.cs` namespace-less by design) |
| Physical copies | `CLEAN` | old Application `PurchaseVerification/` absent |
| Solution Explorer | `CANONICAL` | `/Modules/Order/` grouping complete in `Tooba.slnx` |
| File cohesion | `COHESIVE` | largest production file 560 LOC, single responsibility; W3 splits |
| Endpoint ownership | `MODULE_ENDPOINTS` | 43 route registrations in `Tooba.Order.Endpoints`; Host owns none |
| CQRS / MediatR | `COMPLIANT` | `ISender` dispatch, `IRequestHandler<,>`, MediatR 12.5.0 |
| Validator coverage | `EXHAUSTIVE` | manifest classifies every endpoint-reachable request; guard green |
| Cross-module boundary | `LEGAL_CONTRACTS_ONLY` | 0 foreign csproj refs to Order Application/Infrastructure/Domain; `Reviews.Infrastructure` → `Order.Contracts` |
| Cross-module joins | `NONE` | no foreign DbSet / SQL join |
| Persistence ownership | `CORRECT` | one `OrderDbContext`; 21 migrations untouched |
| Schema / migration safety | `UNCHANGED` | no migration added/regenerated; identifiers/order intact |
| API result mapping | `CANONICAL` | `ApiResponseFactory` only; `Results.Json` = 0; 2 `Results.Content` are the HTML document success path |
| Localization / catalog | `CANONICAL` | `OrderErrorResourceSet` + `OrderErrors(.fa).resx`; one descriptor owner per code; shared `reservation.policy.*` owned by Catalog; `ErrorDefinitionCatalog` fail-fast preserved |
| Logging | `CANONICAL` | `ILogger<T>` only; 0 `Console`/`Debug` writes |
| Sensitive logging | `NONE` | no secrets/tokens/payment payloads logged |
| Correlation / tracing | `CANONICAL` | 0 parallel correlation, 0 `StartActivity`, 0 `AsyncLocal`/`traceparent` |
| Host authority | `ALLOWED_COMPOSITION_ROOT` only | `HostOrderAmcGuardTests` forbids Host Order business/persistence; `HOST_ROOT_FINAL_CERTIFIED` preserved |
| Host final-closure regression | `NONE` | W3/W4 touched zero Host production files |
| Closed-folder / sink regression | `NONE` | no file moved into a closed folder |
| Error descriptors | `UNREGISTERED_CODES` = 0 | all Order-owned codes resolve; composed catalog has no duplicates |

## Focused validation

```text
dotnet build src/backend/Tooba.slnx
Build succeeded. 0 Error(s)

dotnet test src/backend/Modules/Order/Tooba.Order.Tests
Passed! - Failed: 0, Passed: 139, Skipped: 0, Total: 139
```

Order guards green, including the three left red by W2:
`OrderEndpointPresentationTests.Every_order_owned_error_code_has_an_explicit_descriptor`,
`OrderEndpointPresentationTests.Composed_catalog_resolves_order_and_shared_codes_without_duplicates`,
`OrderEndpointValidatorCoverageGuardTests.Manifest_covers_every_endpoint_reachable_request_exactly_once`.

### Pre-existing, out of scope (not caused by this task)

`Tooba.Host.Tests.HostOrderReverseAuditGuardTests.Host_Order_reference_inventory_matches_discovered_production_files`
is RED at the W3 baseline. Cause: Host files were relocated to `Host/Composition/*` and deleted
(`Grid/AdminListGridPolicies.cs`, `Grid/AdminSellersGridQueryEngine.cs`, `Preferences/UserPreferenceEndpoints.cs`,
`Settings/SettingsFoundationDevelopmentSeed.cs`, `Storefront/StorefrontEndpoints.cs`, `Support/*`, `Wishlist/*`) by
`86bfb80a` / `c08c4afa`, while the R7 evidence inventory JSON was never reconciled. Not an Order defect; no Order
file is involved; not repaired here (out of bounded scope).

## Manifest and SoT

- `tmar-module-structure-manifests.json`: **unchanged**. The existing Order entry keeps
  `structureCertified: true` under the prior ARCH-COMPLETE-002 acceptance; it is **not** promoted or re-affirmed by
  this wave because certification did not pass.
- `tmar-current-state.json`: `orderAmc001` updated to the honest `W4_CERTIFY_NOT_CERTIFIED` state with the blocker
  list and the W5 repair scope.

## Required repair scope (next wave)

1. Convert the B3 throw sites to the canonical typed fault:
   - `CheckoutDirectory.Reservations.cs` → `ContractOperationException(code)` for `inventory.supply.unavailable`,
     `PRICE_CHANGED`, `TAX_*`, `PROMOTION_CHANGED` (keep the intentionally-propagating invariant throws as-is).
   - `OrderUnpaidRetrySupplyBridge.cs` → `ContractOperationException(code)`.
   - `OrderSupplyCheckoutStore.cs` → typed fault by `Code`.
   - `Domain/Aggregates/SellerOrder.cs`, `Domain/Checkout/CartShippingDraft.cs` → typed fault by `Code`
     (preserving exact observable codes).
2. Replace `StorefrontOrderResult.TryMapCheckoutDirectoryCode` with a typed `catch (ContractOperationException ex)`
   that maps `ex.Code` only; delete the message parser.
3. Replace `AdminOrderFulfillmentOperations.TryMapStableMachineCode` with a typed `catch (ContractOperationException ex)`
   using `ex.Code` only, including the preserved `fulfillment.cancel.already_dispatched → fulfillment.dispatch.already_dispatched`
   remap keyed on `Code`.
4. Preserve behaviour: `Host.Tests/CheckoutOrderFoundationTests` asserts `Assert.Equal("PRICE_CHANGED", priceChanged.Message)`
   and `CartLifetimeSeparationTests` asserts the `inventory.supply.unavailable` string — both must be updated to the
   typed `Code` (the observable stable code is preserved; only the carrier changes).
5. Re-run focused Order + Host checkout/fulfillment guards and re-issue the Certify verdict.

No schema, route, status-code, response-shape, or public Contracts change is required for the repair.
