# CORRECTION-ADDENDUM — TB-282-R8

- R7 unknown-create fact stopped before recovery click; R8 proves replay adopts the bound draft.
- createDraft non-ok path now refreshes pendingActs so recovery UI mounts without remount.
- Host skip-bootstrap no longer leaves Snapshot null when the admin actor already exists in Identity; EnsureAsync remains the write path and is unused by GET /v1/admin/dev-context.
- Live Story list still fail-closed under empty InMemory authorization when bootstrap is skipped; that is disclosed, not bypassed with grants.
