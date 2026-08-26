# 08 — Schema bootstrap DI fix (TB-P06-T005)

## Problem (pre-T005)

`ConfiguredAuthorizationSchemaBootstrapper` logged schema version on startup but did not resolve live `SpiceDbAuthorizationAdapter` — schema never applied to running SpiceDB when `ApplySchemaOnStartup=true`.

## Fix

`AuthorizationRegistration` registers bootstrapper with `IServiceProvider`:

```csharp
services.AddSingleton<IAuthorizationSchemaBootstrapper>(sp =>
    new ConfiguredAuthorizationSchemaBootstrapper(
        sp.GetRequiredService<IOptions<AuthorizationHostOptions>>(),
        sp.GetRequiredService<IAuthorizationSchemaProvider>(),
        sp.GetRequiredService<ILogger<ConfiguredAuthorizationSchemaBootstrapper>>(),
        sp));
```

## Apply conditions (all required)

1. `ApplySchemaOnStartup=true`
2. `Mode=SpiceDb`
3. `IServiceProvider` injected (Host registration)

When conditions met → `adapter.WriteSchemaAsync(schemaText)`.

## Production guard

Default `ApplySchemaOnStartup=false`. Production schema changes via ops runbook (`docs/operations/authorization-spicedb.md`), not blind Host restart.

## Startup service

`AuthorizationSchemaHostedService` invokes `BootstrapIfConfiguredAsync` once in `StartAsync`.

## Tests

| Test | Covers |
|---|---|
| `AuthorizationFoundationTests.Schema_bootstrap_is_versioned_and_opt_in` | Opt-in gate + version tracking |
| `SpiceDbIntegrationTests` (schema write helper) | Live WriteSchema against Testcontainers |
