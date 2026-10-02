# boundary-security — TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT

## Host/Errors contracts

| Forbidden | State |
|---|---|
| Foreign Application | ZERO |
| Foreign Infrastructure | ZERO |
| Foreign Domain | ZERO |
| DbContext / persistence | ZERO |
| Business command/policy | ZERO |

Allowed: ASP.NET `IExceptionHandler` + BuildingBlocks Presentation.

## Sensitive detail

Canonical presentation responses carry stable `errorCode` + `traceId`; no stack, SQL, connection strings/refs, or exception.Message.

## Protected certifications

- `HOST_SECURITY_AMC_CERTIFIED` PRESERVED
- `HOST_ADMIN_FULLY_CERTIFIED` PRESERVED
- MultiTenancy AMC = NOT_OPENED
