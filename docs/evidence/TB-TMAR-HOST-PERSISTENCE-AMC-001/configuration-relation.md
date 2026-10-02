# configuration-relation — TB-TMAR-HOST-PERSISTENCE-AMC-001

## Authority split

| Concern | Owner |
| --- | --- |
| Connection reference catalog (`PostgreSQL.ConnectionReferences`) | Host **Configuration** (`ToobaPlatformOptions` / `PostgreSqlOptions`) |
| Reference → connection string resolve | Host **Persistence** (`DatabaseConnectionResolver`) |
| Startup fail-fast for edition-required references | `PlatformOptionsValidator` (Production only) |
| Readiness probe of configured references | Host **Health** (`HostReadinessEvaluator`) — separate from resolver |

No duplicate configuration source inside Persistence. No env-var lookup inside resolver.

## Startup fail-fast relation

`PlatformOptionsValidator.CollectConfiguredConnectionReferences`:

- Marketplace: marketplace connection reference
- SingleStore: **all** tenant connection references (not only Active)

Production requires each collected reference present + non-empty string. **Does not** currently parse via `NpgsqlConnectionStringBuilder` at startup.

Therefore:

| Condition | Runtime after successful Production validate |
| --- | --- |
| Missing edition-required reference | Should be impossible |
| Blank edition-required string | Should be impossible |
| Malformed syntax | Still possible — resolver defensive |
| Non-Production / partial catalog | Runtime miss still possible — resolver defensive |
| Messaging / ad-hoc references not in edition collect set | May miss startup check — resolver defensive |
| Active-only vs all-tenant | Resolver is **policy-neutral** (resolves supplied reference only) |

## Active vs all tenant semantics

| Surface | Policy |
| --- | --- |
| MultiTenancy / request path | Active tenant selection elsewhere; resolver neutral |
| Outbox | Active poll targets; resolver neutral |
| Health | Collects configured references for readiness |
| PlatformOptionsValidator | Edition-required set (SingleStore = all tenants) |
| Resolver | **POLICY_NEUTRAL** |

## Legacy root `PostgreSQL.ConnectionString`

| Fact | State |
| --- | --- |
| Property exists on `PostgreSqlOptions` | YES — documented legacy compatibility |
| Used by `DatabaseConnectionResolver` | **NO** (intentionally ignored) |
| Production consumer of root property via Persistence | **ZERO** |
| Cleanup owner | Future **Configuration** task — not Persistence Analyze |

**Legacy-Root-ConnectionString-State** = `INTENTIONAL_COMPATIBILITY_RESIDUE_IGNORED_BY_RESOLVER`

## Error code ownership

| Code | Owner |
| --- | --- |
| `platform.connection.unconfigured` | Foundation (`FoundationErrorCodes` + `FoundationErrors(.fa).resx`) |
| Duplicate codes | NONE observed |
| Message-text classification | ZERO |
