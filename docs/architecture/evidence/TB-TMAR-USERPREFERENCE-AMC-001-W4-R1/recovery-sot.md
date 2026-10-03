# TB-TMAR-USERPREFERENCE-AMC-001-W4-R1 — Recovery / SoT

## Preserved

- `userPreferenceAmc001` W0–W4 certification block remains historical.
- `userPreferenceAmc001.implementationCommit` records W4 SHA `f2c3c2f28e1a7db9075992cd4bf3f41300562c4d` (not rewritten to R1).

## Added

- `userPreferenceAmc001.latestPostCertRepairCheckpoint = userPreferenceAmc001W4R1`
- `userPreferenceAmc001W4R1` repair checkpoint with:
  - `state = USERPREFERENCE_AMC_W4_CERT_DEFECTS_REPAIRED`
  - `endpointCatchAndMapState = ZERO`
  - `workflowStop = USER_REVIEW_USERPREFERENCE_AMC_001_W4_R1`
  - `automaticNextImplementationTask = NONE`
  - `implementationCommit` = W4-R1 implementation SHA
