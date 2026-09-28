# TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001 — Bootstrap Semantics (DEBT B)

## Correction

`ConfiguredAuthorizationSchemaBootstrapper.BootstrapIfConfiguredAsync` (file
`Authorization/SpiceDbAuthorizationBootstrapper.cs`):

Before: `_appliedVersion = _schema.SchemaVersion;` was assigned **before** the gRPC write.
After: it is assigned **only after** `adapter.WriteSchemaAsync(...)` returns successfully.

```csharp
if (!_options.ApplySchemaOnStartup) { return; }

_logger.LogInformation(
    "Authorization schema bootstrap requested. Version {SchemaVersion}. Token is not logged.",
    _schema.SchemaVersion);

if (!string.Equals(_options.Mode, "SpiceDb", StringComparison.Ordinal) || _services is null)
{
    _logger.LogInformation(
        "Authorization schema bootstrap skipped: no SpiceDB write. Mode {Mode}.",
        _options.Mode);
    return;
}

var adapter = _services.GetRequiredService<SpiceDbAuthorizationAdapter>();
await adapter.WriteSchemaAsync(_schema.SchemaText, cancellationToken);

_appliedVersion = _schema.SchemaVersion;
_logger.LogInformation(
    "Authorization schema applied. Version {SchemaVersion}.",
    _schema.SchemaVersion);
```

## Resulting semantics matrix

| Situation | `AppliedVersion` |
| --------- | ---------------- |
| `ApplySchemaOnStartup = false` | `null` |
| requested, `Mode != SpiceDb` (or no services) — no real write | `null` |
| requested, `Mode = SpiceDb`, `WriteSchemaAsync` throws/fails | `null` |
| requested, `Mode = SpiceDb`, write succeeds | `SchemaVersion` (3) |

## Log wording

The REQUESTED message is unchanged ("bootstrap requested … Token is not logged") so it still
signals the request, and it is now followed by exactly one terminal outcome message:
`skipped: no SpiceDB write` or `applied`. REQUESTED vs APPLIED is therefore distinguishable, and
the token is never logged.

No behavior change outside this semantic correction. `IHostedService` wiring
(`AuthorizationSchemaHostedService`) and the `IAuthorizationSchemaBootstrapper` contract are
untouched; the contract already declared `int? AppliedVersion { get; }`.
