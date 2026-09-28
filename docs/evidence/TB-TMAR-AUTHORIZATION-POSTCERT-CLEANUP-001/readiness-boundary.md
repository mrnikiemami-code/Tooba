# TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001 — Readiness Boundary (DEBT A)

## New narrow contract — `Tooba.AccessControl.Contracts/Readiness/`

File: `AuthorizationReadinessContracts.cs`, namespace `Tooba.AccessControl.Contracts.Readiness`
(exact path-derived).

```csharp
public sealed record AuthorizationReadiness(bool Ready, string CheckLabel);

public interface IAuthorizationReadinessProbe
{
    Task<AuthorizationReadiness> EvaluateAsync(CancellationToken cancellationToken);
}
```

The contract exposes only two safe things: readiness boolean and the existing non-secret check
label. It exposes **no** endpoint, token, `SpiceDbConnectionOptions`, or Authzed SDK type.

## Module implementation

File: `Tooba.AccessControl.Infrastructure/Adapters/AuthorizationReadinessProbe.cs`.

- Owns the mode decision, endpoint/token pre-checks, and the optional lightweight SpiceDB probe.
- Preserves the exact pre-existing labels and semantics:

  | Condition | Ready | Label |
  | --------- | ----- | ----- |
  | Mode not `SpiceDb` (e.g. `Disabled`, `InMemory`) | true | lower-cased mode |
  | `SpiceDb` + blank endpoint | false | `spicedb-endpoint-missing` |
  | `SpiceDb` + blank token | false | `spicedb-token-missing` |
  | `SpiceDb` + `ReadinessProbeEnabled=false` | true | `spicedb` (no remote probe) |
  | `SpiceDb` + probe enabled + unreachable | false | `spicedb-unreachable` |
  | `SpiceDb` + probe enabled + reachable | true | `spicedb` |

- Registered in `AccessControlModule.AddServices` as scoped
  `IAuthorizationReadinessProbe` → `AuthorizationReadinessProbe`.

## Host consumption

- `HostReadinessEvaluator.EvaluateAsync` parameter changed from
  `SpiceDbAuthorizationOptions` to `IAuthorizationReadinessProbe` and now only projects the result:

  ```csharp
  var readiness = await authorizationReadiness.EvaluateAsync(cancellationToken);
  checks["authorization"] = readiness.CheckLabel;
  if (!readiness.Ready) { return new Evaluation(false, checks); }
  ```

- `HostHealthEndpoints.EvaluateReadinessAsync` injects `IAuthorizationReadinessProbe` instead of
  `IOptions<SpiceDbAuthorizationOptions>`.
- `Program.cs` no longer imports `Tooba.AccessControl.Infrastructure.Authorization`.

Host readiness → `Tooba.AccessControl.Infrastructure.Authorization` = **ZERO**;
Host readiness → narrow `Tooba.AccessControl.Contracts.Readiness` seam = **YES**.

## Preserved behavior

- All non-authorization checks (edition, postgresql references, messaging bus health) unchanged.
- Response shape and labels unchanged; readiness still returns 503 when not ready.
- No secret/token/endpoint is emitted in readiness output or logs.
- Host composition still goes through `AddToobaModules`; Host never calls `AddToobaAuthorization()`.
