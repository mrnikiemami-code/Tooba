# Residual non-blocking debt — AccessControl (W3)

None of the items below is an `ARCH-COMPLETE-002` violation of the AccessControl surface. They are
recorded honestly for Architect visibility.

## R1 — `AccessControlDirectory.cs` `OVERSIZED_ONLY` (WATCH, baselined)

| Field | Value |
| --- | --- |
| File | `Infrastructure/Directories/AccessControlDirectory.cs` |
| LOC | 968 (W0: 965, delta `+4`) |
| Responsibility | one cohesive directory behind `IAccessControlDirectory` |
| Blocking? | No — `tooba-architecture-structure` §12 allows `OVERSIZED_ONLY` as WATCH |
| Prior state | `accessControlDirectoryState = OVERSIZED_ONLY_WATCH` (preserved from `accessControlStructureRecert001`) |

The `+4` LOC is a pure constant-name substitution (`"access.role.code_conflict"` →
`AccessControlErrorCodes.RoleCodeConflict`) plus one added `using`. No new responsibility, method or
branch. A later split into per-capability directory partials is a **recommended future
consolidation**, not a certification blocker.

## R2 — `Endpoints/Errors/AccessControlHttpErrors.cs` unused (17 LOC)

| Field | Value |
| --- | --- |
| File | `Endpoints/Errors/AccessControlHttpErrors.cs` |
| Referenced by production code | No (only by a negative guard assertion) |
| Blocking? | No — it is not an in-use parallel mapping path |
| Reason not removed | Removing production files is outside a behaviour-preserving structure/cohesion run and was not part of the W0 defect set (`F1`–`F4`) |

Recommendation for a future authorized task: delete the file, or wire it into the canonical
mapping if a real need appears.

## R3 — Stale size-baseline path key (shared baseline, not AccessControl-owned)

`src/backend/Host/Tooba.Host.Tests/Baselines/tmar-source-size-baseline.json` still lists

```text
src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlDirectory.cs   (loc 957)
```

That path does not exist; the real file is
`…/Infrastructure/Directories/AccessControlDirectory.cs` (969 LOC). The guard therefore reports a
`BASELINE_ENTRY_MISSING_FILE` plus a `NEW_OVERSIZED_FILE` for the same file.

| Field | Value |
| --- | --- |
| Blocking? | No — no duplicate physical copy exists; the file has exactly one home |
| Owner | shared size baseline |
| Also stale in the same baseline (other modules) | `Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs` (deleted), `Host/Tooba.Host/Admin/AdminOrderCompletenessComposer.cs`, `Host/Tooba.Host/Admin/AdminOrderOperationsComposer.cs`, `Modules/Catalog/*`, `Modules/Fulfillment/*`, `Modules/Order/*`, `Modules/Settlement/*` |

Repairing it would require re-keying baseline entries across Host and several unrelated modules —
outside this task's authorized scope.

## R4 — Stray untracked `.tmp-baseline/` working copy

| Field | Value |
| --- | --- |
| Path | `.tmp-baseline/` (repo root) |
| Tracked by git | No — matched by `.gitignore` line 37 (`.tmp-*`) |
| Effect | `TmarSourceSizeGuard.ShouldExcludeRelativePath` excludes `.tmp-*` **file names** but not directory names, so the whole stale tree is walked and reported as `NEW_OVERSIZED_FILE` |
| Blocking? | No — it is not repository source |
| Repair options | (a) delete the local `.tmp-baseline/` directory; (b) extend `ExcludedDirNames`/`ShouldExcludeRelativePath` to skip `.tmp-*` directories — a **shared guard** change, outside AccessControl scope |

This is a local working-copy artefact, not a repository structure defect.

## R5 — Three pre-existing repo-wide guard expectation drifts (shared)

| # | Guard | Cause | Owner |
| --- | --- | --- | --- |
| 1 | `TmarCompleteReferenceStructureGateTests` | 21-module expectation omits `Catalog`; manifest declares 22 | shared gate |
| 2 | `TmarCompleteReferenceStructureGateTests` | `Tooba.Catalog.Contracts/Cart/*.cs` namespace drift | `Catalog` module |
| 3 | `TmarDurableGuardTests` | stale 16-module `structureLock.certifiedModules` expectation vs 21-entry SoT | shared gate / SoT |

All three reproduce identically at the W0 baseline `a3ba1a4f`. Repairing them means editing the
shared structure gate, the `Catalog` module and the repo-wide SoT — outside this task's authorized
scope.

## R6 — No `Tooba.AccessControl.Tests` project (W0 `F5`)

The module has no dedicated test project; its behavior and guard tests live in
`Tooba.Host.Tests`. Sibling modules `Offer` and `Order` have their own `*.Tests` projects.

| Field | Value |
| --- | --- |
| Blocking? | No — `ARCH-COMPLETE-002` does not require a per-module test project |
| Note | The manifest correctly declares 5 production projects and does not invent a tests entry |

## R7 — `IClock` / `IIdGenerator` adoption (W0 canonical-mechanisms observation)

`AccessControlDirectory` uses `DateTimeOffset.UtcNow` + `Guid.NewGuid()`, matching the prevailing
repository convention for directory implementations (`CatalogDirectory`, `CartDirectory`). Adopting
`IClock`/`IIdGenerator` (as newer `Adapters/*DevelopmentSeedGateway` code does) would be a
determinism improvement but would change timestamp sourcing and id generation — i.e. it is **not**
behaviour-preserving and is out of scope for an AMSC run.

## R8 — `GetSellerDevContextsQuery` outside the 19-request guard inventory

The `AccessControlValidatorTests` inventory covers 19 endpoint-reachable requests on the
production Admin/Seller surface. The development-only `GetSellerDevContextsQuery` (20th request,
`GET /dev-contexts`) is not in that inventory.

| Field | Value |
| --- | --- |
| Blocking? | No — `NO_VALIDATOR_REQUIRED` (no transport input beyond ambient seller identity) |
| Recommendation | Extend the inventory to 20 requests in a future authorized task if development surfaces are to be guard-covered |

## Summary

```text
blocking residual debt = ZERO
non-blocking WATCH items = 8 (R1–R8)
```

All eight are either architectural `WATCH` items, non-production artefacts, or pre-existing
repo-wide drift outside the AccessControl module.
