# TB-281-R4 schema and runtime

## Schema

- ZERO migration/DDL/seed/backfill this task
- Prior R3 Story migration `20261119020000_Tb281R3TenantScopedCommandOccurrences` left immutable
- Media IntentHash / occurrence tables unchanged

## Runtime targets

- DB: `tooba_alpha` @ `127.0.0.1:5432`
- Commerce: SingleStore `store-alpha`
- Host: `http://127.0.0.1:5088` with `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`
- FE: `http://127.0.0.1:3000` (transport-only refresh if rebuilt)

Live Story/Media business POST/upload: ZERO.
