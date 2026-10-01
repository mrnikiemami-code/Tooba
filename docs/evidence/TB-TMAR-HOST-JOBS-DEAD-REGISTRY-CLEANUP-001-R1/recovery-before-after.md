# Recovery before / after — TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1

## Before

- `lastAcceptedTask` / wave / checkpoint pointed at `TB-TMAR-HOST-COMPOSITION-AMC-001-R1` / `Composition`.
- Implementation commit `913ce3ca6c7e89a04f6ed9ebb74d1bc8a8660179` already removed dead write-only registry, but Recovery/SoT still named Composition as current.

## After

- `lastAcceptedTask` = `TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1`
- `lastAcceptedCommit` = `913ce3ca6c7e89a04f6ed9ebb74d1bc8a8660179` (`IMPLEMENTATION_COMMIT`)
- `currentHostCheckpoint` / `activeModule` = `Jobs`
- `workflowStop` / `nextTask` = `USER_REVIEW_HOST_JOBS_DEAD_REGISTRY_CLEANUP_001_R1`
- `automaticNextImplementationTask` = `NONE`
- `staleCurrentPointerState` = `ZERO`
- Composition AMC R1 retained as historical lineage (`hostCompositionAmcR1` intact)
- Durable Jobs history: `hostJobsDeadRegistryCleanup` + `hostJobsDeadRegistryCleanupR1`
