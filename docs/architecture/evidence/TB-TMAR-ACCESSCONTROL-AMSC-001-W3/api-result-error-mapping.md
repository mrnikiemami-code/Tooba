# API result / error mapping — AccessControl (W3)

## Classification

| Aspect | State |
| --- | --- |
| Canonical factory | `ApiResponseFactory` (`api.From(...)`) |
| `RAW_RESULTS` | `ZERO` |
| `AD_HOC` mapper | `ZERO` |
| `PARALLEL_MAPPER` | `ZERO` |
| `Results.Json` / `Results.BadRequest` / `Results.Problem` | `ZERO` |
| Local `ProblemDetails` builder | `ZERO` |
| Endpoint `catch`-and-map for expected failures | `ZERO` |
| Failure classification by parsing `ex.Message` | `ZERO` |
| `code.Contains(...)` heuristic | `ZERO` |

## Mapping path

1. Endpoints call `api.From(...)` for the success/expected path.
2. Application handlers return `Result` / `Result<…>`.
3. Expected failures are produced **once** in
   `Application/Composition/AccessControlOperation.ExecuteAsync` as
   `Result.Failure<T>(new SemanticError(ex.Code))`.
4. `AccessControlException` carries only a stable code (`AccessControlException(string code)`), never
   message prose — asserted by `AccessControlModuleAmcW2SemanticGuardTests`.
5. HTTP status/classification/localization resolution happens in the shared presentation stack from
   the canonical `ErrorDescriptor` (see `localization-catalog.md`).

## Unknown-exception behaviour

Unexpected exceptions propagate. There is no catch-all converting unknown failures into business
failures, and no duplicate-suppression/overwrite mechanism.

## Success shape

Raw DTO success bodies are preserved exactly as shipped. No wrapper/envelope was introduced by the
AMSC run.

## Non-production note

`Endpoints/Errors/AccessControlHttpErrors.cs` (17 LOC) is an unused helper retained in the module —
it is **not** referenced by any production code path (only by a negative guard assertion). It is
therefore not an in-use parallel mapping path. Recorded as non-blocking residual debt in
`residual-debt.md`; removing it was deliberately deferred because it is not part of the W1
cohesion/ownership defect set and removing production files is outside a behaviour-preserving
structure certification.

## Verification method

Repo-wide scan of `Tooba.AccessControl.*` production `.cs` for
`Results.(Json|BadRequest|Problem)`, `new ProblemDetails`, `.Message.StartsWith|Contains|Equals`,
`when (… .Message)` and `"access.*"` raw literals returned **zero** hits (see `validation.md`).
