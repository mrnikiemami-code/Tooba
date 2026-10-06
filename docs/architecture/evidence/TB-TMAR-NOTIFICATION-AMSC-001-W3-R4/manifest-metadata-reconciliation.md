# TB-TMAR-NOTIFICATION-AMSC-001-W3-R4 — manifest-metadata-reconciliation

Single-point metadata repair inside `tmar-module-structure-manifests.json`.

## Stale phrase — exact before/after

**Before** (`Notification → Tooba.Notification.Application → rootAllowlistJustification`):

> "No root .cs. Capability-first Customer/ and Seller/ branches (Commands/Queries secondary axes
> under the capability, **one folder per use case each carrying request+handler and validator
> files**) with shared cross-capability Composition/ (typed-fault seam), Models/, Ports/,
> Rendering/ and Validators/; the previous technical-axis-first Application/Commands|Queries roots
> are forbidden; namespace alignment is exact path-derived equality."

The bolded fragment described the superseded over-foldered W0/W3-era shape and contradicted the
current R2/R3 certified tree (ZERO per-use-case leaves).

**After**:

> "No root .cs. Capability-first Customer/ and Seller/ branches with Commands/Queries secondary
> axes; all request and validator source files live directly on those axes with **zero per-use-case
> leaf directories (single-file and per-use-case request leaf states are ZERO)**; shared
> cross-capability Composition/ (typed-fault seam), Models/, Ports/, Rendering/ and Validators/
> remain; the previous technical-axis-first Application/Commands|Queries roots are forbidden;
> path↔namespace is exact."

## Proofs

- Structural arrays/flags unchanged (diff vs HEAD shows exactly this one line modified):
  - `rootAllowlist` arrays: unchanged (empty as before, all projects).
  - `forbiddenRootFiles` / `forbiddenTopLevelFolders`: unchanged (Application still forbids
    `Commands`, `Queries`, `Errors`, `Services`).
  - Module/project membership: unchanged (6 Notification projects, single certified module entry).
  - Module-level `structureCertified: true` and `lockVersion: ARCH-COMPLETE-002`: unchanged.
  - `certificationNote`: NOT modified (it already described the R2 flattening truthfully from W3-R3;
    no stale phrase existed there).
- Production files changed: ZERO (no `src/backend/Modules/Notification/**` file in the diff).
- Schema/migrations: unchanged. Frontend: frozen/unchanged.
- Current certification authority remains W3-R3 at `e3eb185ba109779f395d64e35b3704e1439b40ae`
  (`COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` / `CERTIFIED`).
- `automaticNextImplementationTask = NONE` (block-level and repository-global).
- JSON parse: PASS (BOM-tolerant parse of both SoT files).
