# Recovery SoT — TB-TMAR-FE-BASELINE

## State after this task

- Frontend architecture baseline **established** (inventory, ownership, target arch, size/import/SEO guards).
- Canonical FE root: `src/frontend` (task alias `src/Web` does not exist).
- Source-size baseline: `docs/evidence/TB-TMAR-FE-BASELINE/frontend-source-size-baseline.json` (aligned with repo `tmar-source-size-baseline.json` FE entries).
- Locks: FE-ARCH-001, FE-SIZE-001/002, FE-SEO-001, FE-BOUNDARY-001/002.
- No broad FE refactor; no product behavior change.
- Product-Resume-Safety: **SAFE_WITH_TMAR_PARALLEL**
- Backend-Structural-Readiness: unchanged STABLE_FOR_PARALLEL_RECOVERY
- Frontend-Recovery-Readiness: **BASELINED** (guards on; migration phases pending)

## Next recommended task

**TB-TMAR-FE-F1** — low-risk shared structure fixes on top of these guards (stop admin-api growth patterns; optional safe reverse-import cleanup).

## Prior tip

Started from `c8730b2069ec5b607b83f10de97602c136c2f01f` (CONTRACTS-W6).
