# mapper-retirement — TB-TMAR-HOST-ERRORS-AMC-001-W1

## Deleted

- `src/backend/Host/Tooba.Host/Errors/PlatformExceptionMapper.cs`
- Type `PlatformExceptionMapper`
- Type `MappedPlatformError`
- Direct `ProblemDetails` construction path
- Forced `SemanticException` → 400 branch
- Hard-coded `"Bad Request"` / `"Internal Server Error"` titles
- Legacy `PlatformHttpException.Title` passthrough presentation

## Residual references

| Surface | Result |
|---|---|
| Host production `*.cs` | ZERO `PlatformExceptionMapper` / `MappedPlatformError` |
| Host.Tests production references | ZERO (tests assert absence) |
| Shim / alias / replacement mapper | NONE |

## Errors production shape

```text
Errors/
  ToobaExceptionHandler.cs
```

Exact production file count = 1.
