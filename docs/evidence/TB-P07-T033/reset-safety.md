# Reset safety (TB-P07-T033)

## Guards (fail closed)

Reset/seed aborts **before mutation** when any of these hold:

1. `IHostEnvironment.IsProduction()` → blocked
2. `Tooba:CatalogDemo:AllowResetAndSeed` is not `true` → blocked
3. Environment is neither `Development` nor `Testing` → blocked

Safety is **not** inferred from database name.

## Entry points

- Application host: `CatalogDemoResetAndSeedHost.EnsureSafetyOrThrow()` / `ExecuteAsync`
- HTTP (non-Production only): `POST /v1/admin/catalog/demo/reset-and-seed`

## Scope

Deletes only demo/junk Catalog (+ demo Media by `OriginalFileName` prefix `demo-media-`). Does not touch auth, orders, sellers, or unrelated Content.
