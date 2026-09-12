# TB-P10-T004-R14 — Runtime matrix

Host `http://127.0.0.1:5099` rebuilt R14 against `tooba_alpha`. Raw: `r14-runtime-raw.json` ok=true.

| Scenario | Result |
| --- | --- |
| A Sandbox success → manual initiate | PASS — 409 `payment.already_succeeded`; payments=1 attempts=1 |
| B Sandbox success → online initiate (new key) | PASS — 409 `payment.already_succeeded` |
| Same-key replay after success | PASS — 200 same payment `bfb947ad-e3e5-417f-9f0a-704c1c4a0493` |
| UI capability | PASS — `paymentState=Paid`, `canInitiatePayment=false` |
| C Manual confirm → evidence again | PASS — confirm 200 Succeeded; second evidence 409; no new evidence row |
| D Failed online → retry | PASS — 200 new attempt `43ae8cc7-66d4-4248-b346-657ea7fd8396` |
| E Rejected manual → retry | PASS — reject/retry/evidence2 all 200 |
| F Expired unpaid → same Order retry | PASS — Expired then unpaid-retry 200; one checkout `01a096f4-a348-7000-87a1-fec954269225` |
| G Duplicate sandbox success | PASS — 200; still one payment row |
