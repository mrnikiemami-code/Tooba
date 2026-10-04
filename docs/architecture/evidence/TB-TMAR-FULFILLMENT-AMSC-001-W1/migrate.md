# TB-TMAR-FULFILLMENT-AMSC-001 — W1 Migrate

- Module: `Fulfillment`
- Skill: `tooba-architecture-migrate`
- Canonical reference: `Cart` (certified) + `Offer` (reference module) + BuildingBlocks.
- Structure-Handoff-State: `REQUIRED` (W2).

## 1. What changed

### 1.1 Stable error codes completed — `Contracts/Errors/FulfillmentErrorCodes.cs`
Declared constants: **19 → 84**. Added every code the module actually emits/maps:
lifecycle (`fulfillment.not_found`, `order.not_found`, `order.not_paid`, `order_line.not_found`,
`qty.positive`, `status.terminal`, `status.processing_invalid`, `selection.required`, `outbox.unmapped_event`),
processing/pack/ship/cancel/restore, shipment, dispatch/deliver/tracking,
consolidated package (16), shipping-method provider rules (8), work queue (7),
shipping-service catalog (5), and the previously unregistered
`shipping_service_option.code.required` / `shipping_service_option.name.required`.

Preserved: every published code value and every existing constant name.

### 1.2 Error catalog completed — `Infrastructure/Errors/FulfillmentErrorCatalogContributor.cs`
Registered descriptors: **17 → 86**. Every mapper-recognised code now resolves to exactly one
`ErrorDescriptor` with explicit `Classification`, `HttpStatus`, `LocalizationKey = code`,
`Severity` and safe English fallback. Previously unmapped expected failures degraded to
`platform.unexpected` / HTTP 500.

### 1.3 Localization infrastructure added — `Endpoints/Resources/`
- `FulfillmentErrors.resx` (86 keys)
- `FulfillmentErrors.fa.resx` (86 keys)
- `FulfillmentErrorResources.cs` → `FulfillmentErrorResourceSet : IErrorResourceSet`
  owning the `fulfillment.*`, `shipping_service*`, `seller.order.handle.*`, `customer.actor.missing` keyspaces.
- Registered `services.AddSingleton<IErrorResourceSet, FulfillmentErrorResourceSet>()`
  in `FulfillmentEndpointModule.AddFulfillmentEndpointPresentation`.

`Localization-State`: `MISSING_INFRASTRUCTURE_USE` → `CANONICAL`.

### 1.4 Canonical API result mapping — `Endpoints/Shipping/ShippingServiceEndpoints.cs`
`Results.Json(new { ok = true })` (Deactivate + EnsureSeed) → `api.From(result)` (204 on success),
matching the module's own `IRequest<Result>` contract and the canonical factory.

Preserved raw/anonymous success envelopes that ARE the shipped contract:
- `FulfillmentCustomerEndpoints` → `Results.Json(new { fulfillments, preferredCustomerTrackingReference })`
  (consumed by `src/frontend/app/fulfillment/fulfillment-api.ts`).
- `ShippingServiceEndpoints` GET/list raw DTOs.

### 1.5 Typed error mapping — `Application/Errors/`
- New `FulfillmentErrors` helper: validates a stable code once against the declared
  `FulfillmentErrorCodes` set (`RequireKnown` / `Semantic` / `Contract` / `IsKnown`), so an
  uncatalogued code fails fast at the throw site instead of silently becoming `platform.unexpected`.
- `FulfillmentExceptionMapper`: removed the ~100-line `TryMapExact` string switch and the
  `switch (message)` heuristic. Classification is now `FulfillmentErrors.IsKnown(code)` over the
  typed code surface; unknown exceptions still rethrow to the canonical global exception boundary.
- `ShippingServiceSemantic`: `Normalize` throw-on-unknown replaced by `FulfillmentErrors.RequireKnown`.

