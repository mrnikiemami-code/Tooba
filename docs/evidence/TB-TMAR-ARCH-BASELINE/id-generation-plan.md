# ID Generation Plan

Target: `IIdGenerator` / `IUuidGenerator` at orchestration boundaries.

- Domain receives IDs explicitly when creation is orchestrated outside
- Forbid new direct `Guid.NewGuid` / `UuidV7.New` in Domain/Application after gate (allowlist legacy)
- Deterministic tests via fake generator
