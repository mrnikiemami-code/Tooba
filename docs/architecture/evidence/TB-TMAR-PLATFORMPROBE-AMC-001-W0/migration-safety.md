# TB-TMAR-PLATFORMPROBE-AMC-001-W0 — Migration safety

## Authoritative order (runtime Development seam)

`ModuleSchemaMigrationOrder.PlatformProbe = 13` in `Tooba.Persistence/ModuleSchemaMigrator.cs`.

Registered by `PlatformProbeModule` via:

`AddModuleSchemaMigrator<PlatformProbeDbContext>("PlatformProbe", ModuleSchemaMigrationOrder.PlatformProbe)`.

Discovery is DI-based (`IModuleSchemaMigrator`); Host Development migrator does **not** hard-code `PlatformProbeDbContext` (guarded).

## MigrationRunner registry

`Tooba.MigrationRunner/ModuleMigrationRegistry.All` includes an explicit:

`Descriptor<PlatformProbeDbContext>("PlatformProbe", PlatformProbeDbContext.Schema)`

List position is after Promotion and before Reviews in the array (surrounded by other modules whose relative array order differs from the numeric `ModuleSchemaMigrationOrder` constants). Runner applies descriptors in list order for tooling migrations.

## Schema / tables

- Schema: `platform_probe`
- Tables: `probe_records`, `outbox_messages` (plus EF `__EFMigrationsHistory` in that schema)
- Migrations in source:
  - `20260823000054_InitialPlatformProbe`
  - `20260823010100_AddPlatformProbeOutbox`

## Contiguous count assumption?

Guards assert **29** module composition roots register `AddModuleSchemaMigrator` (`HostDevelopmentMigrationSeamGuardTests`). That count **includes** PlatformProbe. Detach requires updating that inventory/count — not a hard contiguous DB sequence requirement.

Numeric order constants for later modules (Reviews=14 …) do **not** require PlatformProbe to remain registered for migrations to apply; uniqueness of remaining registered orders is what matters at runtime.

## Safe detach guidance

| Action | Safety |
|---|---|
| Stop registering PlatformProbe migrator / remove from Host composition | **SAFE** for future runtime — no business path uses the schema |
| Remove MigrationRunner descriptor | **SAFE** for future tooling runs |
| Leave already-deployed `platform_probe` schema/tables orphaned | **PREFERRED** — non-destructive |
| Delete historical migrations from source while environments already applied them | **UNSAFE** for any tool that still targets that DbContext; if DbContext leaves production, keep migrations with test fixture or archive, do not invent DROP |
| `DROP SCHEMA platform_probe` | **NOT recommended** — architecture prefers preservation; no evidence requires drop |
| Change MessagingOptionsValidator forbid of `platform_probe` | Optional later; keeping the forbid is harmless |

## Verdict

`SAFE_TO_DETACH_KEEP_SCHEMA` for production runtime removal, with **bounded compatibility**: preserve historical migration artifacts for test fixture / archive; do not delete deployed schema; update SoT migration-order narrative and the “29 modules” guard when detach executes.
