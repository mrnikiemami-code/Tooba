# Structural re-check (certify defense in depth)

Certify may not infer the Structure PASS from compilation, manifest membership or spot checks. This
file records the independent re-derivation of every structural gate against **current disk state** at
the W3 starting head `86ebb4dd`.

## Structure gate consumed

`docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W2/structure.md` →
`Structure-State = READY_FOR_CERTIFY`, scoped to `src/backend/Modules/Settlement`, produced at
`86ebb4dd` (this wave's starting head). Current, same surface, not contradicted by disk.

## Independent re-check results

| Gate | Re-check method | Result |
|---|---|---|
| Folder-Granularity-State | `Application/` top-level folders enumerated from disk = `Composition, Payouts, Validation`; subfolders under `Payouts/{Commands,Queries}` = 0 | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `/Modules/Settlement/` block parsed from `src/backend/Tooba.slnx` = 6 project entries, all resolving on disk | `CANONICAL` |
| Path-Namespace-State | 66 production `.cs` scanned, declared namespace vs path-derived namespace | `EXACT` (0 mismatches) |
| Physical-Copy-State | 21 retired Application paths + 2 retired Infrastructure paths asserted absent; 9 shared types + 10 CQRS requests asserted single-home | `CLEAN` |
| Root-Allowlist-State | manifest `rootAllowlist` vs on-disk project-root `.cs` set equality for the 3 listed projects; every forbidden file/folder absent | `ENFORCED` |
| Alias workaround | `GlobalUsings*.cs` = 0; `TypeForwardedTo` = 0 | `NONE` |
| Host final closure | no `Host/Settlement`, no `Host/Grid`, 0 `/settlement` literals in Host, no Settlement DbContext in Host | `PRESERVED` |
| Cohesion | largest production file 695 LOC, single responsibility (guard already extracted in W1); no mixed/god file; no cosmetic split | `COHESIVE` (`OVERSIZED_ONLY` watch) |
| Applicability | 10 real routes + real route mapper + Host route count 0 | `HTTP_OWNING` |

## No regression since the Structure PASS

- No production `.cs` was added, moved, renamed or deleted between `86ebb4dd` and this re-check.
- The only changes in the certify wave are documentation, the durable certify guard and SoT/manifest
  metadata; `git diff` over `src/backend/Modules/Settlement` is empty for this wave.
- The manifest entry's structural fields (`rootAllowlist`, `forbiddenRootFiles`,
  `forbiddenTopLevelFolders`) were verified equal to disk after the W2 normalization.

## Conclusion

The Structure PASS is current and valid for the certified surface; Certify did **not** weaken or
override it, and the structural invariants are additionally locked by the W2 durable guard
(`SettlementModuleAmsc001W2StructureGuardTests`, 9 tests) which is green.
