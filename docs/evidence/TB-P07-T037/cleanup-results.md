# Cleanup Results

`POST /v1/admin/catalog/demo/assignment-integrity/cleanup` (gated like reset-and-seed):

- Idempotent on current demo: removed 0 invalid rows; after audit still all zeros
- Cleanup rules: remove invalid Additional L1/L2; remove Additional==Primary; repair Primary only when exactly one valid L3 Additional exists; otherwise delete product + Catalog-owned dependents
- Wired into reset+seed via `EnsureCleanOrThrowAsync`
