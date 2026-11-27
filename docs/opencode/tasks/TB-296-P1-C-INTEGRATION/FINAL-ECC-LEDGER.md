# TB-296 P1-C — FINAL ECC 60-ID CONSOLIDATED LEDGER (summary)

Mode: test/evidence-only consolidation. No product/test/guard/contract/matrix/seed edits,
no test reruns, no DB writes, no nested opencode CLI.
Generated: 2026-11-27T08:13:37Z (UTC).

Authoritative machine ledger: `FINAL-P1C-60-ID-LEDGER.json`
SHA256: `4d076cfff07dbfba29bfa72c47d8c0698d98128c010d7bab43f9d2fc1196ce11`

## Unique counts (60 immutable IDs)

| Status | Count |
|---|---|
| PASS | 57 |
| FAIL | 1 |
| NOT_RUN | 2 |
| **Total** | **60** |

Source contributions (independent, non-overlapping per ID):
- Group A/B/C executed: 22 → 20 PASS + 1 FAIL (`R10-08-N`) + 1 NOT_RUN (`R10-01-B`)
- Group D/E executed: 13 → 13 PASS
- Group F / OWN / D2 executed: 18 → 17 PASS + 1 NOT_RUN (`OWN-10`)
- Preserved prior PASS (7): `R10-01-N`, `R10-04-B`, `R10-10-P`, `R10-10-N`, `R10-10-B`, `R10-12-N`, `R10-13-N`
  (all 7 still PASS; recorded == actual SHA256 for each)

## Integrity checks

- Immutable ID set: exactly 60, unique (order from `FINAL-LEDGER.json`).
- One row per ID: TRUE. Duplicate IDs: none. Missing IDs: none. Extra IDs: none.
- Name-only mapping: FALSE. Every row is backed by an executed/observed evidence file
  (59 unique files; `R10-12-N` and `R10-13-N` share `p1c-inventory-feedback-result.json`).
- Evidence: 60/60 referenced files exist, nonempty, and SHA256 match the recorded hash
  (0 failures). Evidence content carries the exact case ID and real observations
  (HTTP requests/SQL/commands/comparisons/negative controls), not name matching.

## Identity

- Executed source head: `028b9da8d6710c944bf1bcdcd6bf5fea3faa0a53`
  (GROUP-ABC `baseHead` == group-D/E `codeHead`).
- Current worktree HEAD: `2ba30567e9392cb7035ec40b252dc46c5d5d8652` (later task commits).
  The six executed case/harness modules all hash-match the current worktree (6/6),
  so the exact executed harness sources are present unchanged.
- Build: served Host DLL recorded `bc839dc6…47c932` (artifact cleaned post-run);
  Next buildId `7tcRkdNm2wPHBfpPYVSvT`; group-F harness DLL `c9bee9…a3080` re-verified match.
- DB: `tooba_alpha|local|local`; provider SingleStore tenant `store-alpha`;
  baseline dump `p1c-baseline-20261127062248.dump` sha `d5e2df7b…77ade5`.

## Conflicts / adjudication

- No PASS-vs-FAIL contradiction for any ID across any source ledger
  (`p1c-group-abc-ledger-r2`, `p1c-executed-ledger4`, `p1c-group-de-consolidated-ledger`,
  `p1c-group-de-final2-ledger`, `FINAL-LEDGER`).
- Stale `NOT_RUN` rows superseded by later real execution on the exact ID: 109
  (abc-r2 37, executed-ledger4 34, de-consolidated 38); `FINAL-LEDGER.json` (all 60 NOT_RUN)
  is fully superseded. These are ordering/staleness artifacts, not result contradictions.
- Genuine product FAIL (1): `R10-08-N` — blank price is not fail-closed.
  `amount:null` → HTTP 500 `platform.unexpected`; omitted amount → HTTP 200 persisting 0 and
  creating a 2nd price fact (expected 1).
- NOT_RUN (2, with reason + evidence): `R10-01-B` (SingleStore DEV tenant exposes only
  `Marketplace`; no second independently allowed channel), `OWN-10` (DEV capability
  `canEditPhysicalInventory=true / canEditOfferedQuantity=false`; Marketplace-only clauses and
  in-app locale switch not observable).

## PASS IDs (57)

R10-01-P, R10-01-N, R10-02-P, R10-02-N, R10-02-B, R10-03-P, R10-03-N, R10-03-B,
R10-04-P, R10-04-N, R10-04-B, R10-05-P, R10-05-N, R10-05-B, R10-06-P, R10-06-N, R10-06-B,
R10-07-P, R10-07-N, R10-07-B, R10-08-P, R10-08-B, R10-09-P, R10-09-N, R10-09-B,
R10-10-P, R10-10-N, R10-10-B, R10-11-P, R10-11-N, R10-11-B, R10-12-P, R10-12-N, R10-12-B,
R10-13-P, R10-13-N, R10-13-B, R10-14-P, R10-14-N, R10-14-B,
OWN-01, OWN-02, OWN-03, OWN-04, OWN-05, OWN-06, OWN-07, OWN-08, OWN-09,
D2-ORDER, D2-HOLD, D2-ALLOCATION, D2-PARTIAL, D2-RECOVERY, D2-HTTP,
F-BENTRY-01, F-BENTRY-02.

FAIL: `R10-08-N`. NOT_RUN: `R10-01-B`, `OWN-10`.

## Reproduce

`node docs/opencode/tasks/TB-296-P1-C-INTEGRATION/p1c-consolidate-60.cjs`
(reads existing artifacts only; writes the machine ledger; no tests/DB).
