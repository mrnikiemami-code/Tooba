# TB-281-R5 schema and runtime

## Schema

- Migrations added this task: **NONE**
- Applied R3 tenant-scoped `story.command_occurrences` PK `(tenant_id, occurrence_key)` unchanged

## Runtime

- Host: PID `18716`, publish `artifacts/publish-primary`, dll SHA256 `917D16EDAB76B1D08B9DF06D27F2E96B6EFA5ECB96B9B1D2B6BC894D594E85B3`
- `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`
- `/health` 200; `/health/ready` 200 (`edition=SingleStore`, messaging Healthy)
- `/__platform-commerce` → `store-alpha`
- Live Story/Media business writes: **ZERO**
- FE transport unchanged this task (no FE delta required for A/B/C)
