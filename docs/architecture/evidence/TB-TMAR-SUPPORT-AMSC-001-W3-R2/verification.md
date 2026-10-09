# TB-TMAR-SUPPORT-AMSC-001-W3-R2 — Canonical 201 Created Repair (Option A)

**Status: PASS.** Architect decision `OPTION A APPROVED` executed under explicit authority.
Starting head `e097a5d5a6c497b9590362a6bc52f3357cc9c08f` (`HEAD == origin/main`).

## 1. Production change (exactly two files, success return only)

`src/backend/Modules/Support/Tooba.Support.Endpoints/Customer/SupportCustomerEndpoints.cs` (`CreateAsync`):

```csharp
if (result.IsFailure)
    return api.From(result);
return api.Created($"/v1/customer/support/tickets/{result.Value.TicketId}", result);
```

`src/backend/Modules/Support/Tooba.Support.Endpoints/Seller/SupportSellerEndpoints.cs` (`CreateAsync`):

```csharp
if (result.IsFailure)
    return api.From(result);
return api.Created($"/v1/seller/support/tickets/{result.Value.TicketId}", result);
```

No other production file changed: no route mapping, no DTO, no validator, no resx, no catalog,
no `ApiResponseFactory`, no schema/migration, no frontend, no global Host checkpoint.

## 2. Wire-contract parity (status / body / content-type / failure) + additive Location

| Aspect | Before | After | Delta |
| --- | --- | --- | --- |
| Status | 201 | 201 | none |
| Body | raw `TicketSnapshotDto` JSON | raw `TicketSnapshotDto` JSON | none |
| `Content-Type` | `application/json` | `application/json` (charset=utf-8) | none |
| `Location` | absent | `/v1/{customer\|seller}/support/tickets/{TicketId}` | **additive only** |
| Failure path | ProblemDetails (400 `support.rejected`) | ProblemDetails (400 `support.rejected`) | none |
| Failure `Location` | absent | absent | none |

`ApiResponseFactory.Created<T>` already returned `Results.Created(location, result.Value)`
(201 + raw DTO JSON), so the repair removes a duplicated mapping rather than changing the wire
contract. The Location is derived from the real returned `TicketSnapshotDto.TicketId` and the real
matching GET-by-id route template (`/tickets/{ticketId:guid}` under each audience group) — no URI is
invented.

## 3. Executable behavioral proof (not source-string only)

New `src/backend/Modules/Support/Tooba.Support.Tests/Behavior/SupportTicketCreateResponseContractTests.cs`
(4 facts, all passing) invokes the **real** endpoint `CreateAsync` (non-public static handler) through
the **real** MediatR pipeline, the real `ApiResponseFactory` and the real composed error catalog:

| Fact | Proof |
| --- | --- |
| `Customer_create_returns_201_raw_snapshot_with_customer_location` | 201, `application/json`, `Location == /v1/customer/support/tickets/{returned TicketId}`, raw snapshot JSON (no `data` envelope, `status=Open`, `messages` array) |
| `Seller_create_returns_201_raw_snapshot_with_seller_location` | 201, `application/json`, `Location == /v1/seller/support/tickets/{returned TicketId}`, raw snapshot JSON |
| `Customer_create_failure_stays_problem_details_without_location` | 400, `errorCode = support.rejected`, empty `Location` |
| `Seller_create_failure_stays_problem_details_without_location` | 400, `errorCode = support.rejected`, empty `Location` |

The `Location` is asserted against the TicketId parsed from the same response body, so the URI is
proven truthful per audience, not hard-coded.

## 4. Durable guard tightening (never weakened)

`SupportModuleAmsc001W3CertGuardTests.Support_api_results_typed_fault_and_catalog_are_canonical`
updated deliberately:

- replaced the raw-201 count assertion with `Assert.DoesNotContain("Results.Json")` and
  `Assert.DoesNotContain("Status201Created")` over the Support endpoint production;
- added `Assert.Equal(2, Regex.Matches(endpointJoined, @"api\.Created\(").Count)` plus an exact
  per-audience Location-literal assertion binding each call to
  `api.Created($"/v1/{audience}/support/tickets/{result.Value.TicketId}", result)`;
- kept every unrelated assertion: single Development-gated `Results.NotFound()`, 17 `api.From(`,
  error-catalog uniqueness, 28 EN/28 FA resources, Contracts-only boundary, unchanged migrations.

## 5. Verification totals

| Command | Result |
| --- | --- |
| `dotnet build Tooba.Support.Endpoints.csproj` | succeeded, 0 warnings, 0 errors |
| `dotnet test Tooba.Support.Tests.csproj` | **17 passed / 0 failed** (13 pre-existing + 4 new) |
| `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~SupportModuleAmsc001` | **30 passed / 0 failed** (W1 9 + W2 8 + W3 13) |
| `dotnet test Tooba.Host.Tests --filter TmarCompleteReferenceStructureGateTests\|TmarDurableGuardTests` | 7 passed / **3 failed** — byte-identical pre-existing names |

Pre-existing failures (identical to the W3-R1 baseline, unrelated to Support):

```text
TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment
TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable
TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative
```

Zero newly failing fact. No guard weakened, no baseline widened.

## 6. Preservation

- `supportAmsc001W3` certification truth retained; `rawResultsState` corrected to `ZERO`.
- New `supportAmsc001W3R2` SoT block records the repair, the exact two Location patterns and the parity proof.
- `structureLock.certifiedModules` still lists `Support` exactly once; manifest NOT_TOUCHED.
- Global Host checkpoint preserved: `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`,
  `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`, `automaticNextImplementationTask = NONE`.
- Workflow stop: `USER_REVIEW_SUPPORT_AMSC_001_W3_R2`.
