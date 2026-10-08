# Capability map (section 5)

Capabilities are **business responsibility axes**, not folder names invented from type names.
Discovery sources: existing module capability folders, Endpoints audience map, Domain aggregates,
Contracts ports, W0 `TOOBA-CAPABILITY-MAP` findings.

## 1. Capabilities

| Capability | Evidence in module | Physical home |
|---|---|---|
| **Return request lifecycle** (`ReturnRequests`) | Domain `Aggregates/ReturnRequest.cs`, `Aggregates/ReturnItem.cs`; `Events/ReturnRequestedDomainEvent.cs`; Contracts `Events/ReturnRequestedIntegrationEvent.cs`; Application port `IReturnDirectory` | `Application/ReturnRequests/` |
| **Refund settlement attempt** | Domain `Aggregates/RefundAttempt.cs`, `ValueObjects/RefundAttemptStatus.cs`, `ValueObjects/RefundDestination.cs`; Contracts `Events/RefundSucceededIntegrationEvent.cs`, `Settlement/ReturnSettlementContracts.cs` | Domain + `Contracts/Settlement` + `Infrastructure/Bridges/ReturnSettlementBridge.cs` |
| **Refund execution / restock** | Application port `IReturnInventoryGateway` | `Infrastructure/Gateways/ReturnInventoryGateway.cs` |
| **Eligibility evaluation** | Application port `IReturnEligibilityEvaluator` | `Infrastructure/Evaluators/ReturnEligibilityEvaluator.cs` |
| **Admin work queue / grid** | Contracts `Operations/ReturnAdminOperationsContracts.cs`; Application `ReturnRequests/Models/{AdminReturnWorkQueueRow,AdminReturnQueueFilters}`, `Queries/QueryAdminReturnsGridQuery` | `Infrastructure/Queries/AdminReturnGridQueryEngine.cs` |
| **Audience surfaces** (Admin / Seller / Customer) | Endpoints `Admin/`, `Seller/`, `Customer/` | `Endpoints/{Admin,Seller,Customer}/` |

`ReturnRequests` is a genuine capability axis: the module's own aggregate, its own domain event, its
own integration event and its own directory port all use the "return request" vocabulary. It was not
derived from `CreateReturnCommand` type names.

## 2. Capability → physical placement matrix

| Capability | Application | Contracts | Domain | Infrastructure | Endpoints |
|---|---|---|---|---|---|
| ReturnRequests | `ReturnRequests/{Commands,Queries,Models,Ports}` | `Errors/`, `Events/`, `History/`, `Operations/` | `Aggregates/`, `Events/`, `ValueObjects/` | `Directories/`, `Adapters/`, `Queries/` | `Admin/`, `Seller/`, `Customer/` |
| Refund settlement | `ReturnRequests/Commands/RetryReturnRefundCommand` | `Settlement/`, `Events/` | `Aggregates/RefundAttempt` | `Bridges/`, `Messaging/` | — |
| Refund execution | `ReturnRequests/Ports/IReturnInventoryGateway` | — | — | `Gateways/` | — |
| Eligibility | `ReturnRequests/Ports/IReturnEligibilityEvaluator`, `Models/ReturnEligibility*` | `Operations/ReturnEligibilityReasons` | — | `Evaluators/` | — |
| Cross-capability mechanism | `Composition/` (typed fault seam + destination parser), `Validation/` (transport validators + codes) | `Errors/ReturnsErrorResourceSet`, `Resources/` | — | `Errors/ReturnsErrorCatalogContributor`, `Observability/`, `DependencyInjection/`, `Persistence/` | root `ReturnEndpointModule.cs` |

## 3. Cross-capability shared homes

Per section 5, cross-capability shared rules may live under shared `Validation/` / `Composition/`
when the repository pattern already uses them. Both are used here exactly as the certified
Promotion / Inventory / Media modules do:

- `Application/Composition/ReturnsOperation.cs` — the module's single typed-fault → `Result` seam,
  used by every command and query handler.
- `Application/Composition/ReturnRefundDestinationParser.cs` — transport-level destination parsing,
  shared by create/approve/retry paths.
- `Application/Validation/ReturnsRequestValidators.cs` — the 4 transport validators.
- `Application/Validation/ReturnsValidationCodes.cs` — validator-side codes.

## 4. Anti-invention check

No capability folder was created that lacks a Domain aggregate, Contracts port, Endpoints audience or
W0-documented responsibility. No folder exists purely to satisfy symmetry with another module.
Certified modules (Promotion, Inventory, Media, Party, Payment) were used as **principle**
references only; the Returns tree is not a physical clone of any of them.
