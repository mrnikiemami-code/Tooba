# ECC execution metrics — TB-296-P1-C

Consolidation only; no test rerun, no subagent launch, no product edit.

## Actual agent invocations
- planner (CLI invocation, before native-only recovery): partition 53 IDs into groups A-F. Session `ses_de4342dbbffeNUdnwGlABH3lO9`; duration ~55s; reported tokens 77,008 total (input 2,964, output 4,081, reasoning 8,779, cache read 61,184).
- tdd-guide (native Task): groups A/B/C, session `ses_de4190d0fffeStmUjaSS3INWPW`; duration ~35m; 20 PASS, 1 FAIL, 1 NOT_RUN.
- e2e-runner (native Task): groups D/E, session `ses_de41907b0ffe9e7VxEzEgp3fLD`; duration 56m; 13 PASS, 0 FAIL, 0 new NOT_RUN.
- security-reviewer (native Task): group F/OWN/D2, session `ses_de41902a4ffeoFV21A1TeH3yTr`; duration ~35m; 17 PASS, 0 FAIL, 1 NOT_RUN.
- architect/code-reviewer/build native review invocations were dispatched but interrupted; consolidation was completed by the available build outputs and read-only merger. No independent architect/code review completed.

## Parallelism
A/B/C, D/E and F ran concurrently against isolated fixtures; wall-clock maximum was 56m (e2e-runner), not the sum of agent durations.

## Duplication / conflicts
- No duplicate ID ownership in final ledger.
- No PASS/FAIL contradiction. Stale earlier NOT_RUN rows superseded by later actual executions.
- No premature stop in this consolidation; prior worker stops remain historical.

## Final result
PASS 57, FAIL 1 (`R10-08-N` blank price fail-closed defect), NOT_RUN 2 (`R10-01-B`, `OWN-10` environment dependencies), Total 60.
Evidence: FINAL-P1C-60-ID-LEDGER.json; all 60 evidence files exist/nonempty and SHA-256 match. Source executed `028b9da8d6710c944bf1bcdcd6bf5fea3faa0a53`; current worktree `2ba30567e9392cb7035ec40b252dc46c5d5d8652`.
