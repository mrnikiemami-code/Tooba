# TB-TMAR-PARTY-AMSC-001-W3 — Final SHA reconciliation

Recorded at this commit: the W3 certification commit is `d1cc2f48` (`d1cc2f480619ce1ec68cd730650018883684e6c7`).
The `partyAmsc001W3.acceptedLineage.w3` field in `tmar-current-state.json` is reconciled from
`PENDING_THIS_COMMIT` to the real authoritative SHA at this metadata-only commit (the certifying
commit cannot contain its own SHA). Accepted lineage is now exact:
W0 `2477bbb3` → W1 `29012df0` → W2 `ffff7100` → W3 `d1cc2f48`. Zero production change.
