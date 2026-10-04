# Offer manifest structure — W0

## Current state

`docs/architecture/tmar-module-structure-manifests.json` → `modules[]` entry `module: "Offer"`:

- `structureCertified: true`
- `lockVersion: "ARCH-COMPLETE-002"`
- `projects[]`: **3** entries (`Tooba.Offer.Application`, `Tooba.Offer.Endpoints`, `Tooba.Offer.Infrastructure`)
- `forbiddenTopLevelFolders`: `[]` for all three
- `rootAllowlist`: `["OfferEndpointModule.cs"]` for Endpoints, `[]` for the other two

## Gaps (to fix in W2)

| # | Gap | Severity |
| --- | --- | --- |
| 1 | `Tooba.Offer.Domain` and `Tooba.Offer.Contracts` have **no** manifest project entry, although both exist on disk and are governed by project-local guards | medium — manifest/disk reconciliation is part of ARCH-COMPLETE-002 |
| 2 | `forbiddenTopLevelFolders` is empty for every Offer project, so no manifest-level rule blocks a future technical-axis-first regression | medium |
| 3 | Manifest carries no folder-granularity lock (single-file request leaves / technical-axis-first) | medium |
| 4 | `tmar-current-state.json` has three separate historical keys (`offerArchComplete002Audit`, `offerHostResidueRepair`, `offerArchComplete002Structure`) and no AMSC wave key | low |

## SoT entries today

| Key | State |
| --- | --- |
| `offerArchComplete002Audit` | `ACCEPTED_AUDIT_ONLY`, `structureCertifiedUnderArchComplete002: false`, `validatorsMissing: 5` |
| `offerHostResidueRepair` | `HOST_RESIDUE_REMOVED_THIN_SECURITY_ADAPTER_ONLY`, `offerToHostDependency: ZERO` |
| `offerArchComplete002Structure` | `COMPLETE_REFERENCE_PATTERN`, `structureCertifiedUnderArchComplete002: true`, `pathNamespace: EXACT`, `validatorCoverage: 5_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED` |

`offerArchComplete002Structure` claims `COMPLETE_REFERENCE_PATTERN` with `structureCertifiedUnderArchComplete002: true`, but the touched surface now fails two Offer architecture guards and has a `TECHNICAL_AXIS_FIRST` Application tree. This is **certification drift** and is disclosed honestly here. It is repaired by this AMSC run (W1/W2/W3), not by weakening the claim or the guards.

## Commitment

W2 will:

- add honest `Domain` and `Contracts` entries (`rootAllowlist: []`, non-empty `forbiddenTopLevelFolders` for the technical-axis folders being removed);
- keep every existing `forbiddenRootFiles` entry;
- not flip `structureCertified` (that belongs to Certify/W3).

W3 (Certify) will add the AMSC wave key to `tmar-current-state.json`.
