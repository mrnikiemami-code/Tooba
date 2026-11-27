# P1-C execution recovery final report

Status: FAIL — four real cases PASS, 56 immutable IDs/clauses NOT_RUN; no acceptance.
Source: c54a25453a547cb162062cf669010e69bff205c3; product 58a83f7a59a3625c4a5c941a5a3e8dfbb008e6d1; frontend b0c6467249bb945d661490a0eab407ec487e735c.

## Identity/build
- Final Host DLL SHA-256 BC839DC66053490B1A1DE64DAA6D655A7C4BF912D4B4DDCB32C5E57ED347C932; Host 5088 ready; DB tooba_alpha.
- Next build ID 7tcRkdNm2wPHBfpPYVSvT; no frontend source change in correction.
- Scheduler self-test PASS; node --check PASS; Host/Offer/Inventory affected builds/tests PASS from corrective candidate.
- Current fixture restored: sellerB roles 0, assignments 0, ceiling 32 all disabled; no active P1C offers; DB backup outside repo and `pg_restore --list` PASS; no tracked dump on current branch.

## Actual PASS
- R10-01-N PASS: actual HTTP unauthorized-channel denial and independent SQL empty-tuple proof.
- R10-04-B PASS: actual create replay/changed-intent/different-key conflict on empty tuple with independent SQL.
- R10-12-N PASS: actual isolated Draft inventory feedback helper (blank reason no write, valid 12 persisted, controlled abort modal/state preserved, reload preserved 14, exact retry persisted 14/factCount3).
- R10-13-N PASS: same controlled abort/retry evidence with one additional physical fact only.
No descriptive mapping; each result references actual trace/SQL evidence.

## NOT_RUN
- 56 IDs/transferred clauses remain without executable oracles: original R10-01-P/B, R10-02..R10-11, R10-12-P/B, R10-13-P/B, R10-14; OWN-01..10; D2-ORDER/HOLD/ALLOCATION/PARTIAL/RECOVERY/HTTP; F-BENTRY-01/02.
- R10-01-B remains dependency-limited: single trusted SalesChannel context; no invented second channel.
- No screenshots, no product edits, no final AQ receipt, no owner UAT.

## Ledger
`p1c-executed-ledger2.json`: PASS 4, FAIL 0, NOT_RUN 56. This is not complete acceptance; worker report only.
