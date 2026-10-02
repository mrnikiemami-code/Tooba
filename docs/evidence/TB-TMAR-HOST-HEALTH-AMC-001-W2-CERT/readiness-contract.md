# readiness-contract — TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT

## Check order (certified)

1. edition configured
2. configured PostgreSQL reference presence only (`CONFIGURED_NOT_CONNECTIVITY`)
3. authorization readiness (Contracts probe)
4. messaging readiness

## Tenant policy

`ALL_CONFIGURED_PRESERVED` — SingleStore enumerates all configured tenant connection references (not Active-only). Deployment-configuration readiness, not request-routing readiness.

## Response

- ready → 200 `status=ready`
- not-ready → 503 `status=not-ready`
- raw `Results.Json` = `OPERATIONAL_PROTOCOL_CERTIFIED`
- no ApiResponseFactory requirement for health protocol

## Exceptions

No broad `catch (Exception)`. Unknown faults propagate to canonical Host exception boundary. No `exception.Message` classification.
