# File cohesion / god-file balance

State: **`COHESIVE`** (one `OVERSIZED_ONLY` watch item, no multi-responsibility violation remains).

## Defect repaired

`Admin/ShippingRateCoverageEndpoints.cs` was **multi-responsibility** before W2:

* the admin rate/coverage route surface (7 handlers + body reader + `IShippingAdminAuthorizer` +
  `ShippingAdminPermissions`), and
* the module composition root (`ShippingEndpointModule`: `MapShippingEndpoints`,
  `AddShippingEndpointPresentation`) — a DI/route-composition concern that the project root owns.

After the mechanical extraction each file has one responsibility. No algorithm, predicate, code
string, route, DTO, visibility or DI lifetime changed; the composition members were moved verbatim.

## Largest remaining production files (watch only, not structure blockers)

| File | Lines | Assessment |
| --- | --- | --- |
| `Infrastructure/Rates/ShippingRateCoverageDirectory.cs` | 558 | cohesive adapter over one aggregate family — `OVERSIZED_ONLY`, split is optional and out of scope |
| `Domain/Aggregates/ShippingRatePolicy.cs` | 544 | single aggregate + its invariants — `OVERSIZED_ONLY` |
| `Contracts/Rates/ShippingRatePolicyContracts.cs` | 447 | cohesive boundary DTO set — acceptable |
| `Infrastructure/Persistence/ShippingDbContext.cs` | 408 | single EF model — acceptable |
| `Application/Validation/ShippingAdminValidators.cs` | 361 | one capability's validator set — acceptable |

No file is split cosmetically to satisfy a size guard; no god-file was created by flattening
cohesive multi-file use cases.

## Known semantic duplication left in place (explicitly out of scope)

`ReadBodyAsync` / `TryReadBodyAsync` / `InvalidBodyException` are duplicated per admin endpoint
file (a W0 finding). W2 **must not** consolidate them: that is a semantic Migrate repair, not a
physical move. They remain byte-identical, so body-reading behavior and the
`validation.failed` + `body.invalid` mapping are preserved.

Durable guard: `ShippingModuleStructureGuardTests.Endpoints_composition_entry_lives_at_the_root_and_nowhere_else`
(the former Admin-resident composition defect must stay repaired).
