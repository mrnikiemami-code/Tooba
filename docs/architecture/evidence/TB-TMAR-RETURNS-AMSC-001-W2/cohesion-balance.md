# File-Cohesion-State

**Verdict: `COHESIVE`**

## 1. God-files repaired by W1 (verified at W2)

| Pre-W1 file | LOC | Problem | Post-W1 split | LOC each |
|---|---|---|---|---|
| `Application/Models/AdminReturnWorkQueueModels.cs` | 163 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` — internal read-model row + queue filters + status/action projection policy + hardcoded Persian summary in one file | `ReturnRequests/Models/AdminReturnWorkQueueRow.cs` (row DTO) + `ReturnRequests/Models/AdminReturnQueueFilters.cs` (filters/status/action policy) | 24 / 134 |
| `Application/Errors/ReturnsExceptionMapper.cs` | 169 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` — typed-fault seam + destination parsing + ~18 `ex.Message` alias literals | `Composition/ReturnsOperation.cs` (typed seam) + `Composition/ReturnRefundDestinationParser.cs` (transport parser) | 67 / 25 |

## 2. Deleted dead/duplicate files

| File | Reason |
|---|---|
| `Application/Ports/ReturnSemanticMapper.cs` | dead — zero callers |
| `Application/Models/{CreateReturnCommand,ApproveReturnCommand,RejectReturnCommand,RetryRefundCommand}.cs` | duplicate copies of the MediatR requests in the same assembly |

## 3. Largest remaining files (deliberate WATCH, not violations)

| File | LOC | Assessment |
|---|---|---|
| `Infrastructure/Directories/ReturnDirectory.cs` | 384 | `OVERSIZED_ONLY` → single responsibility: the return-request directory/orchestration (create/approve/reject/retry/get/list/map). The `OpenReturnUseCaseGuard` concern was split out into its own file. Multiple `Task<ReturnSnapshot>` methods are one cohesive persistence+orchestration responsibility. **WATCH**, not a structure blocker. |
| `Infrastructure/Queries/AdminReturnGridQueryEngine.cs` | 288 | `OVERSIZED_ONLY` — cohesive DB-native admin grid engine. **WATCH**. |
| `Infrastructure/Evaluators/ReturnEligibilityEvaluator.cs` | 247 | `OVERSIZED_ONLY` — cohesive eligibility evaluation. **WATCH**. |
| `Domain/Aggregates/ReturnRequest.cs` | 188 | aggregate root; cohesive. **WATCH**. |
| `Contracts/Operations/ReturnAdminOperationsContracts.cs` | 161 | boundary contract cluster (enums + wire DTOs + port + reason constants). Cohesive boundary vocabulary; it is **not** an Application dump (section 13). **WATCH**. |
| `Application/ReturnRequests/Models/AdminReturnQueueFilters.cs` | 134 | queue filter/status/action projection policy — one responsibility after the split. |
| `Application/ReturnRequests/Queries/QueryAdminReturnsGridQuery.cs` | 107 | query + handler for the admin grid; cohesive. |
| `Application/Validation/ReturnsRequestValidators.cs` | 79 | the 4 transport validators, one cohesive validation home (repository pattern). |

Per section 12, `OVERSIZED_ONLY` may be WATCH without forcing a structure FAIL when not
multi-responsibility. None of the above mixes unrelated responsibilities; splitting them further
would be a cosmetic split to game size guards (hard rule 7) — explicitly not done.

## 4. Over-split check

| Check | Result |
|---|---|
| Cosmetic splits performed to satisfy a size/structure guard | 0 |
| Files created that hold a single trivial member for symmetry | 0 |
| Splits that reintroduced god-files elsewhere | 0 |
| Legitimate multi-file use-case cohesion flattened into god-files | 0 (all four commands and seven queries are independently cohesive and small) |

## 5. Contracts cohesion (section 13)

| Check | Result |
|---|---|
| CQRS request dump in Contracts | 0 (`CreateAdminReturnCommand`/`ReturnLineCommand` are boundary operation payloads of `IReturnAdminOperations`, not MediatR requests) |
| Root `*Contracts.cs` mixed dump | 0 (root allowlist is empty; `History/ReturnHistoryContracts.cs`, `Settlement/ReturnSettlementContracts.cs`, `Operations/ReturnAdminOperationsContracts.cs` each live in their purpose folder) |
| Stable boundary vocabulary only | YES — `Errors/` (codes + resource set), `Events/`, `History/`, `Operations/`, `Settlement/`, `Resources/` |
| Path ↔ namespace exact | YES (0 mismatches) |

## 6. Domain cohesion (section 14)

`Aggregates/` (3 files), `Events/` (3 files), `ValueObjects/` (3 files). No root god file bundling
enums + entities + rules. No cross-module Domain reference. Path ↔ namespace exact.

## 7. Infrastructure cohesion (section 15)

`INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS` satisfied: `Persistence/` + `Persistence/Migrations/`
(migrations are **not** at the Infrastructure root), `Directories/`, `Adapters/`, `Bridges/`,
`Evaluators/`, `Gateways/`, `Messaging/`, `Observability/`, `Errors/`, `DependencyInjection/`,
`Queries/`. No fragmented competing top-level homes. Root allowlist empty.

## 8. Endpoints cohesion (section 16)

`ENDPOINTS_CAPABILITY_FOLDERS` satisfied: `Admin/`, `Seller/`, `Customer/` audience folders each hold
their routes + authorizer. Root holds exactly `ReturnEndpointModule.cs` (composition). No capability
`*Endpoints.cs` at root. No Domain/Infrastructure import in Endpoints (Contracts + Application only).
