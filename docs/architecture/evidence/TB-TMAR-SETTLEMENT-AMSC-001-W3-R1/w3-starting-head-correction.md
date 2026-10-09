# TB-TMAR-SETTLEMENT-AMSC-001-W3-R1 — W3 starting-head provenance correction note

This note is the required **correction record** for the second W3 proof defect. The historical W3 evidence
is an immutable record and is deliberately **not** rewritten; the correction is recorded here (and in the
Master Recovery `Settlement AMSC W3-R1 ...` entry) instead.

## 1. The defect

The historical `TB-TMAR-SETTLEMENT-AMSC-001-W3` SoT block recorded

```text
"startingHead": "4ca4aafc0acc3b5bc62c6cc366a9e50d07b11d05"
```

which is the **W1 Migrate** commit. The actual W3 starting HEAD was the **W2 Structure** commit

```text
86ebb4ddb0ba85da480f44a7e797cf1791233f2a
```

The historical W3 guard (`SettlementModuleAmsc001W3CertGuardTests`) asserted the same wrong value, so the
incorrect lineage was durably locked rather than caught.

The mistake is self-evident in the same SoT block: `currentStructureCommit` already names
`86ebb4ddb0ba85da480f44a7e797cf1791233f2a` as the consumed structure authority, and `parentTask` is
`TB-TMAR-SETTLEMENT-AMSC-001-W2`. A certify wave consumes its parent structure wave, so its starting head
must be the W2 commit.

## 2. Authority chain (verified ancestry, unchanged)

```text
W0 Analyze            bac4dbe38a46788c8fe27f42820c087294584d6d
W1 Migrate            4ca4aafc0acc3b5bc62c6cc366a9e50d07b11d05   (parent W0)
W2 Structure          86ebb4ddb0ba85da480f44a7e797cf1791233f2a   (parent W1)
W3 Certify (historical) 529b8054ec8cdfe43c0fadf7c2c6436ac8a4a4c2 (parent W2)
W3-R1 (this repair)   starting head 529b8054ec8cdfe43c0fadf7c2c6436ac8a4a4c2 (parent W3)
```

`git merge-base --is-ancestor` confirms all four authority commits are ancestors of the W3-R1 starting
head. Only the W3 **startingHead** value was wrong; no commit, no wave output and no production artifact was
misidentified.

## 3. Exact changes made by W3-R1

| Location | Field | Before | After |
|---|---|---|---|
| `docs/architecture/tmar-current-state.json` | `settlementAmsc001W3.startingHead` | `4ca4aafc0acc3b5bc62c6cc366a9e50d07b11d05` | `86ebb4ddb0ba85da480f44a7e797cf1791233f2a` |
| `docs/architecture/tmar-current-state.json` | `settlementAmsc001W3.authorityAncestry` | `W0 bac4dbe3 (analyze) -> W1 4ca4aafc (migrate) -> W2 (structure) -> W3 (this certify wave).` | `W0 bac4dbe3 (analyze) -> W1 4ca4aafc (migrate) -> W2 86ebb4dd (structure) -> W3 (this certify wave, starting head 86ebb4dd).` |
| `docs/architecture/tmar-current-state.json` | `settlementAmsc001W2.startingHeadFull` (new) | — | `4ca4aafc0acc3b5bc62c6cc366a9e50d07b11d05` (full W1 SHA, so the abbreviated W2 starting head is unambiguous) |
| `src/backend/Host/Tooba.Host.Tests/Architecture/SettlementModuleAmsc001W3CertGuardTests.cs` | starting-head assertion | `Assert.Equal("4ca4aafc0...", w3.startingHead)` | `Assert.Equal("86ebb4ddb0...", w3.startingHead)` (with the W3-R1 provenance comment) |
| `docs/architecture/tmar-module-structure-manifests.json` | Settlement `certificationNote` | `... W2 (structure) -> W3 (this certify wave) ... /v1/seller/settlements x5 and /v1/admin/settlements x5 ...` | `... W2 86ebb4dd (structure) -> W3 529b8054 ... -> W3-R1 ... /v1/seller/settlement x5 and /v1/admin/settlement x5 ...` plus the 3-of-6 root-rule-map sentence |

The W3 guard's other lineage assertions (`settlementAmsc001W0.commit == "bac4dbe3"`,
`settlementAmsc001W1.commit == "4ca4aafc"`) are **real and preserved**; they were not part of the defect.

## 4. What was deliberately NOT changed

- The historical W3 evidence files under `docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W3/`
  (`certification.md`, `validation.md`, `request-handler-validator-matrix.md`, `structural-recheck.md`).
  These already recorded the correct baseline (`Baseline: main @ 86ebb4dd`) and stay immutable.
- The historical W3 Master Recovery entry (`Settlement AMSC W3 fresh ARCH-COMPLETE-002 certification
  (module-local)`), including its own (incorrect) starting-head sentence. It is history and stays as
  written; the W3-R1 entry supersedes it explicitly.
- W0/W1/W2 SoT commit values and any production file, manifest project list or allowlist.

## 5. Guard against recurrence

- `SettlementModuleAmsc001W3CertGuardTests` now asserts the corrected
  `startingHead == 86ebb4ddb0ba85da480f44a7e797cf1791233f2a`, so the wrong lineage cannot silently return.
- `SettlementModuleAmsc001W3R1SetEqualityGuardTests` re-derives the full route→request→handler→validator
  universe from endpoint source, so a future dispatch replacement fails the build even when all counts are
  preserved.

No self-referential current-commit SHA is claimed in any as-yet-uncommitted file; the W3-R1 commit SHA is
reported only in the Bridge Result.
