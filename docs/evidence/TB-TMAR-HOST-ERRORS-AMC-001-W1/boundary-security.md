# boundary-security — TB-TMAR-HOST-ERRORS-AMC-001-W1

## Contracts / foreign layers

- Host/Errors and touched MultiTenancy path depend on BuildingBlocks presentation + Foundation codes only.
- No foreign `.Application` / `.Infrastructure` / `.Domain` / DbContext on the Errors or MultiTenancy resolution path.
- No business authority moved into Errors.

## Protected certifications

| Label | State |
|---|---|
| `HOST_SECURITY_AMC_CERTIFIED` | PRESERVED (no Security production edits) |
| `HOST_ADMIN_FULLY_CERTIFIED` | PRESERVED (no Admin production edits) |

## Sensitive detail

Fail-closed 404 responses carry stable `errorCode` + `traceId` only; no connection strings, tenant existence, or exception.Message classification in presentation.
