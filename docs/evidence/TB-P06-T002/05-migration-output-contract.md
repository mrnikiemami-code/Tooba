# 05 — Migration output contract (TB-P06-T002)

## CLI JSON stdout (per target)

Emitted by `Program.cs` after orchestration:

```json
{
  "command": "Status|Plan|Apply",
  "edition": "Marketplace|SingleStore",
  "tenantId": "<tenant-id-or-null>",
  "database": "<logical-database-name>",
  "connectionReference": "<logical-ref-key>",
  "modules": [
    {
      "module": "Catalog",
      "schema": "catalog",
      "currentMigration": "20260826173000_EnsureBrandLogoMediaAssetIdColumn",
      "pendingMigrations": [],
      "succeeded": true,
      "durationMs": 42
    }
  ]
}
```

## Structured logs (JSON console)

`MigrationOrchestrator` emits per-module structured log fields:

- `command`, `module`, `edition`, `tenantId`, `database`, `connectionRef`, `schema`
- `current`, `pendingCount`, `durationMs`, `result`, `traceId`

## Never printed

- Password / token values
- Raw secret-bearing connection strings
- Full Npgsql connection string with credentials

## Safe identifiers only

- Edition name
- Tenant/store id (from control plane registry)
- Connection **reference key** (not resolved secret)
- Logical database name (from connection builder `Database` property)

## Trace / correlation

`Activity.Current?.TraceId` when available; otherwise generated GUID for log correlation.
