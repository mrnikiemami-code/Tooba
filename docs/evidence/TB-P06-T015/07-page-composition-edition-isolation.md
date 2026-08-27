# 07 — Edition / tenant isolation (TB-P06-T015)

| Concern | Implementation |
|---|---|
| Tenant scope | `PageDefinition.TenantId` (Guid); store-alpha / store-beta fixed Guids + hashed keys |
| Seed | Idempotent per tenant in Marketplace + ProductWorkspace bootstraps |
| Public GET | Tenant from `ICurrentTenant` |
| Admin mutations | `AdminPanelAccess` + SpiceDB ReBAC + current tenant |
| Isolation test | Foundation tests: two tenants → independent compositions |

## Edition notes

- Single-Store: per-tenant DB resolution unchanged.
- Marketplace: composition remains tenant-scoped; no platform-wide shared home definition in MVP.
- No cross-tenant leakage of section order/visibility.
