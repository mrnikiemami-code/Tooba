# Manifest structure interaction (skill §23)

`docs/architecture/tmar-module-structure-manifests.json`

## Settlement entry — before / after this wave

### Before (as produced by W1 `4ca4aafc`)

```json
{
  "module": "Settlement",
  "structureCertified": true,
  "lockVersion": "ARCH-COMPLETE-002",
  "projects": [
    {
  "projectName": "Tooba.Settlement.Application",
  "rootAllowlist": [],
  "rootAllowlistJustification": "…retired GlobalUsings…",
  "forbiddenRootFiles": [ …10 entries… ],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.Settlement.Endpoints",
      "rootAllowlist": [ "SettlementEndpointModule.cs" ],
      "forbiddenRootFiles": [ …4 entries… ],
      "forbiddenTopLevelFolders": []
    },
    {
  "projectName": "Tooba.Settlement.Infrastructure",
  "rootAllowlist": [],
  "rootAllowlistJustification": "…retired GlobalUsings…",
  "forbiddenRootFiles": [ …8 entries… ],
      "forbiddenTopLevelFolders": []
    }
  ]
}
```

Problems: (a) `projectName`/`rootAllowlist`/`rootAllowlistJustification`/`forbiddenRootFiles` were at
the wrong indentation depth inside each object — valid JSON, inconsistent with the other 28 certified
entries; (b) **no `certificationNote` / `evidence`**, making Settlement the only certified entry with no
recorded certification basis; (c) `forbiddenTopLevelFolders` was empty everywhere, so the retired
technical-axis-first Application roots and an Infrastructure `Migrations`/`Repositories` dump were not
durably locked.

### After (this wave)

```json
{
  "module": "Settlement",
  "structureCertified": true,
  "lockVersion": "ARCH-COMPLETE-002",
  "certificationNote": "CERTIFIED by TB-TMAR-SETTLEMENT-AMSC-001-W3 (tooba-architecture-certify) after the Structure wave W2. …",
  "evidence": "docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W3/certification.md",
  "projects": [
    { "projectName": "Tooba.Settlement.Application",
      "rootAllowlist": [],
      "forbiddenRootFiles": [ …10 entries… ],
      "forbiddenTopLevelFolders": ["Commands","Queries","Models","Ports","Validators","Errors","Handlers","Requests"] },
    { "projectName": "Tooba.Settlement.Endpoints",
      "rootAllowlist": ["SettlementEndpointModule.cs"],
      "forbiddenRootFiles": [ …4 entries… ],
      "forbiddenTopLevelFolders": [] },
    { "projectName": "Tooba.Settlement.Infrastructure",
      "rootAllowlist": [],
      "forbiddenRootFiles": [ …8 entries… ],
      "forbiddenTopLevelFolders": ["Migrations","Repositories"] }
  ]
}
```

## Repository-level manifest state

| Key | Value |
|---|---|
| `modules[]` length | 29 (was 28 before this wave's promotion) |
| Settlement entries in `modules[]` | exactly 1 |
| Settlement entries in `preCertModules` | 0 (never present) |
| `uncertifiedHttpOwningModules` | `["Support","Wallet"]` (Settlement absent) |
| `definitionMarker` / `rules` / `version` | unchanged |

## Honesty rules respected

- `rootAllowlist` was **not** widened to hide debt — Application and Infrastructure stay `[]`.
- `forbiddenRootFiles` was **not** trimmed; the two `GlobalUsings` files remain forbidden in both
  projects.
- `structureCertified` / `lockVersion` were not flipped by W2 (the module was already certified under
  ARCH-COMPLETE-002; this wave records the re-certification basis and adds the missing locks).
- No unrelated module entry was touched. The diff is confined to the Settlement object plus the
  `modules[]` promotion performed by the certify wave.
- `modules[]` grew by one (Returns, promoted by the parallel Returns AMSC wave at `fa535eca`, which is
  already in the baseline history) — not by this wave.

## Machine enforcement

| Guard | What it locks |
|---|---|
| `SettlementModuleAmsc001W2StructureGuardTests.Root_allowlists_match_disk_and_forbidden_entries_are_absent` | manifest `rootAllowlist` ↔ disk equality + forbidden files/folders absent |
| `SettlementModuleAmsc001W2StructureGuardTests.Module_is_certified_and_absent_from_uncertified_lists` | `structureCertified == true`, `lockVersion == ARCH-COMPLETE-002`, module absent from `uncertifiedHttpOwningModules` and `preCertModules` |
| `TmarCompleteReferenceStructureGateTests` | repository-global certified-module set + per-module root allowlist / namespace alignment |
