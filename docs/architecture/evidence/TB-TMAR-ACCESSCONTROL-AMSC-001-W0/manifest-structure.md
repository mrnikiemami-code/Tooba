# Manifest structure — AccessControl (W0)

Source: `docs/architecture/tmar-module-structure-manifests.json`.

## Current entry

| Field | Value |
| --- | --- |
| `module` | `AccessControl` |
| `structureCertified` | `true` |
| `lockVersion` | `ARCH-COMPLETE-002` |
| `projects` | 5 (`Application`, `Domain`, `Endpoints`, `Infrastructure`, `Contracts`) |

All five module projects are declared. Exactly one certified AccessControl entry exists. No temporary
pre-cert duplicate entry is present.

`uncertifiedHttpOwningModules` currently lists `Returns, Notification, Support, Wallet, Promotion` —
AccessControl is not among them.

## Manifest honesty check

| Manifest claim | Disk reality | Honest? |
| --- | --- | --- |
| Application root allowlist `[]` | no root `.cs` | YES |
| Application forbidden root files include `AccessControlDtos.cs` | file currently exists at `Application/Models/AccessControlDtos.cs` (not at root) | YES — prohibition targets the root, not the Models folder |
| Domain root allowlist `[]` | no root `.cs` | YES |
| Endpoints root allowlist `[AccessControlEndpointModule.cs]` | matches | YES |
| Infrastructure root allowlist `[AccessControlModule.cs]` | matches | YES |
| Contracts root allowlist `[]` | no root `.cs` | YES |
| `forbiddenTopLevelFolders` `[]` everywhere | no forbidden folder present | YES |

## Required W1/W2 manifest work

1. Extend Application `forbiddenRootFiles` with the 11 new cohesion-split filenames
   (see `root-allowlist.md`).
2. Record `Exceptions` in Application `forbiddenTopLevelFolders` once the W1 `Validation/` merge
   removes it, so the folder cannot silently return.
3. Add `Validation` to the Application capability-root expectations used by the W2 durable structure
   guard.
4. Keep exactly one certified AccessControl entry; do **not** flip `structureCertified` during
   W0/W1/W2 (that is a Certify-task action performed in W3).

## SoT keys currently recording AccessControl

```text
accessControlArchComplete002Structure
accessControlModuleAmc001
accessControlModuleAmc001W1
accessControlModuleAmc001W2
accessControlModuleAmc001W3
accessControlModuleAmc001W4
accessControlModuleAmc001W5Cert
accessControlModuleAmc002
accessControlModuleAmc002W1
accessControlModuleAmc002W2Cert
accessControlStructureRepair001
accessControlStructureRecert001
```

W0 adds `accessControlModuleAmsc001W0`. W1–W3 append their own keys. Historical keys are never
rewritten.

## Already-recorded prior certification facts (must not be contradicted)

| Key | Recorded fact |
| --- | --- |
| `accessControlArchComplete002Structure` | `COMPLETE_REFERENCE_PATTERN`, `HTTP_OWNING`, `MODULE_ENDPOINTS`, `MEDIATR_12_5`, validator coverage 6/6 required + 13 no-validator-required, 19 endpoint-reachable requests, `pathNamespace=EXACT`, `rootAllowlist=ENFORCED`, `aliasWorkaround=NONE`, `hostAccessControlResidue=ZERO`, `contractsBoundary=CLEAN_CONTRACTS_ONLY` |
| `accessControlStructureRecert001` | `ACCESSCONTROL_STRUCTURE_RECERTIFIED`, `PROFESSIONAL_SHALLOW`, `singleFileRequestLeafState=ZERO`, `technicalAxisFirstState=ZERO`, `physicalCopyState=CLEAN`, `solutionExplorerState=CANONICAL`, `accessControlDirectoryState=OVERSIZED_ONLY_WATCH`, `microserviceExtractable=true` |

The AMSC run must preserve every one of these facts while repairing F1–F4. Note the historical
"19 endpoint-reachable requests" count; the current working tree contains 20 requests (the 20th being
`GetSellerDevContextsQuery`, added by the later SellerDev work). W1 records the reconciled count
honestly rather than contradicting either record silently.
