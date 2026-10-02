# readiness-parity — TB-TMAR-HOST-HEALTH-AMC-001-W1

## Routes (exact 4)

- `GET /health/live` → `{ status: "ok" }`
- `GET /health` → `{ status: "ok" }`
- `GET /health/ready` → evaluator
- `GET /ready` → same evaluator

## Check order preserved

1. edition
2. configured PostgreSQL references (presence only — CONFIGURED_NOT_CONNECTIVITY)
3. authorization readiness (Contracts probe)
4. messaging readiness

## Tenant policy

`CollectConnectionReferences` still enumerates **all configured** SingleStore tenants (not Active-only). Documented as `CONFIGURED_DEPLOYMENT_REFERENCES_NOT_ACTIVE_TENANT_CONNECTIVITY`.

## Exceptions

No broad `catch (Exception)` added. Unknown faults continue to Host global exception boundary.

## Presentation

Raw `Results.Json` retained as intentional operational health protocol exception (not ApiResponseFactory).
