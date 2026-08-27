# 12 — FE/test blockers (parallel)

Task: TB-P06-T025

## Status at FE worker checkpoint

| Item | State |
|------|-------|
| FE pages + Shopeiva UI | Done (working tree) |
| Nav un-defer + support.view | Done |
| Evidence 04–11, 17–18 | Done |
| Host.Tests source-scan | `SupportFoundationTests.cs` present; activates when module files exist |
| Typed authz/integration | **Blocked** — `Modules/Support` and `Host/Support` not yet on disk |
| PermissionCatalog support.* | **Blocked** — not yet in catalog |
| `dotnet test` Host.Tests | **Blocked** — Tooba.Host (pid) locks bin DLLs; do not stop Host per task |

## Seed IDs

Pending sibling seed + `demo-preview` payload.
