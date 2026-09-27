# R1 Repair — TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1

Parent commit verified: `8c0e605c97ec35c116179d95915d38cf736f5468`

## Defects repaired

### 1. Canonical Result pipeline
| Route | Before | After |
| --- | --- | --- |
| GET /profile | `Results.Json(page)` | `IRequest<Result<CustomerProfilePage>>` → `api.From(result)` |
| PUT /profile | `Results.Json(page)` | `IRequest<Result<CustomerProfilePage>>` → `api.From(result)` |
| GET /dashboard | `Results.Json(page)` | `IRequest<Result<CustomerDashboardPage>>` → `api.From(result)` |

`ApiResponseFactory.From<T>` emits `Results.Json(result.Value)` on success → **success DTO JSON shape unchanged**.

Unauthorized session path remains `api.FromFailure(SemanticError("customer.session.required"))` (Foundation-owned descriptor; not re-registered).

### 2. Dev-context — PLATFORM_DEV_ROUTE_EXCEPTION
`GET /v1/customer/dev-context` is platform/dev presentation, not business CQRS.
- Non-dev/testing → exact `Results.NotFound()` (404)
- Dev/testing success → raw `{ actorUserId, label }`
No `ApiResponseFactory` method represents NotFound + anonymous success without inventing a new abstraction. Exception is documented in code XML + this evidence; guarded narrowly (must retain PLATFORM_DEV_ROUTE_EXCEPTION marker + NotFound).

### 3. Solution Explorer grouping
Moved five CustomerProfile projects from flat `/Modules/` into `/Modules/CustomerProfile/` in `Tooba.slnx`.
Physical paths / namespaces / assembly names unchanged.

### 4. Recovery artifacts
- Parent: `docs/ai/tasks/TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001.task.md` (PRESENT)
- Repair: `docs/ai/tasks/TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1.task.md` (PRESENT)

## Guards added/strengthened
- `CustomerProfileResultPipelineGuardTests`
- `CustomerProfileSolutionGroupingGuardTests`
- Validator coverage asserts `api.From(result)` on business endpoints
- Existing HostCustomerFullClosure / validator guards retained

## Schema / frontend
NONE / unchanged
