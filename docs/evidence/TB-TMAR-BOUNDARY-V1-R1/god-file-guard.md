# God-file / source-size guard — TB-TMAR-BOUNDARY-V1-R1

Baseline: `src/backend/Host/Tooba.Host.Tests/Baselines/tmar-source-size-baseline.json`

- Exact paths only (no wildcards)
- 55 oversized entries (CRITICAL_GOD_FILE + OVERSIZED_LEGACY) at claim tip
- `thresholdNewFileLoc`: 800

Guard implementation:

- `TmarSourceSizeGuard.cs` — scan + evaluate
- `TmarSourceSizeAndInfraAppTests.Hand_written_source_size_does_not_expand_beyond_baseline`

Rules enforced:

| Violation type | Condition |
|---|---|
| NEW_OVERSIZED_FILE | hand-written source not in baseline with physical LOC > 800 |
| OVERSIZED_GROWTH | baselined file grows above recorded LOC |
| BASELINE_ENTRY_MISSING_FILE | baseline path deleted/split without removing baseline entry |

Shrink allowed: current LOC ≤ baseline LOC.

Baseline never auto-raises.

Synthetic proofs:

- `Synthetic_new_hand_written_file_over_800_loc_is_rejected`
- `Synthetic_oversized_legacy_growth_is_rejected`
- `Synthetic_oversized_legacy_shrink_is_allowed`

Report fields on failure: exact file, currentLoc, baselineLoc, threshold, violationType.
