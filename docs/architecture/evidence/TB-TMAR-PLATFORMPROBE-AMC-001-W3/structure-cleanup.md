# TB-TMAR-PLATFORMPROBE-AMC-001-W3 — Structure cleanup

## Removed

- Entire `src/backend/Modules/PlatformProbe/` tree
- `Tooba.slnx` PlatformProbe project entry
- `ModuleSchemaMigrationOrder.PlatformProbe = 13` (+ comment)

## Preserved

- Host.Tests `Fixtures/PlatformProbe` (TEST_ONLY)
- Deployed `platform_probe` schema (no DROP)
- Later migration-order constants unchanged (Reviews=14 … Support=29)
- Historical recoverability via git + W0–W3 evidence

## Structure handoff

`READY_FOR_FINAL_ABSENCE_CERTIFICATION` (W4 owns final cert; `structureCertified` remains false).
