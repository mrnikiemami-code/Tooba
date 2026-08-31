# TB-P07-T041-R1 — Focused validation

## Compile

Isolated Host build (avoids live `:5088` DLL lock):

```text
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -o artifacts/t041-r1-build
→ 0 Error(s)
```

## Backend focused

```text
dotnet test Host/Tooba.Host.Tests --filter "FullyQualifiedName~AdminDbNativeGridQueryTests|FullyQualifiedName~AdminListGridQueryEngineTests"
```

Covers: Content EF InMemory paging/filter, Normalize rejects invalid field, composer source no Execute, engines use PageAsync/Count+Skip+Take.

## Frontend focused

```text
node --experimental-strip-types --test app/admin/admin-grid-migration.test.ts design-system/app-data-grid/legacy-grid-bridge.test.ts
```

## Guard

```text
node docs/ai/recovery-staleness.guard.test.mjs
```

## Not run

Repository-wide full suites (per task).
