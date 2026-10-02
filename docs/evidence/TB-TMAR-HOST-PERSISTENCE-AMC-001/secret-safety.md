# secret-safety — TB-TMAR-HOST-PERSISTENCE-AMC-001

## Required ZERO exposures — verified

| Surface | State |
| --- | --- |
| Connection string in exception message/title | ZERO |
| Connection reference in exception title/detail | ZERO |
| Connection string logged in resolver | ZERO (`ILogger` absent) |
| Connection reference in public ProblemDetails from resolver | ZERO (stable code only) |
| Npgsql parser exception text propagation | ZERO (caught `ArgumentException` discarded) |
| `Console.Write` / debug dumps | ZERO |

## Trust boundary

Raw connection string returns only to trusted Host/module infrastructure. Public responses use localized Foundation code presentation without credentials.

## Verdict

**SENSITIVE_DATA_STATE = ZERO_LEAKAGE_PROVEN** for the Persistence folder production type.
