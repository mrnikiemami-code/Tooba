# 03 — Production validation / fail-fast (TB-P06-T005)

## Validator

`AuthorizationOptionsValidator` registered in `Program.cs` as `IValidateOptions<AuthorizationHostOptions>`.

## Rules

| Condition | Result |
|---|---|
| Mode not in `Disabled` / `InMemory` / `SpiceDb` | Fail at options validation |
| Production + `Mode=InMemory` | Fail — `"InMemory authorization is not allowed in Production."` |
| `Mode=SpiceDb` + empty `Endpoint` | Fail |
| `Mode=SpiceDb` + empty `Token` | Fail |
| `TimeoutSeconds <= 0` | Fail |
| `RetryMaxAttempts <= 0` | Fail |
| `RetryBaseDelayMilliseconds < 0` | Fail |
| `ConsistencyMode` not `FullyConsistent` or `MinimizeLatency` | Fail |
| Production + `UseTls=false` | Fail — `"SpiceDB TLS must be enabled in Production."` |

## Readiness pre-checks (config-only, before probe)

`HostReadinessEvaluator` returns early when SpiceDB mode selected but endpoint/token missing:

| Check label | Meaning |
|---|---|
| `authorization=spicedb-endpoint-missing` | Endpoint blank |
| `authorization=spicedb-token-missing` | Token blank |
| `authorization=spicedb-unreachable` | `SpiceDbHealthProbe.CheckAsync` returned false |

## Secret safety

- Token stored in config reference only; never logged by bootstrap or adapter.
- **Proof:** `AuthorizationFoundationTests.Authorization_token_is_not_logged_by_bootstrap`.

## Tests

| Test | Covers |
|---|---|
| `SpiceDbIntegrationTests.Production_rejects_insecure_tls_and_inmemory_mode` | Production InMemory + no-TLS rejection |
| `AuthorizationFoundationTests.SpiceDb_mode_without_endpoint_fails_validation` | Missing endpoint |