`API-Result-Pattern-State`: `AD_HOC` → `CANONICAL` (typed-code classification, no prose heuristic).
`Stable-Error-Code-State`: `UNREGISTERED_CODES` → `CATALOGUED`.

### 1.6 Duplicate descriptor ownership removed
`seller.order.missing` and `customer.order.missing` were registered by **both**
`FulfillmentErrorCatalogContributor` and `OrderErrorCatalogContributor`. Order owns the order
aggregate and the `seller.*` / `customer.*` keyspace (its resx already ships both keys), so
Fulfillment now consumes the codes without re-registering the descriptors.
Duplicate usage retained; duplicate ownership removed; no suppression/overwrite introduced.

## 2. Behavior preservation

- Routes, HTTP methods, DTO success shapes, authorization semantics, state transitions,
  persistence/schema, migrations, telemetry metric names and correlation behavior: unchanged.
- Domain throw sites: **untouched** (a Domain-wide `InvalidOperationException` →
  `ContractOperationException` rewrite was attempted and reverted because Host parity tests
  assert the exact exception type).
- Infrastructure/Application raw-code throw sites: **untouched** for the same reason; the code
  values are identical, so `FulfillmentErrors.IsKnown` classification preserves behavior exactly.
- Only the *failure envelope* of previously-unmapped expected failures changes: from
  `platform.unexpected` 500 to the typed catalogued/localized status.

## 3. Files

| File | Change |
| --- | --- |
| `Contracts/Errors/FulfillmentErrorCodes.cs` | 19 → 84 declared codes |
| `Infrastructure/Errors/FulfillmentErrorCatalogContributor.cs` | 17 → 86 descriptors; duplicate ownership removed |
| `Application/Errors/FulfillmentErrors.cs` | new typed code-validation helper |
| `Application/Errors/FulfillmentExceptionMapper.cs` | string switch removed → typed classification |
| `Application/Shipping/ShippingServiceReadContracts.cs` | `Normalize` → `RequireKnown` |
| `Endpoints/Shipping/ShippingServiceEndpoints.cs` | raw `Results.Json` → `api.From` |
| `Endpoints/FulfillmentEndpointModule.cs` | registers `IErrorResourceSet` |
| `Endpoints/Resources/FulfillmentErrors.resx` | new (86 keys) |
| `Endpoints/Resources/FulfillmentErrors.fa.resx` | new (86 keys) |
| `Endpoints/Resources/FulfillmentErrorResources.cs` | new resource set |
| `Tests/Behavior/FulfillmentErrorAndGridTests.cs` | `TryMapExact` assertions → `FulfillmentErrors.IsKnown` |

## 4. Focused validation

| Validation | Result |
| --- | --- |
| `dotnet build src/backend/Tooba.slnx` | Build succeeded, 0 errors |
| `Tooba.Fulfillment.Tests` | 65/65 passed |
| `Host.Tests --filter ErrorCatalogUniqueCodeGuardTests` | 3/3 passed (was 2 failures from duplicate descriptors) |
| `Host.Tests --filter FulfillmentLineQuantityOpsTests\|FulfillmentFoundationTests` | 7 failed / 6 passed — **identical to the pre-change baseline** (verified by stash/re-run) |

Pre-existing failures (not introduced by W1, not in scope): the Host Fulfillment parity tests
assert `InvalidOperationException` while the Domain throws `ContractOperationException`
(workspace state divergence), and `FulfillmentFoundationTests` asserts a contradictory
`Assert.Contains("Tooba.Order.Application", Fulfillment.Infrastructure.csproj)`.

## 5. Residual for W2/W3

- `Application` tree is still `TECHNICAL_AXIS_FIRST` + `OVER_FOLDERED` (W2).
- `FulfillmentDirectory.cs` 1103 LOC `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (W2).
- Application `ShippingService*Contracts.cs` mixed bundles → capability `Models`/`Ports` (W2).
- `Infrastructure/Queries/` technical-axis folder (W2).
- Manifest/SoT/durable cert guard (W3).
