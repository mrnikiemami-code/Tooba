# tenant-host-normalization — TB-TMAR-HOST-CONFIGURATION-AMC-001

## Host normalization

Uses BuildingBlocks `HostNormalizer.TryNormalize` (not local duplicate).

Covered by existing `HostNormalizerTests` + BuildRegistry invalid-host fail-fast.

BuildRegistry:

- normalizes each Host entry
- rejects invalid host
- rejects duplicate normalized host across tenants
- optionally adds `PrimaryDomain` into host list if missing
- requires ≥1 host mapping per tenant
- sets `PrimaryDomain` to normalized primary or first host

## TenantId

Dictionary key = raw `TenantRecordOptions.TenantId` string (`Ordinal` comparer). Empty rejected. Duplicate ID rejected. **No explicit trim/case-fold on key** before insert — casing variants can dual-register (debt). `TenantId` value object wraps raw string for record field.

## Tenant status

`ParseStatus`: Active (default/blank), Disabled, Suspended; unknown → `InvalidOperationException` (fail-fast). Case-insensitive.

## Edition parsing

`TryParseEdition`: Unset/blank, Marketplace, SingleStore / Single-Store; unsupported → false → validation fail. Case-insensitive.

## DeploymentId

Blank → falls back to `edition.ToString()` in BuildRegistry. Trimmed when set. **No Production-required DeploymentId** today. Telemetry consumers read registry/edition deployment label elsewhere.
