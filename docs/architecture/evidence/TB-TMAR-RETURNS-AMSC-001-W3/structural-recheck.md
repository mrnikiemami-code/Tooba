# Structural re-check (defense in depth — Certify does not own Structure)

The Structure PASS is current and scoped to `src/backend/Modules/Returns`. Certify re-derived the
structural invariants from disk anyway; the results agree with W2.

| Invariant | W2 verdict | W3 re-check |
|---|---|---|
| Application root folders | `Composition`, `ReturnRequests`, `Validation` | identical |
| Retired technical-axis roots (`Commands`,`Queries`,`Models`,`Ports`,`Validators`,`Errors`,`Handlers`,`Requests`) | absent | absent |
| `ReturnRequests` subfolders | `Commands`, `Models`, `Ports`, `Queries` | identical |
| Subfolders under `Commands` / `Queries` | none (no single-file request leaves) | none |
| Path ↔ namespace | EXACT, 71 production `.cs`, 0 mismatches | 0 mismatches |
| Root allowlists | `[]` x5 + `Endpoints: ["ReturnEndpointModule.cs"]` | matches disk |
| `/Modules/Returns/` solution grouping | 6 projects | 6 projects |
| Stale/duplicate copies | CLEAN | CLEAN |
| Host final closure | PRESERVED | PRESERVED (no new Host production folder/file) |

No structural regression was found, so the W2 PASS remains valid for this surface. Certify did not
weaken, override or re-issue the Structure verdict; the structural gates above are recorded here only
as corroboration.
