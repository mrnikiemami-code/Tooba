# 04 — SpiceDbHostOptions baseline (TB-P06-T005)

Section: `Tooba:Authorization:SpiceDb` (nested under `AuthorizationHostOptions`).

| Key | Default | Purpose |
|---|---|---|
| `Endpoint` | `""` | gRPC/HTTP SpiceDB address |
| `Token` | `""` | Bearer preshared key (env/secret ref) |
| `UseTls` | `true` | TLS for non-local endpoints |
| `TimeoutSeconds` | `5` | gRPC deadline per call |
| `RetryMaxAttempts` | `3` | Max infrastructure retry attempts |
| `RetryBaseDelayMilliseconds` | `100` | Linear backoff multiplier (`delay * attempt`) |
| `ConsistencyMode` | `FullyConsistent` | Default when no ZedToken on check |
| `ReadinessProbeEnabled` | `true` | Enable `ReadSchema` probe in readiness |

## Parent options

| Key | Default | Purpose |
|---|---|---|
| `Tooba:Authorization:Mode` | `Disabled` | Engine selection |
| `Tooba:Authorization:ApplySchemaOnStartup` | `false` | Opt-in schema write on startup |

## Source files

| File | Role |
|---|---|
| `AuthorizationHostOptions.cs` | Options + validator |
| `appsettings.json` | Dev defaults documented |

## Production example (ops)

```json
"Authorization": {
  "Mode": "SpiceDb",
  "ApplySchemaOnStartup": false,
  "SpiceDb": {
    "Endpoint": "spicedb.internal:443",
    "Token": "${SPICEDB_TOKEN}",
    "UseTls": true,
    "TimeoutSeconds": 5,
    "RetryMaxAttempts": 3,
    "RetryBaseDelayMilliseconds": 100,
    "ConsistencyMode": "FullyConsistent",
    "ReadinessProbeEnabled": true
  }
}
```

Apply schema via controlled ops process (`authorization-foundation.zed`) — not blind startup overwrite in production.
