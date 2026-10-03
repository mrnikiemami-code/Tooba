# TB-TMAR-PLATFORMPROBE-AMC-001-W3 — Migration order retirement

## Change

Removed `ModuleSchemaMigrationOrder.PlatformProbe = 13`.

## Intentional gap

Order **13** is retired and not reused. Later constants retain pre-W3 values:

| Constant | Value |
|---|---|
| Promotion | 12 |
| Reviews | 14 |
| ProductQnA | 15 |
| … | … |
| Support | 29 |

## Database

No DROP. No compensating migration. Deployed `platform_probe` may remain orphaned.
