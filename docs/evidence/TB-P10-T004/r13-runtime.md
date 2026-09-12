# TB-P10-T004-R13 — Runtime matrix

Host :5098 rebuilt R13 against `tooba_alpha`. Raw: `r13-runtime-raw.json` ok=true.

| Scenario | Result |
| --- | --- |
| A Guest normal | PASS — commit 200; source Converted; Payment GET 200; new Active AddToCart 200; initiate/result 200 |
| B Guest manual | PASS — manual initiate 200 after owned checkout (same committed cart/secret) |
| C Guest sandbox | PASS — sandbox context 200 + complete success 200 after new Cart existed |
| D Authenticated | PASS — session-owned stub path; unrelated actor without secret 404 (no leak) |
| E Old Converted Cart | PASS — mutation rejected (409 version / EnsureActive); client rotates before AddToCart |
| F Security | PASS — new secret 403 access.denied; id-alone 403; detail is payment-access copy |
| G Refresh / proof survive | PASS — Payment GET after fresh cart still 200 with committed secret |
| H no duplicate Order | PASS — second shipping commit 400; one checkout id |

Known fixture `01a096cd-eee4-7000-8954-683cb41244de` not required; equivalent live fixture created above.
