# TB-TMAR-PAGECOMPOSITION-AMSC-001-W3-R2 — Lineage Reconciliation

## Requirement

The Architect verdict established two recovery facts that blocked closure:

1. `pageCompositionAmsc001W3R1` did not record its own final commit SHA `9770b884243b9e586fbeb163e5d0d538f488ea0a`.
2. A real W1 metadata reconciliation commit `11e22747b6f9d4f02d9bdc27ac6706925c7b4fcc` exists between the W1 implementation and W2, but the final recovery lineage omitted it, showing only `W1 6a28c921 -> W2 361a1837`. W2 started from `11e22747`.

## Reconciliation decisions (per task contract)

| Item | Decision |
|---|---|
| W1 implementation authority | `6a28c921f14a5cc7127b03d41fd35ee625695da5` — PRESERVED, not replaced (`pageCompositionAmsc001W1.commit = 6a28c921` stays) |
| W1 metadata reconciliation | `11e22747b6f9d4f02d9bdc27ac6706925c7b4fcc` — RECORDED as METADATA_ONLY, never an implementation wave |
| W2 starting head | `11e22747` — RECORDED (`w2StartingHead`) |
| Actual parent chain | `RECONCILED` — git parent proof below |
| R1 final SHA | `RECORDED_9770B884` in SoT `pageCompositionAmsc001W3R1.commit/commitFull` |
| R2 state | `PAGECOMPOSITION_AMSC_001_RECOVERY_CLOSED` (no self-referential PENDING commit field) |
| Certification authority | unchanged: W3 `07e9032a20e1d0d52afb0bc70f61c821d1bb0fad` |

## Verified git parent chain

```text
652036cc parent=0478668b
6a28c921 parent=652036cc
11e22747 parent=6a28c921
361a1837 parent=11e22747
07e9032a parent=361a1837
9770b884 parent=07e9032a
```

All four Architect-required parent assertions hold exactly.

## Two lineages, both true

- **Semantic AMSC wave lineage** (implementation authority, recorded in `acceptedLineage`): W0 `652036cc` → W1 `6a28c921` → W2 `361a1837` → W3 `07e9032a`.
- **Actual git/recovery handoff chain** (what W2 actually started from): W0 `652036cc` → W1 `6a28c921` → **W1 metadata reconciliation `11e22747`** → W2 `361a1837` → W3 `07e9032a` → W3-R1 `9770b884`.

`11e22747` only reconciled W1 metadata/recovery lineage (SoT docs); it changed no PageComposition production code and carries no implementation authority, so it does not appear in `acceptedLineage` and is never counted as a wave.

## Preservation checks

- W3 verdict `COMPLETE_REFERENCE_PATTERN` / lock `ARCH-COMPLETE-002` / `structureCertified = true`: unchanged.
- Validator matrix `EXHAUSTIVE_8_OF_8`: unchanged.
- Self-contained boundary (`NONE_SELF_CONTAINED`, zero foreign edges): unchanged.
- `tmar-module-structure-manifests.json`: untouched (0-byte diff).
- Historical AMC-001 lineage: HISTORICAL/SUPERSEDED, not rewritten.
- Global Host checkpoint: PRESERVED.
- `automaticNextImplementationTask = NONE`; `workflowStop = USER_REVIEW_PAGECOMPOSITION_AMSC_001_W3_R2`.
