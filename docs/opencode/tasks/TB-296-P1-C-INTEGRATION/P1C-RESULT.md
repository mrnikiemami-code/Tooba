# TB-296-P1-C connected campaign result

Status: FAIL — one connected mandatory feedback case failed; full campaign aborted before remaining IDs. No product edits.

Candidate/source: 41d1b2698a4912bb8964f593d6ab7fc5eacccc40 (product b0c6467249bb945d661490a0eab407ec487e735c).
Served: Host 5088 PID 4440, DLL SHA-256 A7980377463FCF05C397F5231D6B7259F20C7509BB3E9606AED7FDBB82F14C44; Next 3000 PID 8508, BUILD_ID 7tcRkdNm2wPHBfpPYVSvT; DB tooba_alpha. Exact built source; no stale --no-build backend claim.

## Required build/guards
- Host build/publish PASS.
- Frontend typecheck PASS; production build PASS (BUILD_ID 7tcRkdNm2wPHBfpPYVSvT).
- Frontend architecture exit 1 at 27 baseline violations (no new P1-C path/size finding); i18n PASS.
- Acceptance gate self-test PASS; architect review gate self-test PASS.
- Strict OpenSpec exit 0 (existing warnings); Impeccable changed detector `[]`.
- No screenshots.

## Connected matrix failure
- R9-02 FAIL: after real create→edit navigation, admin price save returned HTTP 200 and independently read price changed to 876544, but no shared Toast became visible within 15s (`verify-r10.cjs:226`, raw p1c-r10-run2.log). The retry copy confirmed the same toast timeout; the next scenario then encountered an open MessageBox/blocking dialog intercepting save, consistent with feedback/dialog handling not matching the accepted flow.
- Because the verifier writes its result only after completion, no aggregate 42 JSON was emitted. R10-01..04 preconditions may have executed but are NOT_RUN as verified IDs, not PASS. All remaining original 42 IDs, additive 10 and D2/HTTP/ORDER/HOLD/ALLOCATION/PARTIAL/RECOVERY clauses are NOT_RUN. No name-only mapping.
- Fixtures: aborted attempts created two active R9 offers; both were removed with dependent pricing/inventory/history/outbox rows. Seed baselines 457e/01a0 read OK in cleanup. No residual active R9 test offer.
- Provider campaign did not proceed to third-locale/DB-registry, capabilities/history, real order/checkout recovery, response-correlated races or 360/768/1440 geometry.

## AQ/B-F
No eight-AQ acceptance. B-F01..06 connected proof remains NOT_RUN except what focused pre-campaign tests established; R9-02 is a connected FAIL. R9-02 feedback failure is architect-owned substantive product behavior; no worker repair.
