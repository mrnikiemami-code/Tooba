# 09 — Single-Store tenant isolation proof (TB-P06-T002)

## Model

- One shared publish / control plane
- Tenant resolved from CLI flags (not hostname at migration time)
- Database per tenant via distinct `ConnectionReference` entries

## Target selection (CLI)

| Mode | Flags | Default safety |
|---|---|---|
| Single tenant | `--tenant <id>` | Safe — explicit |
| Tenant set | `--tenants id,id` | Safe — explicit |
| All active tenants | `--all-tenants` | Safe — requires explicit flag |
| No flags | (none) | **Rejected** exit 3 — prevents accidental all-tenant migration |

## Integration test proof

`Single_tenant_apply_does_not_touch_other_tenant_database`:

1. Create two databases: `tenant_a_db`, `tenant_b_db`
2. Configure two tenants with separate connection references
3. Apply migrations for tenant A only
4. Assert `catalog.__ef_migrations_history` count in tenant B DB = **0**

## Per-tenant reporting

Runner iterates resolved targets sequentially; each target emits separate JSON payload with `tenantId` and `database` fields.
