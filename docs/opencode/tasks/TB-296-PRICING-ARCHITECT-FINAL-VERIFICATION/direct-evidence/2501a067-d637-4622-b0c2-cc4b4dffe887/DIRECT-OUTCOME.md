# Direct R11 verification — actual outcome (run 2501a067-d637-4622-b0c2-cc4b4dffe887)

Executed: `node docs/opencode/tasks/TB-296-PRICING-ARCHITECT-FINAL-VERIFICATION/direct-closure.cjs --publish`
on existing Host 5088 / Next 3000 / tooba_alpha, HEAD `0b1a6973829d69418bee79d46677259f7e1d022d`, product candidate `4f19708be3d980a22535fb446d3cfaa1a2ddfc9c` (HEAD adds docs only). No product/architecture change.

## Script results (DIRECT-RESULT.json)
- R11-PRICE-01-admin — **BLOCKED_OR_FAIL**: “foreign currency overwrote primary/other revision” (`direct-closure.cjs:173`; created offer `d85fffcd-215f-d9d0-4811-6c8b778f4cfc`, EUR 20.5, primary IRR).
- R11-PRICE-01-seller — **BLOCKED_OR_FAIL**: “create HTTP 403” (`direct-closure.cjs:151`; seller `01a030d1-40db-7000-b90c-a0705133f0eb`, free published variant `DEMO-demo-prod-fashion-men-men-shirts-1-7`; no offer created).
- independent-probe-fixture — **PASS**.
- R11-REF-03-missing-market — **BLOCKED_OR_FAIL in script**: “direct-market-probe exit=null” because the bounded `dotnet run` hit the 150000ms timeout during the first build. Rebuilt and re-ran the same architect probe with `--no-build`: **PASS** (4/4 observations `pricing.market.invalid`, readRows 0, positiveRows 1, read-only witness `|on`). See `DIRECT-MARKET-MANUAL.json`.
- R11-UAT-02-stock — **BLOCKED_OR_FAIL**: “fetch failed” (`direct-closure.cjs:210`; no mutation asserted, fixture cleaned).
- R11-PRICE-05-concurrent — **PASS** (one durable fact, same revision, one mutation).
- R11-LOCALE-01-seller — **NOT_RUN**: seller create fixture unavailable (seller create 403).
- R11-UAT-03-seller — **NOT_RUN**: seller create fixture unavailable.
- R11-REG-01-new-currency-checkout — **NOT_RUN**: script’s explicit safe-boundary (separate isolated checkout proof required).
- Cleanup: 3× PASS; all three fixtures removed exactly; no residue.

## Identity
- Next BUILD_ID `fzXq0vXgQ2zeCrG2p_IBW`; Host DLL SHA-256 `10098e16bfca69fa41a736609b2535aad8e911457db194ed916a112e4c379529`; DB `tooba_alpha`.
- `legacyFractionalIrr` = 2310 (read-only measure; no rewrite authorized).
- Script publication commit `8e7cac268f383e28e35f900543f86f37467cbc57`; this addendum is evidence-only.
- No product/architecture edits, no new Host/DB, no screenshots, no reviewer/subagents, no acceptance criteria changes.
