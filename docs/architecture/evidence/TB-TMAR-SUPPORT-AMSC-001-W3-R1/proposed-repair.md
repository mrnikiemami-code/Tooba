# TB-TMAR-SUPPORT-AMSC-001-W3-R1 — Proposed Repair (Option A) + Test Plan

**Status: NOT IMPLEMENTED.** This document is a proposal only. No production file was changed by
`TB-TMAR-SUPPORT-AMSC-001-W3-R1`. Implementation requires explicit Architect approval
(`ARCHITECT_DECISION_REQUIRED`).

## 1. Exact minimal repair (two lines, one file each)

`src/backend/Modules/Support/Tooba.Support.Endpoints/Customer/SupportCustomerEndpoints.cs`
(`CreateAsync`, currently lines 71–73):

```csharp
if (result.IsFailure)
    return api.From(result);
return api.Created($"/v1/customer/support/tickets/{result.Value.TicketId}", result);
```

`src/backend/Modules/Support/Tooba.Support.Endpoints/Seller/SupportSellerEndpoints.cs`
(`CreateAsync`, currently lines 55–57):

```csharp
if (result.IsFailure)
    return api.From(result);
return api.Created($"/v1/seller/support/tickets/{result.Value.TicketId}", result);
```

## 2. Observable contract delta

| Aspect | Before | After | Delta |
| --- | --- | --- | --- |
| Status | 201 | 201 | none |
| Body | raw `TicketSnapshotDto` JSON | raw `TicketSnapshotDto` JSON | none |
| `Content-Type` | `application/json` | `application/json` | none |
| `Location` | absent | `/v1/{audience}/support/tickets/{ticketId}` | **additive** |
| Failure path | ProblemDetails | ProblemDetails | none |

The `Location` URI is derived from the value already returned by the handler
(`TicketSnapshotDto.TicketId`) and the route the client already posted to; no URI is invented.

## 3. Required follow-up in the same bounded task

1. Update `SupportModuleAmsc001W3CertGuardTests.Support_api_results_typed_fault_and_catalog_are_canonical`
   deliberately (never weaken):
   - replace the `Assert.Equal(2, Regex.Matches(..., @"Results\.Json\(result\.Value, statusCode: StatusCodes\.Status201Created\)"))`
     assertion with `Assert.DoesNotContain("Results.Json", endpointJoined)` and
     `Assert.DoesNotContain("Status201Created", endpointJoined)`;
   - add `Assert.Equal(2, Regex.Matches(endpointJoined, @"api\.Created\(").Count)` and bind each
     `api.Created(` call to the exact `Location` literal per audience;
   - keep the existing single `Results.NotFound()` (Development-gated demo-preview) assertion.
2. Update the SoT field `supportAmsc001W3.rawResultsState` from
   `TWO_201_CREATE_JSON_PLUS_ONE_DEVELOPMENT_GATED_NOTFOUND_LOCKED_SHIPPED_SHAPES` to a canonical
   value recording `ApiResponseFactory.Created` on the two create paths plus the single
   Development-gated `Results.NotFound()`, and add the `Location` delta note.
3. Do **not** change the manifest, schema, migrations, routes, verbs, error codes, resx, DTO shapes
   or the 17-route / 9-validator + 8-exemption matrix.

## 4. Test plan

| Test | Purpose | Expected |
| --- | --- | --- |
| `SupportModuleAmsc001W3CertGuardTests` (updated) | locks canonical `api.Created` on exactly the two create paths, zero raw `Results.Json`/`Status201Created`, single Development-gated `Results.NotFound()` | PASS |
| `SupportModuleAmsc001W1MigrateGuardTests` | unchanged canonical mechanisms | PASS (9/9) |
| `SupportModuleAmsc001W2StructureGuardTests` | unchanged structure | PASS (8/8) |
| `ApiResponseFactoryResultMappingTests.Created_success_sets_201_and_location` | proves the canonical factory emits 201 + `Location` + raw DTO | PASS |
| `SupportFoundationTests` + Support behavior/characterization tests | behavior preservation | PASS |
| `ErrorCatalogUniqueCodeGuardTests` | descriptor ownership untouched | PASS |

No test may be weakened; no baseline may be widened; the additive `Location` header is the only
permitted wire delta.

## 5. Alternative (Option B) — explicit narrow exception

If the Architect prefers to keep the raw 201, the Architect must author an explicit canonical lock
(not a certification note) permitting `Results.Json(..., 201)` for exactly these two successful
create responses, with negative constraints (no other Support route, no failure path, no envelope
change, no status change) and a durable guard. The worker must not self-authorize this.
