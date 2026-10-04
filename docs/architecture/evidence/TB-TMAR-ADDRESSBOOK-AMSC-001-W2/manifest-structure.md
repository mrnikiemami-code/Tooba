# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — manifest-structure

## Files changed

`docs/architecture/tmar-module-structure-manifests.json` — module `AddressBook`:

1. `certificationNote` — appended the W2 disposition (capability-first flattening, namespace move, new
   durable guards, `Folder-Granularity-State TECHNICAL_AXIS_FIRST -> PROFESSIONAL_SHALLOW`,
   `Structure-State READY_FOR_CERTIFY`).
2. `Tooba.AddressBook.Application.rootAllowlistJustification` — **corrected**. The old text documented the
   non-canonical shape as accepted:

   > "…intentionally has no root .cs file: `Commands/<UseCase>`, `Queries/<UseCase>`, Models, Ports, the
   > shared Validators capability and the Composition fault-to-Result seam…"

   The new text documents the repaired shape and explicitly records that the previous justification was the
   drift itself:

   > "…the single real capability lives under `Addresses/` with secondary `Commands/Queries/Validators` axes,
   > plus shared Models, Ports, the cross-cutting Validators capability and the Composition fault-to-Result
   > seam… AMSC-001 W2 removed the previous `Commands/<UseCase>`, `Queries/<UseCase>`, `Validators/<UseCase>`
   > single-file leaf folders that this justification used to document as accepted…"

## Fields deliberately NOT changed

| Field | Value | Reason |
| --- | --- | --- |
| `modules[AddressBook].structureCertified` | `true` (unchanged) | Skill section 23: do **not** flip whole-module `structureCertified` unless the invoking task is explicitly a Certify task. W2 is a Structure task; it prepares the handoff and leaves the verdict to W3. |
| `modules[AddressBook].lockVersion` | `ARCH-COMPLETE-002` (unchanged) | Lock vocabulary unchanged |
| all `rootAllowlist` / `forbiddenRootFiles` / `forbiddenTopLevelFolders` values | unchanged | No file moved to/from a root; widening an allowlist to hide debt is forbidden (section 19) |

## Manifest consistency verification

`TmarCompleteReferenceStructureGateTests` iterates the certified modules and, for `AddressBook`:

- `missing Tooba.AddressBook.<X> for AddressBook` → **not raised** (all 5 project paths exist)
- `rootAllowlist` vs actual root files → **equal** for all 5 projects
- `AssertNamespaceAlignment` for all 5 projects → **exact** after the move
- `forbiddenRootFiles` / `forbiddenTopLevelFolders` → **absent** on disk

The AddressBook project loop produced **no** failure. The three failures observed in this test class are
pre-existing and belong to a **different** module (`Catalog`):

```text
Expected: "Tooba.Catalog.Contracts.Cart"   Actual: "Tooba.Catalog.Contracts"
Expected: [..., "Cart", "Content", ...]    Actual: [..., "Cart", "Catalog", "Content", ...]
```

i.e. `Catalog` is present in the manifest's certified list but absent from the SoT
`structureLock.certifiedModules` (and vice versa in `Uncertified_modules_are_explicitly_not_claimed`). This
predates W2 (it was already red in W0 and W1) and is out of the AddressBook scope.

## SoT

`docs/architecture/tmar-current-state.json` — new `addressBookModuleAmsc001W2` entry recording the wave,
its closed findings (F3/F4), its preserved observations (F5/F6/F7), the classification states, the flattened
file list, the namespace move, the durable guards added, the behaviour-preservation statement
(`NONE_PURE_STRUCTURE_AND_NAMESPACE_MOVE`), the focused test counts, and the pre-existing-drift proof.
Both JSON files parse (`JSON.parse` verified).
