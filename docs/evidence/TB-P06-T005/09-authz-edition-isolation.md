# 09 — Versioned schema foundation (TB-P06-T005)

## Version

Schema **v2** (`FoundationAuthorizationSchemaProvider.SchemaVersion => 2`).

## Artifacts

| Path | Role |
|---|---|
| `src/backend/Host/Tooba.Host/authorization-foundation.zed` | Versioned Zed source (ops / review) |
| `FoundationAuthorizationSchemaProvider.SchemaText` | Runtime embedded mirror |

## Definitions

```zed
definition user {}

definition tenant {
  relation member: user
  permission view = member
}

definition party {
  relation member: user
  permission view = member
}
```

## Scope boundary

Foundation only — user + tenant + party membership. Catalog/Order/product permissions are future schema versions.

## Apply process

1. Review diff against deployed schema
2. Apply via ops tooling or controlled `ApplySchemaOnStartup` in non-prod
3. Bump `SchemaVersion` when definitions change

## Not present

- Automated schema drift detection job
- Multi-schema tenant isolation (single SpiceDB deployment assumed)
