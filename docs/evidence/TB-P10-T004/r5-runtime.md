# TB-P10-T004-R5 — Runtime A–E

Host `:5088` (published Release `.tmp-t004r5-host-run`). Raw: `r5-runtime-raw.json` (`ok: true`).

| Scenario | Result |
| --- | --- |
| A normal manual review | PASS — evidence 200; ExpiresAt promoted ~24h beyond Cart TTL; confirm 200; Payment Succeeded; paid ExpiresAt null |
| B Admin reject | PASS — Held → Released |
| C retry same Order | PASS — manual-retry + evidence2; reservation Held again; same Payment |
| D review expiry + late confirm | PASS — Released then confirm 200 via authoritative reacquire; no raw `not_active` UX |
| E decimal 1.25 | PASS — Host domain/docker focused tests |
| Sandbox online smoke | PASS — gateway initiate + sandbox complete 200 |

Script: `_r5_runtime.mjs`.
